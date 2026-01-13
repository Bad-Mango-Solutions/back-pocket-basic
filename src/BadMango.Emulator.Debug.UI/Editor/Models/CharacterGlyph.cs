// <copyright file="CharacterGlyph.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.Models;

/// <summary>
/// Represents a single character glyph (8 bytes, 7×8 pixels).
/// </summary>
/// <remarks>
/// <para>
/// Each character occupies 8 consecutive bytes (8 scanlines, top to bottom).
/// Each byte contains 7 pixels in bits 0-6; bit 7 is unused.
/// Bit 6 is the leftmost pixel, bit 0 is the rightmost.
/// </para>
/// </remarks>
public sealed class CharacterGlyph
{
    /// <summary>
    /// The width of a character glyph in pixels.
    /// </summary>
    public const int Width = 7;

    /// <summary>
    /// The height of a character glyph in pixels (scanlines).
    /// </summary>
    public const int Height = 8;

    /// <summary>
    /// Gets the 8 scanlines of the glyph, from top (index 0) to bottom (index 7).
    /// Each byte contains 7 pixels in bits 0-6; bit 7 is unused.
    /// </summary>
    public byte[] Scanlines { get; } = new byte[Height];

    /// <summary>
    /// Gets or sets a pixel value.
    /// </summary>
    /// <param name="x">X coordinate (0-6, where 0 is leftmost).</param>
    /// <param name="y">Y coordinate (0-7, where 0 is topmost).</param>
    /// <returns>True if pixel is set, false if clear.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="x"/> is not in range 0-6 or
    /// <paramref name="y"/> is not in range 0-7.
    /// </exception>
    public bool this[int x, int y]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(x, Width - 1);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(y, Height - 1);

            // Bit 6 is leftmost pixel, bit 0 is rightmost
            int bit = (Width - 1) - x;
            return (Scanlines[y] & (1 << bit)) != 0;
        }

        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(x, Width - 1);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(y, Height - 1);

            int bit = (Width - 1) - x;
            if (value)
            {
                Scanlines[y] |= (byte)(1 << bit);
            }
            else
            {
                Scanlines[y] &= (byte)~(1 << bit);
            }
        }
    }

    /// <summary>
    /// Creates a deep copy of this glyph.
    /// </summary>
    /// <returns>A new <see cref="CharacterGlyph"/> with the same pixel data.</returns>
    public CharacterGlyph Clone()
    {
        var clone = new CharacterGlyph();
        Array.Copy(Scanlines, clone.Scanlines, Height);
        return clone;
    }

    /// <summary>
    /// Copies scanline data from a byte array.
    /// </summary>
    /// <param name="source">The source data to copy from.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="source"/> contains fewer than 8 bytes.
    /// </exception>
    public void CopyFrom(ReadOnlySpan<byte> source)
    {
        if (source.Length < Height)
        {
            throw new ArgumentException(
                $"Source must contain at least {Height} bytes.",
                nameof(source));
        }

        source[..Height].CopyTo(Scanlines);
    }

    /// <summary>
    /// Copies scanline data to a byte array.
    /// </summary>
    /// <param name="destination">The destination to copy to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination"/> has capacity for fewer than 8 bytes.
    /// </exception>
    public void CopyTo(Span<byte> destination)
    {
        if (destination.Length < Height)
        {
            throw new ArgumentException(
                $"Destination must have capacity for {Height} bytes.",
                nameof(destination));
        }

        Scanlines.CopyTo(destination);
    }

    /// <summary>
    /// Clears all pixels in the glyph.
    /// </summary>
    public void Clear()
    {
        Array.Clear(Scanlines);
    }

    /// <summary>
    /// Sets all pixels in the glyph.
    /// </summary>
    public void Fill()
    {
        Array.Fill(Scanlines, (byte)0x7F);
    }

    /// <summary>
    /// Inverts all pixels in the glyph.
    /// </summary>
    public void Invert()
    {
        for (int y = 0; y < Height; y++)
        {
            Scanlines[y] = (byte)(~Scanlines[y] & 0x7F);
        }
    }
}