// <copyright file="CharacterRenderer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

using System.Runtime.CompilerServices;

/// <summary>
/// Provides methods for rendering Apple II character ROM data to pixel buffers.
/// </summary>
/// <remarks>
/// <para>
/// The Apple II character generator uses a 7x8 pixel format where each character
/// occupies 8 bytes in ROM (one byte per scanline). Only the lower 7 bits of each
/// byte are used for pixel data, with bit 6 being the leftmost pixel.
/// </para>
/// <para>
/// Character codes are mapped as follows:
/// <list type="bullet">
/// <item><description>$00-$3F: Inverse characters</description></item>
/// <item><description>$40-$7F: Flashing characters</description></item>
/// <item><description>$80-$BF: Normal characters (uppercase, punctuation)</description></item>
/// <item><description>$C0-$FF: Normal characters (lowercase, symbols)</description></item>
/// </list>
/// </para>
/// </remarks>
public static class CharacterRenderer
{
    /// <summary>
    /// The width of a character in pixels.
    /// </summary>
    public const int CharacterWidth = 7;

    /// <summary>
    /// The height of a character in pixels (scanlines).
    /// </summary>
    public const int CharacterHeight = 8;

    /// <summary>
    /// The number of bytes per character in ROM.
    /// </summary>
    public const int BytesPerCharacter = 8;

    /// <summary>
    /// Renders a character from ROM data to a pixel buffer at the specified position.
    /// </summary>
    /// <param name="pixels">The pixel buffer span to render into.</param>
    /// <param name="stride">The stride (width) of the pixel buffer.</param>
    /// <param name="romData">The character ROM data.</param>
    /// <param name="charCode">The character code (0-255) to render.</param>
    /// <param name="romOffset">The base offset in ROM for the character set (e.g., 0x0000 or 0x0800).</param>
    /// <param name="destX">The destination X coordinate in the pixel buffer.</param>
    /// <param name="destY">The destination Y coordinate in the pixel buffer.</param>
    /// <param name="foregroundColor">The BGRA color for set pixels.</param>
    /// <returns><see langword="true"/> if the character was rendered successfully; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RenderCharacter(
        Span<uint> pixels,
        int stride,
        ReadOnlySpan<byte> romData,
        int charCode,
        int romOffset,
        int destX,
        int destY,
        uint foregroundColor)
    {
        return RenderCharacterScaled(
            pixels,
            stride,
            romData,
            charCode,
            romOffset,
            destX,
            destY,
            foregroundColor,
            scale: 1);
    }

    /// <summary>
    /// Renders a character from ROM data to a pixel buffer at the specified position with scaling.
    /// </summary>
    /// <param name="pixels">The pixel buffer span to render into.</param>
    /// <param name="stride">The stride (width) of the pixel buffer.</param>
    /// <param name="romData">The character ROM data.</param>
    /// <param name="charCode">The character code (0-255) to render.</param>
    /// <param name="romOffset">The base offset in ROM for the character set (e.g., 0x0000 or 0x0800).</param>
    /// <param name="destX">The destination X coordinate in the pixel buffer.</param>
    /// <param name="destY">The destination Y coordinate in the pixel buffer.</param>
    /// <param name="foregroundColor">The BGRA color for set pixels.</param>
    /// <param name="scale">The scale factor (1 = 1:1, 2 = 2x, etc.).</param>
    /// <returns><see langword="true"/> if the character was rendered successfully; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RenderCharacterScaled(
        Span<uint> pixels,
        int stride,
        ReadOnlySpan<byte> romData,
        int charCode,
        int romOffset,
        int destX,
        int destY,
        uint foregroundColor,
        int scale)
    {
        // Validate ROM offset and bounds
        int characterOffset = romOffset + (charCode * BytesPerCharacter);
        if (characterOffset < 0 || characterOffset + CharacterHeight > romData.Length)
        {
            return false;
        }

        // Render each scanline of the character
        for (int scanline = 0; scanline < CharacterHeight; scanline++)
        {
            byte scanlineData = romData[characterOffset + scanline];
            RenderScanline(pixels, stride, scanlineData, destX, destY + (scanline * scale), foregroundColor, scale);
        }

        return true;
    }

    /// <summary>
    /// Renders a single scanline of character data to a pixel buffer.
    /// </summary>
    /// <param name="pixels">The pixel buffer span to render into.</param>
    /// <param name="stride">The stride (width) of the pixel buffer.</param>
    /// <param name="scanlineData">The byte containing the scanline pixel data.</param>
    /// <param name="destX">The destination X coordinate in the pixel buffer.</param>
    /// <param name="destY">The destination Y coordinate in the pixel buffer.</param>
    /// <param name="foregroundColor">The BGRA color for set pixels.</param>
    /// <param name="scale">The scale factor (1 = 1:1, 2 = 2x, etc.).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RenderScanline(
        Span<uint> pixels,
        int stride,
        byte scanlineData,
        int destX,
        int destY,
        uint foregroundColor,
        int scale)
    {
        for (int pixel = 0; pixel < CharacterWidth; pixel++)
        {
            // Bit 6 is leftmost, bit 0 is rightmost
            bool isSet = (scanlineData & (1 << (6 - pixel))) != 0;

            if (isSet)
            {
                ScaledPixelWriter.WriteScaledPixel(
                    pixels,
                    stride,
                    destX + (pixel * scale),
                    destY,
                    foregroundColor,
                    scale);
            }
        }
    }

    /// <summary>
    /// Gets a human-readable display string for a character code.
    /// </summary>
    /// <param name="charCode">The character code (0-255).</param>
    /// <returns>A string describing the character and its display mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetCharacterDisplayString(int charCode)
    {
        return charCode switch
        {
            < 0x20 => $"'{(char)(charCode + 0x40)}' Inverse",
            < 0x40 => $"'{(char)charCode}' Inverse",
            < 0x60 => $"'{(char)charCode}' Flashing",
            < 0x80 => $"'{(char)(charCode - 0x40)}' Flashing",
            < 0xA0 => $"Control-'{(char)(charCode - 0x40)}'",
            _ => $"'{(char)(charCode - 0x80)}'",
        };
    }
}