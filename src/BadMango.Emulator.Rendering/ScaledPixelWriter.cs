// <copyright file="ScaledPixelWriter.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

using System.Runtime.CompilerServices;

// ReSharper disable once GrammarMistakeInComment

/// <summary>
/// Provides methods for writing scaled pixels to pixel buffers.
/// </summary>
/// <remarks>
/// This class handles the common pattern of rendering scaled pixels where a single
/// logical pixel is expanded to fill a square of physical pixels based on the scale factor.
/// </remarks>
public static class ScaledPixelWriter
{
    /// <summary>
    /// Writes a scaled pixel to the pixel buffer.
    /// </summary>
    /// <param name="pixels">The pixel buffer span to write to.</param>
    /// <param name="stride">The stride (width) of the pixel buffer.</param>
    /// <param name="x">The X coordinate of the top-left corner of the scaled pixel.</param>
    /// <param name="y">The Y coordinate of the top-left corner of the scaled pixel.</param>
    /// <param name="color">The BGRA color value.</param>
    /// <param name="scale">The scale factor (1 = 1x1, 2 = 2x2, etc.).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteScaledPixel(
        Span<uint> pixels,
        int stride,
        int x,
        int y,
        uint color,
        int scale)
    {
        for (int sy = 0; sy < scale; sy++)
        {
            for (int sx = 0; sx < scale; sx++)
            {
                int px = x + sx;
                int py = y + sy;
                pixels[(py * stride) + px] = color;
            }
        }
    }

    /// <summary>
    /// Writes a scaled pixel to the pixel buffer with bounds checking.
    /// </summary>
    /// <param name="pixels">The pixel buffer span to write to.</param>
    /// <param name="stride">The stride (width) of the pixel buffer.</param>
    /// <param name="bufferHeight">The height of the pixel buffer.</param>
    /// <param name="x">The X coordinate of the top-left corner of the scaled pixel.</param>
    /// <param name="y">The Y coordinate of the top-left corner of the scaled pixel.</param>
    /// <param name="color">The BGRA color value.</param>
    /// <param name="scale">The scale factor (1 = 1x1, 2 = 2x2, etc.).</param>
    /// <returns>
    /// <see langword="true"/> if at least one pixel was written; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WriteScaledPixelSafe(
        Span<uint> pixels,
        int stride,
        int bufferHeight,
        int x,
        int y,
        uint color,
        int scale)
    {
        bool anyWritten = false;

        for (int sy = 0; sy < scale; sy++)
        {
            for (int sx = 0; sx < scale; sx++)
            {
                int px = x + sx;
                int py = y + sy;

                if (px >= 0 && px < stride && py >= 0 && py < bufferHeight)
                {
                    pixels[(py * stride) + px] = color;
                    anyWritten = true;
                }
            }
        }

        return anyWritten;
    }

    /// <summary>
    /// Fills a rectangular region with a color.
    /// </summary>
    /// <param name="pixels">The pixel buffer span to write to.</param>
    /// <param name="stride">The stride (width) of the pixel buffer.</param>
    /// <param name="x">The X coordinate of the top-left corner.</param>
    /// <param name="y">The Y coordinate of the top-left corner.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="color">The BGRA color value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillRectangle(
        Span<uint> pixels,
        int stride,
        int x,
        int y,
        int width,
        int height,
        uint color)
    {
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                int px = x + col;
                int py = y + row;
                pixels[(py * stride) + px] = color;
            }
        }
    }

    /// <summary>
    /// Fills a rectangular region with a color, with bounds checking.
    /// </summary>
    /// <param name="pixels">The pixel buffer span to write to.</param>
    /// <param name="stride">The stride (width) of the pixel buffer.</param>
    /// <param name="bufferHeight">The height of the pixel buffer.</param>
    /// <param name="x">The X coordinate of the top-left corner.</param>
    /// <param name="y">The Y coordinate of the top-left corner.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="color">The BGRA color value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillRectangleSafe(
        Span<uint> pixels,
        int stride,
        int bufferHeight,
        int x,
        int y,
        int width,
        int height,
        uint color)
    {
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                int px = x + col;
                int py = y + row;

                if (px >= 0 && px < stride && py >= 0 && py < bufferHeight)
                {
                    pixels[(py * stride) + px] = color;
                }
            }
        }
    }
}