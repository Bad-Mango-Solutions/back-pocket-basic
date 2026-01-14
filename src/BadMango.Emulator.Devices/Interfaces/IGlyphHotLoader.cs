// <copyright file="IGlyphHotLoader.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Interface for devices that support hot-loading of glyph data from external tools.
/// </summary>
/// <remarks>
/// <para>
/// This interface is separate from <see cref="ICharacterRomProvider"/> to keep the
/// runtime rendering interface clean. It is implemented by <c>CharacterDevice</c>
/// to support hot-loading from the glyph editor.
/// </para>
/// </remarks>
public interface IGlyphHotLoader
{
    /// <summary>
    /// Event raised when character ROM/RAM data changes.
    /// The video renderer should subscribe to this to refresh the glyph cache.
    /// </summary>
    event EventHandler<GlyphDataChangedEventArgs>? GlyphDataChanged;

    /// <summary>
    /// Hot-loads a complete 4KB glyph file into the device.
    /// </summary>
    /// <param name="glyphData">4KB glyph data (two 2KB character sets).</param>
    /// <param name="target">Whether to load into ROM (permanent) or RAM (overlay).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="glyphData"/> is not exactly 4096 bytes.
    /// </exception>
    void HotLoadGlyphData(ReadOnlySpan<byte> glyphData, GlyphLoadTarget target);

    /// <summary>
    /// Hot-loads a single character's bitmap.
    /// </summary>
    /// <param name="charCode">Character code (0-255).</param>
    /// <param name="scanlines">8 bytes of scanline data.</param>
    /// <param name="useAltCharSet">Target primary or alternate set.</param>
    /// <param name="target">Whether to load into ROM or RAM.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="scanlines"/> contains fewer than 8 bytes.
    /// </exception>
    void HotLoadCharacter(
        byte charCode,
        ReadOnlySpan<byte> scanlines,
        bool useAltCharSet,
        GlyphLoadTarget target);

    /// <summary>
    /// Gets the current glyph data for the editor to read back.
    /// </summary>
    /// <param name="target">Whether to read from ROM or RAM.</param>
    /// <returns>4KB glyph data.</returns>
    byte[] GetGlyphData(GlyphLoadTarget target);
}