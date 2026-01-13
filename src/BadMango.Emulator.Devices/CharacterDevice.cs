// <copyright file="CharacterDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Interfaces;

/// <summary>
/// Character generator device managing character glyph ROM and RAM.
/// </summary>
/// <remarks>
/// <para>
/// The character device manages the character generator ROM and RAM, providing
/// character bitmap data to the video renderer. This follows the design where
/// character data exists outside the CPU's address space, similar to how a
/// modern GPU has its own memory.
/// </para>
/// <para>
/// The device owns:
/// <list type="bullet">
/// <item><description>4KB character ROM for primary and alternate character sets</description></item>
/// <item><description>4KB glyph RAM for custom character overlays</description></item>
/// </list>
/// </para>
/// <para>
/// "Glyph Bank 1" and "Glyph Bank 2" refer to the lower ($0000-$07FF) and upper
/// ($0800-$0FFF) halves of either ROM or RAM. The ALTCHAR switch determines which
/// bank is used. ALTGLYPHx determines which ROM glyph bank to overlay with RAM.
/// </para>
/// <para>
/// Toggle soft switches ($C042-$C04D) - write-only:
/// </para>
/// <list type="bullet">
/// <item><description>$C042: CLRGLYPHRD - Disable glyph RAM reading</description></item>
/// <item><description>$C043: SETGLYPHRD - Enable glyph RAM reading</description></item>
/// <item><description>$C044: CLRGLYPHWRT - Disable glyph RAM writing</description></item>
/// <item><description>$C045: SETGLYPHWRT - Enable glyph RAM writing</description></item>
/// <item><description>$C046: CLRNOFLASH1 - Enable flashing for bank 1</description></item>
/// <item><description>$C047: SETNOFLASH1 - Disable flashing for bank 1</description></item>
/// <item><description>$C048: CLRNOFLASH2 - Enable flashing for bank 2</description></item>
/// <item><description>$C049: SETNOFLASH2 - Disable flashing for bank 2</description></item>
/// <item><description>$C04A: CLRALTGLYPH1 - Disable glyph bank 1 overlay</description></item>
/// <item><description>$C04B: SETALTGLYPH1 - Enable glyph bank 1 overlay</description></item>
/// <item><description>$C04C: CLRALTGLYPH2 - Disable glyph bank 2 overlay</description></item>
/// <item><description>$C04D: SETALTGLYPH2 - Enable glyph bank 2 overlay</description></item>
/// </list>
/// <para>
/// Status reads ($C034-$C039):
/// </para>
/// <list type="bullet">
/// <item><description>$C034: RDGLYPHRD - Read glyph read status</description></item>
/// <item><description>$C035: RDGLYPHWRT - Read glyph write status</description></item>
/// <item><description>$C036: RDNOFLASH1 - Read no-flash bank 1 status</description></item>
/// <item><description>$C037: RDNOFLASH2 - Read no-flash bank 2 status</description></item>
/// <item><description>$C038: RDALTGLYPH1 - Read glyph bank 1 status</description></item>
/// <item><description>$C039: RDALTGLYPH2 - Read glyph bank 2 status</description></item>
/// </list>
/// <para>
/// The CharacterDevice also owns the ALTCHAR switch:
/// </para>
/// <list type="bullet">
/// <item><description>$C00E: CLRALTCHAR - Select primary character set</description></item>
/// <item><description>$C00F: SETALTCHAR - Select alternate character set</description></item>
/// <item><description>$C01E: RDALTCHAR - Read alternate character set status</description></item>
/// </list>
/// </remarks>
[DeviceType("character")]
public sealed class CharacterDevice : ICharacterDevice, ISoftSwitchProvider, IGlyphHotLoader
{
    /// <summary>
    /// Character ROM size: 4KB for two complete character sets.
    /// </summary>
    public const int CharacterRomSize = 4096;

    /// <summary>
    /// Size of each character set within the ROM.
    /// </summary>
    public const int CharacterSetSize = 2048;

    /// <summary>
    /// Size of the glyph RAM: 4KB for character overlays.
    /// </summary>
    public const int GlyphRamSize = 4096;

    private const byte StatusBitSet = 0x80;
    private const byte StatusBitClear = 0x00;

    private readonly PhysicalMemory glyphRam;

    private PhysicalMemory? characterRom;

    private bool altCharSet;
    private bool altGlyph1Enabled;
    private bool altGlyph2Enabled;
    private bool noFlash1Enabled;
    private bool noFlash2Enabled = true; // Bank 2 defaults to NOFLASH
    private bool glyphReadEnabled;
    private bool glyphWriteEnabled;
    private bool characterRomChangePending;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterDevice"/> class.
    /// </summary>
    public CharacterDevice()
    {
        // Create single glyph RAM (4KB)
        glyphRam = new PhysicalMemory(GlyphRamSize, "GlyphRAM");
    }

    /// <inheritdoc />
    public event Action? CharacterRomChanged;

    /// <inheritdoc />
    public event EventHandler<GlyphDataChangedEventArgs>? GlyphDataChanged;

    /// <inheritdoc />
    public string Name => "Character Generator";

    /// <inheritdoc />
    public string DeviceType => "Character";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.Motherboard;

    /// <inheritdoc />
    public string ProviderName => "Character";

    /// <inheritdoc />
    public bool IsCharacterRomLoaded => characterRom != null;

    /// <inheritdoc />
    public bool IsAltCharSet => altCharSet;

    /// <inheritdoc />
    public bool IsAltGlyph1Enabled => altGlyph1Enabled;

    /// <inheritdoc />
    public bool IsAltGlyph2Enabled => altGlyph2Enabled;

    /// <inheritdoc />
    public bool IsNoFlash1Enabled => noFlash1Enabled;

    /// <inheritdoc />
    public bool IsNoFlash2Enabled => noFlash2Enabled;

    /// <inheritdoc />
    public bool IsGlyphReadEnabled => glyphReadEnabled;

    /// <inheritdoc />
    public bool IsGlyphWriteEnabled => glyphWriteEnabled;

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        // No scheduled events needed for character device
    }

    /// <inheritdoc />
    public void RegisterHandlers(IOPageDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        // ALTCHAR switch ($C00E-$C00F) - write-only toggles
        dispatcher.RegisterWrite(0x0E, HandleClrAltChar);    // CLRALTCHAR
        dispatcher.RegisterWrite(0x0F, HandleSetAltChar);    // SETALTCHAR

        // Toggle switches ($C042-$C04D) - write-only
        dispatcher.RegisterWrite(0x42, HandleClrGlyphRd);    // CLRGLYPHRD
        dispatcher.RegisterWrite(0x43, HandleSetGlyphRd);    // SETGLYPHRD
        dispatcher.RegisterWrite(0x44, HandleClrGlyphWrt);   // CLRGLYPHWRT
        dispatcher.RegisterWrite(0x45, HandleSetGlyphWrt);   // SETGLYPHWRT
        dispatcher.RegisterWrite(0x46, HandleClrNoFlash1);   // CLRNOFLASH1
        dispatcher.RegisterWrite(0x47, HandleSetNoFlash1);   // SETNOFLASH1
        dispatcher.RegisterWrite(0x48, HandleClrNoFlash2);   // CLRNOFLASH2
        dispatcher.RegisterWrite(0x49, HandleSetNoFlash2);   // SETNOFLASH2
        dispatcher.RegisterWrite(0x4A, HandleClrAltGlyph1);  // CLRALTGLYPH1
        dispatcher.RegisterWrite(0x4B, HandleSetAltGlyph1);  // SETALTGLYPH1
        dispatcher.RegisterWrite(0x4C, HandleClrAltGlyph2);  // CLRALTGLYPH2
        dispatcher.RegisterWrite(0x4D, HandleSetAltGlyph2);  // SETALTGLYPH2

        // Status reads ($C01E for ALTCHAR, $C034-$C039 for others)
        dispatcher.RegisterRead(0x1E, ReadAltCharStatus);    // RDALTCHAR
        dispatcher.RegisterRead(0x34, ReadGlyphRdStatus);    // RDGLYPHRD
        dispatcher.RegisterRead(0x35, ReadGlyphWrtStatus);   // RDGLYPHWRT
        dispatcher.RegisterRead(0x36, ReadNoFlash1Status);   // RDNOFLASH1
        dispatcher.RegisterRead(0x37, ReadNoFlash2Status);   // RDNOFLASH2
        dispatcher.RegisterRead(0x38, ReadAltGlyph1Status);  // RDALTGLYPH1
        dispatcher.RegisterRead(0x39, ReadAltGlyph2Status);  // RDALTGLYPH2
    }

    /// <inheritdoc />
    public void Reset()
    {
        altCharSet = false;
        altGlyph1Enabled = false;
        altGlyph2Enabled = false;
        noFlash1Enabled = false;
        noFlash2Enabled = true; // Bank 2 defaults to NOFLASH
        glyphReadEnabled = false;
        glyphWriteEnabled = false;
        characterRomChangePending = false;
    }

    /// <inheritdoc />
    public void LoadCharacterRom(byte[] romData)
    {
        ArgumentNullException.ThrowIfNull(romData);

        if (romData.Length != CharacterRomSize)
        {
            throw new ArgumentException(
                $"Character ROM must be exactly {CharacterRomSize} bytes, " +
                $"but got {romData.Length} bytes.",
                nameof(romData));
        }

        characterRom = new PhysicalMemory(romData, "CharacterROM");
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetCharacterScanline(byte charCode, int scanline, bool useAltCharSet)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(scanline, 7);

        // Determine which source to use based on overlay state
        var source = GetCharacterSource(useAltCharSet);

        if (source == null)
        {
            return 0x00; // No ROM loaded - return blank
        }

        int offset = GetCharacterOffset(charCode, useAltCharSet) + scanline;
        return source.AsReadOnlySpan()[offset];
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<byte> GetCharacterBitmap(byte charCode, bool useAltCharSet)
    {
        var source = GetCharacterSource(useAltCharSet);

        if (source == null)
        {
            return Memory<byte>.Empty;
        }

        int offset = GetCharacterOffset(charCode, useAltCharSet);
        return source.Slice((uint)offset, 8);
    }

    /// <inheritdoc />
    public Memory<byte> GetCharacterRomData()
    {
        if (characterRom == null)
        {
            return Memory<byte>.Empty;
        }

        return characterRom.Slice(0, CharacterRomSize);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetCharacterScanlineWithEffects(
        byte charCode,
        int scanline,
        bool useAltCharSet,
        bool flashState)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(scanline, 7);

        byte pixelData = GetCharacterScanline(charCode, scanline, useAltCharSet);

        // Apply flash effect if needed
        bool isFlashing = charCode >= 0x40 && charCode < 0x80;
        if (isFlashing)
        {
            // Check if flashing is disabled for the current bank
            bool noFlash = useAltCharSet ? noFlash2Enabled : noFlash1Enabled;
            if (!noFlash && flashState)
            {
                // Invert only the 7 pixel bits
                pixelData = (byte)(~pixelData & 0x7F);
            }
        }

        return pixelData;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetScanlineRow(
        ReadOnlySpan<byte> characterCodes,
        int scanline,
        bool useAltCharSet,
        bool flashState,
        Span<byte> outputBuffer)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(scanline, 7);

        if (outputBuffer.Length < characterCodes.Length)
        {
            throw new ArgumentException(
                $"Output buffer must be at least {characterCodes.Length} bytes.",
                nameof(outputBuffer));
        }

        for (int i = 0; i < characterCodes.Length; i++)
        {
            outputBuffer[i] = GetCharacterScanlineWithEffects(
                characterCodes[i],
                scanline,
                useAltCharSet,
                flashState);
        }
    }

    /// <summary>
    /// Called at VBLANK to process pending character ROM changes.
    /// </summary>
    /// <remarks>
    /// Character table switches should only take effect at VBLANK to prevent
    /// mid-frame rendering artifacts.
    /// </remarks>
    public void OnVBlank()
    {
        if (characterRomChangePending)
        {
            characterRomChangePending = false;
            CharacterRomChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<SoftSwitchState> GetSoftSwitchStates()
    {
#pragma warning disable SA1515 // Single-line comment should be preceded by blank line
        return
        [
            // ALTCHAR switch
            new("CLRALTCHAR", 0xC00E, !altCharSet, "Primary character set"),
            new("SETALTCHAR", 0xC00F, altCharSet, "Alternate character set"),
            new("RDALTCHAR", 0xC01E, altCharSet, "Alternate character set status"),

            // Glyph read/write control
            new("CLRGLYPHRD", 0xC042, !glyphReadEnabled, "Glyph RAM reading disabled"),
            new("SETGLYPHRD", 0xC043, glyphReadEnabled, "Glyph RAM reading enabled"),
            new("CLRGLYPHWRT", 0xC044, !glyphWriteEnabled, "Glyph RAM writing disabled"),
            new("SETGLYPHWRT", 0xC045, glyphWriteEnabled, "Glyph RAM writing enabled"),

            // Flash control
            new("CLRNOFLASH1", 0xC046, !noFlash1Enabled, "Flashing enabled for bank 1"),
            new("SETNOFLASH1", 0xC047, noFlash1Enabled, "Flashing disabled for bank 1"),
            new("CLRNOFLASH2", 0xC048, !noFlash2Enabled, "Flashing enabled for bank 2"),
            new("SETNOFLASH2", 0xC049, noFlash2Enabled, "Flashing disabled for bank 2"),

            // Glyph bank control
            new("CLRALTGLYPH1", 0xC04A, !altGlyph1Enabled, "Glyph bank 1 overlay disabled"),
            new("SETALTGLYPH1", 0xC04B, altGlyph1Enabled, "Glyph bank 1 overlay enabled"),
            new("CLRALTGLYPH2", 0xC04C, !altGlyph2Enabled, "Glyph bank 2 overlay disabled"),
            new("SETALTGLYPH2", 0xC04D, altGlyph2Enabled, "Glyph bank 2 overlay enabled"),

            // Status reads
            new("RDGLYPHRD", 0xC034, glyphReadEnabled, "Glyph read status"),
            new("RDGLYPHWRT", 0xC035, glyphWriteEnabled, "Glyph write status"),
            new("RDNOFLASH1", 0xC036, noFlash1Enabled, "No-flash bank 1 status"),
            new("RDNOFLASH2", 0xC037, noFlash2Enabled, "No-flash bank 2 status"),
            new("RDALTGLYPH1", 0xC038, altGlyph1Enabled, "Glyph bank 1 status"),
            new("RDALTGLYPH2", 0xC039, altGlyph2Enabled, "Glyph bank 2 status"),
        ];
#pragma warning restore SA1515 // Single-line comment should be preceded by blank line
    }

    /// <summary>
    /// Gets read-only access to glyph RAM.
    /// </summary>
    /// <returns>A read-only span of the glyph RAM data.</returns>
    public ReadOnlySpan<byte> GetGlyphRamData()
    {
        return glyphRam.AsReadOnlySpan();
    }

    /// <summary>
    /// Writes data to glyph RAM at the specified offset.
    /// </summary>
    /// <param name="offset">The offset within glyph RAM.</param>
    /// <param name="data">The data to write.</param>
    public void WriteGlyphRam(int offset, ReadOnlySpan<byte> data)
    {
        data.CopyTo(glyphRam.AsSpan().Slice(offset, data.Length));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IGlyphHotLoader Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public void HotLoadGlyphData(ReadOnlySpan<byte> glyphData, GlyphLoadTarget target)
    {
        if (glyphData.Length != CharacterRomSize)
        {
            throw new ArgumentException(
                $"Glyph data must be exactly {CharacterRomSize} bytes.",
                nameof(glyphData));
        }

        switch (target)
        {
            case GlyphLoadTarget.GlyphRom:
                if (characterRom == null)
                {
                    characterRom = new PhysicalMemory(CharacterRomSize, "CharacterROM");
                }

                glyphData.CopyTo(characterRom.AsSpan());
                break;

            case GlyphLoadTarget.GlyphRam:
                glyphData.CopyTo(glyphRam.AsSpan());
                break;
        }

        OnGlyphDataChanged(new GlyphDataChangedEventArgs
        {
            Target = target,
            CharacterCode = null,
            IsAlternateSet = false,
        });
    }

    /// <inheritdoc />
    public void HotLoadCharacter(
        byte charCode,
        ReadOnlySpan<byte> scanlines,
        bool useAltCharSet,
        GlyphLoadTarget target)
    {
        if (scanlines.Length < 8)
        {
            throw new ArgumentException(
                "Scanlines must contain at least 8 bytes.",
                nameof(scanlines));
        }

        int offset = (useAltCharSet ? CharacterSetSize : 0) + (charCode * 8);

        switch (target)
        {
            case GlyphLoadTarget.GlyphRom:
                if (characterRom == null)
                {
                    characterRom = new PhysicalMemory(CharacterRomSize, "CharacterROM");
                }

                scanlines[..8].CopyTo(characterRom.AsSpan()[offset..]);
                break;

            case GlyphLoadTarget.GlyphRam:
                scanlines[..8].CopyTo(glyphRam.AsSpan()[offset..]);
                break;
        }

        OnGlyphDataChanged(new GlyphDataChangedEventArgs
        {
            Target = target,
            CharacterCode = charCode,
            IsAlternateSet = useAltCharSet,
        });
    }

    /// <inheritdoc />
    public byte[] GetGlyphData(GlyphLoadTarget target)
    {
        var result = new byte[CharacterRomSize];

        var source = target == GlyphLoadTarget.GlyphRom ? characterRom : glyphRam;
        source?.AsReadOnlySpan().CopyTo(result);

        return result;
    }

    /// <summary>
    /// Gets the offset within the character data source for a character.
    /// </summary>
    /// <param name="charCode">The character code.</param>
    /// <param name="useAltCharSet">Whether to use the alternate character set.</param>
    /// <returns>The byte offset to the character's first scanline.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetCharacterOffset(byte charCode, bool useAltCharSet)
    {
        // Bank 1 = lower half ($0000-$07FF), Bank 2 = upper half ($0800-$0FFF)
        int baseOffset = useAltCharSet ? CharacterSetSize : 0;
        return baseOffset + (charCode * 8);
    }

    /// <summary>
    /// Gets the character data source (ROM or RAM) based on overlay settings.
    /// </summary>
    /// <param name="useAltCharSet">Whether to use the alternate character set.</param>
    /// <returns>The physical memory containing the character data, or null if unavailable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PhysicalMemory? GetCharacterSource(bool useAltCharSet)
    {
        // Check if we should use glyph RAM overlay
        // Bank 1 = lower half ($0000-$07FF), Bank 2 = upper half ($0800-$0FFF)
        if (!useAltCharSet && altGlyph1Enabled)
        {
            return glyphRam;
        }

        if (useAltCharSet && altGlyph2Enabled)
        {
            return glyphRam;
        }

        // Fall back to character ROM
        return characterRom;
    }

    // ALTCHAR switch handlers (write-only)
    private void HandleClrAltChar(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree && altCharSet)
        {
            altCharSet = false;
            characterRomChangePending = true;
        }
    }

    private void HandleSetAltChar(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree && !altCharSet)
        {
            altCharSet = true;
            characterRomChangePending = true;
        }
    }

    // Soft switch handlers for glyph read/write control (write-only)
    private void HandleClrGlyphRd(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphReadEnabled = false;
        }
    }

    private void HandleSetGlyphRd(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphReadEnabled = true;
        }
    }

    private void HandleClrGlyphWrt(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphWriteEnabled = false;
        }
    }

    private void HandleSetGlyphWrt(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphWriteEnabled = true;
        }
    }

    // Soft switch handlers for flash control (write-only)
    private void HandleClrNoFlash1(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash1Enabled = false;
        }
    }

    private void HandleSetNoFlash1(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash1Enabled = true;
        }
    }

    private void HandleClrNoFlash2(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash2Enabled = false;
        }
    }

    private void HandleSetNoFlash2(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash2Enabled = true;
        }
    }

    // Soft switch handlers for glyph bank control (write-only)
    private void HandleClrAltGlyph1(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree && altGlyph1Enabled)
        {
            altGlyph1Enabled = false;
            characterRomChangePending = true;
        }
    }

    private void HandleSetAltGlyph1(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree && !altGlyph1Enabled)
        {
            altGlyph1Enabled = true;
            characterRomChangePending = true;
        }
    }

    private void HandleClrAltGlyph2(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree && altGlyph2Enabled)
        {
            altGlyph2Enabled = false;
            characterRomChangePending = true;
        }
    }

    private void HandleSetAltGlyph2(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree && !altGlyph2Enabled)
        {
            altGlyph2Enabled = true;
            characterRomChangePending = true;
        }
    }

    // Status read handlers
    private byte ReadAltCharStatus(byte offset, in BusAccess context)
    {
        return altCharSet ? StatusBitSet : StatusBitClear;
    }

    private byte ReadGlyphRdStatus(byte offset, in BusAccess context)
    {
        return glyphReadEnabled ? StatusBitSet : StatusBitClear;
    }

    private byte ReadGlyphWrtStatus(byte offset, in BusAccess context)
    {
        return glyphWriteEnabled ? StatusBitSet : StatusBitClear;
    }

    private byte ReadNoFlash1Status(byte offset, in BusAccess context)
    {
        return noFlash1Enabled ? StatusBitSet : StatusBitClear;
    }

    private byte ReadNoFlash2Status(byte offset, in BusAccess context)
    {
        return noFlash2Enabled ? StatusBitSet : StatusBitClear;
    }

    private byte ReadAltGlyph1Status(byte offset, in BusAccess context)
    {
        return altGlyph1Enabled ? StatusBitSet : StatusBitClear;
    }

    private byte ReadAltGlyph2Status(byte offset, in BusAccess context)
    {
        return altGlyph2Enabled ? StatusBitSet : StatusBitClear;
    }

    private void OnGlyphDataChanged(GlyphDataChangedEventArgs e)
    {
        GlyphDataChanged?.Invoke(this, e);
    }
}