// <copyright file="ICharacterRomProvider.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Provides access to character bitmap data for rendering.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the API for accessing character generator ROM data
/// used by video renderers to draw text characters. The character data is
/// organized as 8-byte bitmaps, with each byte representing one scanline of
/// a character.
/// </para>
/// <para>
/// The Apple IIe supports two character sets: a primary set and an alternate
/// set that includes MouseText glyphs. The <c>useAltCharSet</c> parameter
/// on the access methods controls which set is accessed.
/// </para>
/// </remarks>
public interface ICharacterRomProvider
{
    /// <summary>
    /// Gets a value indicating whether character ROM is loaded and available.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if character ROM data is loaded and available
    /// for rendering; otherwise, <see langword="false"/>.
    /// </value>
    bool IsCharacterRomLoaded { get; }

    /// <summary>
    /// Gets one scanline (row) of pixels for a character.
    /// </summary>
    /// <param name="charCode">The 8-bit character code (0-255).</param>
    /// <param name="scanline">The scanline within the character (0-7).</param>
    /// <param name="useAltCharSet">
    /// <see langword="true"/> to use the alternate character set;
    /// <see langword="false"/> to use the primary character set.
    /// </param>
    /// <returns>
    /// 7 bits representing pixels. Bit 6 is the leftmost pixel,
    /// bit 0 is the rightmost pixel.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="scanline"/> is greater than 7.
    /// </exception>
    byte GetCharacterScanline(byte charCode, int scanline, bool useAltCharSet);

    /// <summary>
    /// Gets all 8 scanlines for a character.
    /// </summary>
    /// <param name="charCode">The 8-bit character code (0-255).</param>
    /// <param name="useAltCharSet">
    /// <see langword="true"/> to use the alternate character set;
    /// <see langword="false"/> to use the primary character set.
    /// </param>
    /// <returns>
    /// 8 bytes representing the character bitmap, or an empty memory region if
    /// no character ROM is loaded.
    /// </returns>
    Memory<byte> GetCharacterBitmap(byte charCode, bool useAltCharSet);
}