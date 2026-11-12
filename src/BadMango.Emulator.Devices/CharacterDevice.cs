// <copyright file="CharacterDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

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
/// Glyph RAM overlays, flash control, and glyph read/write gates are controlled
/// programmatically (hot-loader API and internal test hooks). They are intentionally
/// <strong>not</strong> exposed on $C034-$C039 / $C042-$C04D so those addresses stay
/// compatible with stock Apple IIe/IIgs I/O (open bus / floating $FF on read).
/// </para>
/// <para>
/// The CharacterDevice owns the standard ALTCHAR switch:
/// </para>
/// <list type="bullet">
/// <item><description>$C00E: CLRALTCHAR - Select primary character set</description></item>
/// <item><description>$C00F: SETALTCHAR - Select alternate character set</description></item>
/// <item><description>$C01E: RDALTCHAR - Read alternate character set status</description></item>
/// </list>
/// </remarks>
[DeviceType("character")]
public sealed class CharacterDevice : ICharacterDevice, ISoftSwitchProvider, IGlyphHotLoader, IResetReceiver
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

        // Status read ($C01E for ALTCHAR only — $C034-$C039 left unhandled for IIe compat)
        dispatcher.RegisterRead(0x1E, ReadAltCharStatus);    // RDALTCHAR
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
    /// <remarks>
    /// ALTCHRSET is an IOU switch, and the IOU is reset by RESET' (Sather, <i>Understanding the
    /// Apple IIe</i>, Table 7.1, p. 7-5: ALTCHRSET off after RESET'). The glyph RAM and flash
    /// controls are emulator extensions with no hardware RESET behavior, so they are left alone.
    /// </remarks>
    public void OnResetAsserted(Core.Cycle cycle)
    {
        altCharSet = false;
    }

    /// <inheritdoc />
    public void OnResetReleased(Core.Cycle cycle)
    {
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

    // Status read handler
    private byte ReadAltCharStatus(byte offset, in BusAccess context)
    {
        return altCharSet ? StatusBitSet : StatusBitClear;
    }

    /// <summary>Programmatically enables or disables glyph bank 1 overlay (tests / tools).</summary>
    internal void SetAltGlyph1Enabled(bool enabled)
    {
        if (altGlyph1Enabled != enabled)
        {
            altGlyph1Enabled = enabled;
            characterRomChangePending = true;
        }
    }

    /// <summary>Programmatically enables or disables glyph bank 2 overlay (tests / tools).</summary>
    internal void SetAltGlyph2Enabled(bool enabled)
    {
        if (altGlyph2Enabled != enabled)
        {
            altGlyph2Enabled = enabled;
            characterRomChangePending = true;
        }
    }

    /// <summary>Programmatically enables or disables no-flash for glyph bank 1 (tests / tools).</summary>
    internal void SetNoFlash1Enabled(bool enabled) => noFlash1Enabled = enabled;

    /// <summary>Programmatically enables or disables no-flash for glyph bank 2 (tests / tools).</summary>
    internal void SetNoFlash2Enabled(bool enabled) => noFlash2Enabled = enabled;

    /// <summary>Programmatically enables or disables glyph RAM read gate (tests / tools).</summary>
    internal void SetGlyphReadEnabled(bool enabled) => glyphReadEnabled = enabled;

    /// <summary>Programmatically enables or disables glyph RAM write gate (tests / tools).</summary>
    internal void SetGlyphWriteEnabled(bool enabled) => glyphWriteEnabled = enabled;

    private void OnGlyphDataChanged(GlyphDataChangedEventArgs e)
    {
        GlyphDataChanged?.Invoke(this, e);
    }
}