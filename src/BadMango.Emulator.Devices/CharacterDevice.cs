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
/// <item><description>Two 4KB glyph RAM banks for custom character overlays</description></item>
/// </list>
/// </para>
/// <para>
/// Soft switches control various aspects of character rendering:
/// </para>
/// <list type="bullet">
/// <item><description>$C068: ALTGLYPH1OFF - Disable glyph bank 1 overlay</description></item>
/// <item><description>$C069: ALTGLYPH1ON - Enable glyph bank 1 overlay</description></item>
/// <item><description>$C06A: ALTGLYPH2OFF - Disable glyph bank 2 overlay</description></item>
/// <item><description>$C06B: ALTGLYPH2ON - Enable glyph bank 2 overlay</description></item>
/// <item><description>$C024: RDALTGLYPH1 - Read glyph bank 1 status</description></item>
/// <item><description>$C025: RDALTGLYPH2 - Read glyph bank 2 status</description></item>
/// <item><description>$C064: NOFLASH1OFF - Enable flashing for bank 1</description></item>
/// <item><description>$C065: NOFLASH1ON - Disable flashing for bank 1</description></item>
/// <item><description>$C066: NOFLASH2OFF - Enable flashing for bank 2</description></item>
/// <item><description>$C067: NOFLASH2ON - Disable flashing for bank 2</description></item>
/// <item><description>$C026: RDNOFLASH1 - Read no-flash bank 1 status</description></item>
/// <item><description>$C027: RDNOFLASH2 - Read no-flash bank 2 status</description></item>
/// <item><description>$C060: RDGLYPHOFF - Disable glyph RAM reading</description></item>
/// <item><description>$C061: RDGLYPHON - Enable glyph RAM reading</description></item>
/// <item><description>$C062: WRTGLYPHOFF - Disable glyph RAM writing</description></item>
/// <item><description>$C063: WRTGLYPHON - Enable glyph RAM writing</description></item>
/// <item><description>$C028: RDGLYPHRD - Read glyph read status</description></item>
/// <item><description>$C029: RDGLYPHWR - Read glyph write status</description></item>
/// </list>
/// </remarks>
[DeviceType("character")]
public sealed class CharacterDevice : ICharacterDevice, ISoftSwitchProvider
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
    /// Size of each glyph RAM bank: 4KB.
    /// </summary>
    public const int GlyphBankSize = 4096;

    private const byte StatusBitSet = 0x80;
    private const byte StatusBitClear = 0x00;

    private readonly PhysicalMemory glyphBank1;
    private readonly PhysicalMemory glyphBank2;

    private PhysicalMemory? characterRom;

    private bool altGlyph1Enabled;
    private bool altGlyph2Enabled;
    private bool noFlash1Enabled;
    private bool noFlash2Enabled = true; // Bank 2 defaults to NOFLASH
    private bool glyphReadEnabled;
    private bool glyphWriteEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterDevice"/> class.
    /// </summary>
    public CharacterDevice()
    {
        // Create glyph RAM banks (4KB each)
        glyphBank1 = new PhysicalMemory(GlyphBankSize, "GlyphBank1");
        glyphBank2 = new PhysicalMemory(GlyphBankSize, "GlyphBank2");
    }

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

        // Glyph read/write control ($C060-$C063)
        dispatcher.Register(0x60, HandleRdGlyphOff, HandleRdGlyphOffWrite);       // RDGLYPHOFF
        dispatcher.Register(0x61, HandleRdGlyphOn, HandleRdGlyphOnWrite);         // RDGLYPHON
        dispatcher.Register(0x62, HandleWrtGlyphOff, HandleWrtGlyphOffWrite);     // WRTGLYPHOFF
        dispatcher.Register(0x63, HandleWrtGlyphOn, HandleWrtGlyphOnWrite);       // WRTGLYPHON

        // Flash control ($C064-$C067)
        dispatcher.Register(0x64, HandleNoFlash1Off, HandleNoFlash1OffWrite);     // NOFLASH1OFF
        dispatcher.Register(0x65, HandleNoFlash1On, HandleNoFlash1OnWrite);       // NOFLASH1ON
        dispatcher.Register(0x66, HandleNoFlash2Off, HandleNoFlash2OffWrite);     // NOFLASH2OFF
        dispatcher.Register(0x67, HandleNoFlash2On, HandleNoFlash2OnWrite);       // NOFLASH2ON

        // Glyph bank control ($C068-$C06B)
        dispatcher.Register(0x68, HandleAltGlyph1Off, HandleAltGlyph1OffWrite);   // ALTGLYPH1OFF
        dispatcher.Register(0x69, HandleAltGlyph1On, HandleAltGlyph1OnWrite);     // ALTGLYPH1ON
        dispatcher.Register(0x6A, HandleAltGlyph2Off, HandleAltGlyph2OffWrite);   // ALTGLYPH2OFF
        dispatcher.Register(0x6B, HandleAltGlyph2On, HandleAltGlyph2OnWrite);     // ALTGLYPH2ON

        // Status reads ($C024-$C029)
        dispatcher.RegisterRead(0x24, ReadAltGlyph1Status);   // RDALTGLYPH1
        dispatcher.RegisterRead(0x25, ReadAltGlyph2Status);   // RDALTGLYPH2
        dispatcher.RegisterRead(0x26, ReadNoFlash1Status);    // RDNOFLASH1
        dispatcher.RegisterRead(0x27, ReadNoFlash2Status);    // RDNOFLASH2
        dispatcher.RegisterRead(0x28, ReadGlyphRdStatus);     // RDGLYPHRD
        dispatcher.RegisterRead(0x29, ReadGlyphWrStatus);     // RDGLYPHWR
    }

    /// <inheritdoc />
    public void Reset()
    {
        altGlyph1Enabled = false;
        altGlyph2Enabled = false;
        noFlash1Enabled = false;
        noFlash2Enabled = true; // Bank 2 defaults to NOFLASH
        glyphReadEnabled = false;
        glyphWriteEnabled = false;
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
        var source = GetCharacterSource(charCode, useAltCharSet);

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
        var source = GetCharacterSource(charCode, useAltCharSet);

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

    /// <inheritdoc />
    public IReadOnlyList<SoftSwitchState> GetSoftSwitchStates()
    {
#pragma warning disable SA1515 // Single-line comment should be preceded by blank line
        return
        [
            // Glyph read/write control
            new("RDGLYPHOFF", 0xC060, !glyphReadEnabled, "Glyph RAM reading disabled"),
            new("RDGLYPHON", 0xC061, glyphReadEnabled, "Glyph RAM reading enabled"),
            new("WRTGLYPHOFF", 0xC062, !glyphWriteEnabled, "Glyph RAM writing disabled"),
            new("WRTGLYPHON", 0xC063, glyphWriteEnabled, "Glyph RAM writing enabled"),

            // Flash control
            new("NOFLASH1OFF", 0xC064, !noFlash1Enabled, "Flashing enabled for bank 1"),
            new("NOFLASH1ON", 0xC065, noFlash1Enabled, "Flashing disabled for bank 1"),
            new("NOFLASH2OFF", 0xC066, !noFlash2Enabled, "Flashing enabled for bank 2"),
            new("NOFLASH2ON", 0xC067, noFlash2Enabled, "Flashing disabled for bank 2"),

            // Glyph bank control
            new("ALTGLYPH1OFF", 0xC068, !altGlyph1Enabled, "Glyph bank 1 overlay disabled"),
            new("ALTGLYPH1ON", 0xC069, altGlyph1Enabled, "Glyph bank 1 overlay enabled"),
            new("ALTGLYPH2OFF", 0xC06A, !altGlyph2Enabled, "Glyph bank 2 overlay disabled"),
            new("ALTGLYPH2ON", 0xC06B, altGlyph2Enabled, "Glyph bank 2 overlay enabled"),

            // Status reads
            new("RDALTGLYPH1", 0xC024, altGlyph1Enabled, "Glyph bank 1 status"),
            new("RDALTGLYPH2", 0xC025, altGlyph2Enabled, "Glyph bank 2 status"),
            new("RDNOFLASH1", 0xC026, noFlash1Enabled, "No-flash bank 1 status"),
            new("RDNOFLASH2", 0xC027, noFlash2Enabled, "No-flash bank 2 status"),
            new("RDGLYPHRD", 0xC028, glyphReadEnabled, "Glyph read status"),
            new("RDGLYPHWR", 0xC029, glyphWriteEnabled, "Glyph write status"),
        ];
#pragma warning restore SA1515 // Single-line comment should be preceded by blank line
    }

    /// <summary>
    /// Gets read-only access to glyph bank 1 RAM.
    /// </summary>
    /// <returns>A read-only span of the glyph bank 1 data.</returns>
    public ReadOnlySpan<byte> GetGlyphBank1Data()
    {
        return glyphBank1.AsReadOnlySpan();
    }

    /// <summary>
    /// Gets read-only access to glyph bank 2 RAM.
    /// </summary>
    /// <returns>A read-only span of the glyph bank 2 data.</returns>
    public ReadOnlySpan<byte> GetGlyphBank2Data()
    {
        return glyphBank2.AsReadOnlySpan();
    }

    /// <summary>
    /// Writes data to glyph bank 1 RAM at the specified offset.
    /// </summary>
    /// <param name="offset">The offset within glyph bank 1.</param>
    /// <param name="data">The data to write.</param>
    public void WriteGlyphBank1(int offset, ReadOnlySpan<byte> data)
    {
        data.CopyTo(glyphBank1.AsSpan().Slice(offset, data.Length));
    }

    /// <summary>
    /// Writes data to glyph bank 2 RAM at the specified offset.
    /// </summary>
    /// <param name="offset">The offset within glyph bank 2.</param>
    /// <param name="data">The data to write.</param>
    public void WriteGlyphBank2(int offset, ReadOnlySpan<byte> data)
    {
        data.CopyTo(glyphBank2.AsSpan().Slice(offset, data.Length));
    }

    /// <summary>
    /// Gets the character data source (ROM or RAM) based on overlay settings.
    /// </summary>
    /// <param name="charCode">The character code.</param>
    /// <param name="useAltCharSet">Whether to use the alternate character set.</param>
    /// <returns>The physical memory containing the character data, or null if unavailable.</returns>
    private PhysicalMemory? GetCharacterSource(byte charCode, bool useAltCharSet)
    {
        // Check if we should use glyph RAM overlay
        if (useAltCharSet && altGlyph2Enabled)
        {
            return glyphBank2;
        }

        if (!useAltCharSet && altGlyph1Enabled)
        {
            return glyphBank1;
        }

        // Fall back to character ROM
        return characterRom;
    }

    /// <summary>
    /// Gets the offset within the character data source for a character.
    /// </summary>
    /// <param name="charCode">The character code.</param>
    /// <param name="useAltCharSet">Whether to use the alternate character set.</param>
    /// <returns>The byte offset to the character's first scanline.</returns>
    private int GetCharacterOffset(byte charCode, bool useAltCharSet)
    {
        // When using glyph RAM banks, the offset is always from the start of the bank
        if ((useAltCharSet && altGlyph2Enabled) || (!useAltCharSet && altGlyph1Enabled))
        {
            return charCode * 8;
        }

        // For ROM, apply the character set offset
        int baseOffset = useAltCharSet ? CharacterSetSize : 0;
        return baseOffset + (charCode * 8);
    }

    // Soft switch handlers for glyph read/write control
    private byte HandleRdGlyphOff(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphReadEnabled = false;
        }

        return 0xFF;
    }

    private void HandleRdGlyphOffWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphReadEnabled = false;
        }
    }

    private byte HandleRdGlyphOn(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphReadEnabled = true;
        }

        return 0xFF;
    }

    private void HandleRdGlyphOnWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphReadEnabled = true;
        }
    }

    private byte HandleWrtGlyphOff(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphWriteEnabled = false;
        }

        return 0xFF;
    }

    private void HandleWrtGlyphOffWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphWriteEnabled = false;
        }
    }

    private byte HandleWrtGlyphOn(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphWriteEnabled = true;
        }

        return 0xFF;
    }

    private void HandleWrtGlyphOnWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            glyphWriteEnabled = true;
        }
    }

    // Soft switch handlers for flash control
    private byte HandleNoFlash1Off(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash1Enabled = false;
        }

        return 0xFF;
    }

    private void HandleNoFlash1OffWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash1Enabled = false;
        }
    }

    private byte HandleNoFlash1On(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash1Enabled = true;
        }

        return 0xFF;
    }

    private void HandleNoFlash1OnWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash1Enabled = true;
        }
    }

    private byte HandleNoFlash2Off(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash2Enabled = false;
        }

        return 0xFF;
    }

    private void HandleNoFlash2OffWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash2Enabled = false;
        }
    }

    private byte HandleNoFlash2On(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash2Enabled = true;
        }

        return 0xFF;
    }

    private void HandleNoFlash2OnWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            noFlash2Enabled = true;
        }
    }

    // Soft switch handlers for glyph bank control
    private byte HandleAltGlyph1Off(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            altGlyph1Enabled = false;
        }

        return 0xFF;
    }

    private void HandleAltGlyph1OffWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            altGlyph1Enabled = false;
        }
    }

    private byte HandleAltGlyph1On(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            altGlyph1Enabled = true;
        }

        return 0xFF;
    }

    private void HandleAltGlyph1OnWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            altGlyph1Enabled = true;
        }
    }

    private byte HandleAltGlyph2Off(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            altGlyph2Enabled = false;
        }

        return 0xFF;
    }

    private void HandleAltGlyph2OffWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            altGlyph2Enabled = false;
        }
    }

    private byte HandleAltGlyph2On(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            altGlyph2Enabled = true;
        }

        return 0xFF;
    }

    private void HandleAltGlyph2OnWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            altGlyph2Enabled = true;
        }
    }

    // Status read handlers
    private byte ReadAltGlyph1Status(byte offset, in BusAccess context)
    {
        return altGlyph1Enabled ? StatusBitSet : StatusBitClear;
    }

    private byte ReadAltGlyph2Status(byte offset, in BusAccess context)
    {
        return altGlyph2Enabled ? StatusBitSet : StatusBitClear;
    }

    private byte ReadNoFlash1Status(byte offset, in BusAccess context)
    {
        return noFlash1Enabled ? StatusBitSet : StatusBitClear;
    }

    private byte ReadNoFlash2Status(byte offset, in BusAccess context)
    {
        return noFlash2Enabled ? StatusBitSet : StatusBitClear;
    }

    private byte ReadGlyphRdStatus(byte offset, in BusAccess context)
    {
        return glyphReadEnabled ? StatusBitSet : StatusBitClear;
    }

    private byte ReadGlyphWrStatus(byte offset, in BusAccess context)
    {
        return glyphWriteEnabled ? StatusBitSet : StatusBitClear;
    }
}