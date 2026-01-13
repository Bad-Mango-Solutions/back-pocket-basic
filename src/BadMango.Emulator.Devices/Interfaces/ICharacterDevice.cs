// <copyright file="ICharacterDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Character generator device interface for text rendering.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the API for the character generator device, which manages
/// character glyph ROM and RAM and provides character bitmap data to the video renderer.
/// </para>
/// <para>
/// The character device owns:
/// <list type="bullet">
/// <item><description>4KB character ROM with primary and alternate character sets</description></item>
/// <item><description>4KB glyph RAM for custom character overlays</description></item>
/// </list>
/// </para>
/// <para>
/// "Glyph Bank 1" and "Glyph Bank 2" refer to the lower ($0000-$07FF) and upper
/// ($0800-$0FFF) halves of either ROM or RAM. The ALTCHAR switch determines which
/// bank is used. ALTGLYPHx determines which ROM glyph bank to overlay with RAM.
/// </para>
/// <para>
/// Soft switches control glyph bank selection and flash behavior:
/// <list type="bullet">
/// <item><description>ALTCHAR - Select primary or alternate character set</description></item>
/// <item><description>ALTGLYPH1/ALTGLYPH2 - Enable glyph RAM bank overlays</description></item>
/// <item><description>NOFLASH1/NOFLASH2 - Disable flashing for glyph banks</description></item>
/// <item><description>GLYPHRD/GLYPHWRT - Control glyph RAM read/write access</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ICharacterDevice : IMotherboardDevice, ICharacterRomProvider
{
    /// <summary>
    /// Event raised when the character ROM configuration changes and should be reloaded.
    /// </summary>
    /// <remarks>
    /// This event is fired at VBLANK when character table switches occur, allowing
    /// the video window to safely reload its character ROM buffer without mid-frame
    /// rendering artifacts.
    /// </remarks>
    event Action? CharacterRomChanged;

    /// <summary>
    /// Gets a value indicating whether the alternate character set is active.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if alternate character set is selected via ALTCHAR;
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool IsAltCharSet { get; }

    /// <summary>
    /// Gets a value indicating whether glyph bank 1 overlay is enabled.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if glyph RAM overlays the primary character set (bank 1);
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool IsAltGlyph1Enabled { get; }

    /// <summary>
    /// Gets a value indicating whether glyph bank 2 overlay is enabled.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if glyph RAM overlays the alternate character set (bank 2);
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool IsAltGlyph2Enabled { get; }

    /// <summary>
    /// Gets a value indicating whether flashing is disabled for glyph bank 1.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if flashing is disabled for bank 1 characters ($40-$7F in primary);
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool IsNoFlash1Enabled { get; }

    /// <summary>
    /// Gets a value indicating whether flashing is disabled for glyph bank 2.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if flashing is disabled for bank 2 characters ($40-$7F in alternate);
    /// otherwise, <see langword="false"/> (default for bank 2).
    /// </value>
    bool IsNoFlash2Enabled { get; }

    /// <summary>
    /// Gets a value indicating whether glyph RAM reading is enabled.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if reads from the glyph window return glyph RAM data;
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool IsGlyphReadEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether glyph RAM writing is enabled.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if writes to the glyph window go to glyph RAM;
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool IsGlyphWriteEnabled { get; }

    /// <summary>
    /// Loads character ROM data into the character device.
    /// </summary>
    /// <param name="romData">
    /// The character ROM data to load. Must be exactly 4096 bytes (4KB)
    /// containing two 2KB character sets (primary and alternate).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="romData"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="romData"/> is not exactly 4096 bytes.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The character ROM is organized as two 2KB segments:
    /// </para>
    /// <list type="bullet">
    /// <item><description>$0000-$07FF: Primary character set (256 × 8 bytes)</description></item>
    /// <item><description>$0800-$0FFF: Alternate character set with MouseText (256 × 8 bytes)</description></item>
    /// </list>
    /// <para>
    /// Each character occupies 8 consecutive bytes, one per scanline from top to bottom.
    /// Each byte contains 7 pixel bits (bits 0-6), with bit 7 unused.
    /// </para>
    /// </remarks>
    void LoadCharacterRom(byte[] romData);

    /// <summary>
    /// Gets a full scanline row of pixels for an entire text row.
    /// </summary>
    /// <param name="characterCodes">
    /// The character codes for the row (typically 40 or 80 bytes).
    /// </param>
    /// <param name="scanline">The scanline within the character row (0-7).</param>
    /// <param name="useAltCharSet">
    /// <see langword="true"/> to use the alternate character set;
    /// <see langword="false"/> to use the primary character set.
    /// </param>
    /// <param name="flashState">
    /// <see langword="true"/> if flashing characters should show inverted;
    /// <see langword="false"/> for normal display.
    /// </param>
    /// <param name="outputBuffer">
    /// The buffer to receive scanline pixel data. Must be at least
    /// <paramref name="characterCodes"/>.Length bytes.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method generates pixel data for an entire row of characters at once,
    /// enabling efficient scanline-based rendering. Each character contributes
    /// one byte (7 pixels) to the output.
    /// </para>
    /// <para>
    /// The output buffer contains consecutive 7-bit pixel patterns, one byte per
    /// character. Bit 6 is the leftmost pixel, bit 0 is the rightmost.
    /// </para>
    /// </remarks>
    void GetScanlineRow(
        ReadOnlySpan<byte> characterCodes,
        int scanline,
        bool useAltCharSet,
        bool flashState,
        Span<byte> outputBuffer);

    /// <summary>
    /// Gets a single character's scanline with proper overlay and flash handling.
    /// </summary>
    /// <param name="charCode">The 8-bit character code (0-255).</param>
    /// <param name="scanline">The scanline within the character (0-7).</param>
    /// <param name="useAltCharSet">
    /// <see langword="true"/> to use the alternate character set;
    /// <see langword="false"/> to use the primary character set.
    /// </param>
    /// <param name="flashState">
    /// <see langword="true"/> if flashing characters should show inverted;
    /// <see langword="false"/> for normal display.
    /// </param>
    /// <returns>
    /// The scanline pixel data with overlay and flash effects applied.
    /// </returns>
    byte GetCharacterScanlineWithEffects(
        byte charCode,
        int scanline,
        bool useAltCharSet,
        bool flashState);

    /// <summary>
    /// Called at VBLANK to process pending character ROM changes.
    /// </summary>
    /// <remarks>
    /// Character table switches should only take effect at VBLANK to prevent
    /// mid-frame rendering artifacts.
    /// </remarks>
    void OnVBlank();
}