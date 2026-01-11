// <copyright file="Pocket2VideoRenderer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Devices;

/// <summary>
/// Renders Pocket2e video modes to a pixel buffer.
/// </summary>
/// <remarks>
/// <para>
/// This renderer supports the following Pocket2e video modes:
/// </para>
/// <list type="bullet">
/// <item><description>40-column text mode (Text40)</description></item>
/// <item><description>Lo-res graphics (LoRes, 40×48)</description></item>
/// <item><description>Hi-res graphics (HiRes, 280×192)</description></item>
/// <item><description>Mixed modes with 4-line text window</description></item>
/// </list>
/// <para>
/// All modes are rendered to a canonical 560×384 pixel buffer using integer scaling.
/// This allows consistent window sizing regardless of mode switching.
/// </para>
/// </remarks>
public sealed class Pocket2VideoRenderer : IVideoRenderer
{
    /// <summary>
    /// Text page 1 base address.
    /// </summary>
    private const ushort TextPage1Base = 0x0400;

    /// <summary>
    /// Text page 2 base address.
    /// </summary>
    private const ushort TextPage2Base = 0x0800;

    /// <summary>
    /// Hi-res page 1 base address.
    /// </summary>
    private const ushort HiResPage1Base = 0x2000;

    /// <summary>
    /// Hi-res page 2 base address.
    /// </summary>
    private const ushort HiResPage2Base = 0x4000;

    /// <summary>
    /// Number of text rows.
    /// </summary>
    private const int TextRows = 24;

    /// <summary>
    /// Number of text columns in 40-column mode.
    /// </summary>
    private const int Text40Columns = 40;

    /// <summary>
    /// Character width in pixels.
    /// </summary>
    private const int CharWidth = 7;

    /// <summary>
    /// Character height in scanlines.
    /// </summary>
    private const int CharHeight = 8;

    /// <summary>
    /// Number of hi-res scanlines.
    /// </summary>
    private const int HiResScanlines = 192;

    /// <summary>
    /// Number of bytes per hi-res scanline.
    /// </summary>
    private const int HiResBytesPerLine = 40;

    /// <summary>
    /// Number of lo-res rows (48 rows, 2 per text byte).
    /// </summary>
    private const int LoResRows = 48;

    // Pre-computed text row base addresses for both pages
    private static readonly ushort[] TextRowAddresses = ComputeTextRowAddresses();

    // Pre-computed hi-res row base addresses
    private static readonly ushort[] HiResRowAddresses = ComputeHiResRowAddresses();

    /// <inheritdoc />
    public int CanonicalWidth => 560;

    /// <inheritdoc />
    public int CanonicalHeight => 384;

    /// <inheritdoc />
    public void RenderFrame(
        Span<uint> pixels,
        VideoMode mode,
        Func<ushort, byte> readMemory,
        ReadOnlySpan<byte> characterRomData,
        bool useAltCharSet,
        bool isPage2,
        bool flashState)
    {
        ArgumentNullException.ThrowIfNull(readMemory);

        switch (mode)
        {
            case VideoMode.Text40:
                RenderText40(pixels, readMemory, characterRomData, useAltCharSet, isPage2, flashState);
                break;

            case VideoMode.LoRes:
                RenderLoRes(pixels, readMemory, isPage2, mixedMode: false);
                break;

            case VideoMode.LoResMixed:
                RenderLoRes(pixels, readMemory, isPage2, mixedMode: true);
                RenderTextWindow(pixels, readMemory, characterRomData, useAltCharSet, isPage2, flashState);
                break;

            case VideoMode.HiRes:
                RenderHiRes(pixels, readMemory, isPage2, mixedMode: false);
                break;

            case VideoMode.HiResMixed:
                RenderHiRes(pixels, readMemory, isPage2, mixedMode: true);
                RenderTextWindow(pixels, readMemory, characterRomData, useAltCharSet, isPage2, flashState);
                break;

            default:
                // For unsupported modes, render as text40
                RenderText40(pixels, readMemory, characterRomData, useAltCharSet, isPage2, flashState);
                break;
        }
    }

    /// <inheritdoc />
    public void Clear(Span<uint> pixels)
    {
        pixels.Fill(DisplayColors.Black);
    }

    /// <summary>
    /// Computes the text row base addresses for page 1.
    /// </summary>
    private static ushort[] ComputeTextRowAddresses()
    {
        var addresses = new ushort[TextRows];

        for (int row = 0; row < TextRows; row++)
        {
            int group = row / 8;       // 0, 1, or 2
            int offset = row % 8;      // 0-7
            addresses[row] = (ushort)(TextPage1Base + (offset * 128) + (group * 40));
        }

        return addresses;
    }

    /// <summary>
    /// Computes the hi-res row base addresses for page 1.
    /// </summary>
    private static ushort[] ComputeHiResRowAddresses()
    {
        var addresses = new ushort[HiResScanlines];

        for (int row = 0; row < HiResScanlines; row++)
        {
            int group = row / 64;          // 0, 1, or 2
            int subRow = (row % 64) / 8;   // 0-7
            int scanLine = row % 8;        // 0-7
            addresses[row] = (ushort)(HiResPage1Base + (scanLine * 1024) + (subRow * 128) + (group * 40));
        }

        return addresses;
    }

    /// <summary>
    /// Renders 40-column text mode.
    /// </summary>
    private void RenderText40(
        Span<uint> pixels,
        Func<ushort, byte> readMemory,
        ReadOnlySpan<byte> characterRomData,
        bool useAltCharSet,
        bool isPage2,
        bool flashState)
    {
        ushort pageBase = isPage2 ? TextPage2Base : TextPage1Base;

        for (int row = 0; row < TextRows; row++)
        {
            ushort rowAddr = (ushort)(TextRowAddresses[row] + (isPage2 ? 0x0400 : 0));

            for (int col = 0; col < Text40Columns; col++)
            {
                byte charCode = readMemory((ushort)(rowAddr + col));
                RenderCharacter40(pixels, characterRomData, charCode, row, col, useAltCharSet, flashState);
            }
        }
    }

    /// <summary>
    /// Renders a single character in 40-column mode with 2× scaling.
    /// </summary>
    private void RenderCharacter40(
        Span<uint> pixels,
        ReadOnlySpan<byte> characterRomData,
        byte charCode,
        int row,
        int col,
        bool useAltCharSet,
        bool flashState)
    {
        // Determine display style based on character code
        bool isInverse = charCode < 0x40;
        bool isFlashing = charCode >= 0x40 && charCode < 0x80;

        // Map character code to ROM offset
        int romCharCode = charCode & 0x3F;
        if (charCode >= 0x80)
        {
            romCharCode = charCode & 0x7F;
        }

        // Choose character set
        int romOffset = 0;
        if (useAltCharSet && charCode >= 0x40 && charCode < 0x80)
        {
            romOffset = 2048; // Alternate character set offset
        }

        // Calculate pixel position (2× scaling: 14×16 pixels per character)
        int pixelX = col * CharWidth * 2;
        int pixelY = row * CharHeight * 2;

        // Determine if character should be inverted
        bool shouldInvert = isInverse || (isFlashing && flashState);

        // Render each scanline
        for (int scanline = 0; scanline < CharHeight; scanline++)
        {
            byte scanlineData = 0;
            if (!characterRomData.IsEmpty && romOffset + (romCharCode * 8) + scanline < characterRomData.Length)
            {
                scanlineData = characterRomData[romOffset + (romCharCode * 8) + scanline];
            }

            if (shouldInvert)
            {
                scanlineData = (byte)~scanlineData;
            }

            // Render 7 pixels per scanline with 2× horizontal scaling
            for (int pixel = 0; pixel < CharWidth; pixel++)
            {
                bool isSet = (scanlineData & (1 << (6 - pixel))) != 0;
                uint color = isSet ? DisplayColors.GreenPhosphor : DisplayColors.Black;

                // Write 2×2 block for each source pixel
                int x = pixelX + (pixel * 2);
                int y = pixelY + (scanline * 2);

                SetPixel2x2(pixels, x, y, color);
            }
        }
    }

    /// <summary>
    /// Renders lo-res graphics mode.
    /// </summary>
    private void RenderLoRes(
        Span<uint> pixels,
        Func<ushort, byte> readMemory,
        bool isPage2,
        bool mixedMode)
    {
        int maxTextRows = mixedMode ? 20 : TextRows;
        int maxLoResRows = maxTextRows * 2; // 2 lo-res rows per text row

        for (int textRow = 0; textRow < maxTextRows; textRow++)
        {
            ushort rowAddr = (ushort)(TextRowAddresses[textRow] + (isPage2 ? 0x0400 : 0));

            for (int col = 0; col < Text40Columns; col++)
            {
                byte data = readMemory((ushort)(rowAddr + col));

                // Each byte contains two vertically stacked color blocks
                int topColor = data & 0x0F;
                int bottomColor = (data >> 4) & 0x0F;

                // Lo-res blocks are 14×8 pixels in canonical framebuffer (560/40 × 384/48)
                int blockWidth = 14;
                int blockHeight = 8;

                int pixelX = col * blockWidth;
                int topY = (textRow * 2) * blockHeight;
                int bottomY = ((textRow * 2) + 1) * blockHeight;

                // Render top block
                FillBlock(pixels, pixelX, topY, blockWidth, blockHeight, DisplayColors.GetLoResColor(topColor));

                // Render bottom block (if not cut off by mixed mode)
                if ((textRow * 2) + 1 < maxLoResRows)
                {
                    FillBlock(pixels, pixelX, bottomY, blockWidth, blockHeight, DisplayColors.GetLoResColor(bottomColor));
                }
            }
        }
    }

    /// <summary>
    /// Renders hi-res graphics mode.
    /// </summary>
    private void RenderHiRes(
        Span<uint> pixels,
        Func<ushort, byte> readMemory,
        bool isPage2,
        bool mixedMode)
    {
        int maxScanlines = mixedMode ? 160 : HiResScanlines;
        ushort pageBase = isPage2 ? HiResPage2Base : HiResPage1Base;

        for (int scanline = 0; scanline < maxScanlines; scanline++)
        {
            ushort lineAddr = (ushort)(HiResRowAddresses[scanline] + (isPage2 ? 0x2000 : 0));
            int pixelY = scanline * 2; // 2× vertical scaling

            for (int byteCol = 0; byteCol < HiResBytesPerLine; byteCol++)
            {
                byte data = readMemory((ushort)(lineAddr + byteCol));
                int pixelX = byteCol * CharWidth * 2; // 7 pixels per byte, 2× scaling

                // Render 7 pixels from this byte
                for (int bit = 0; bit < 7; bit++)
                {
                    bool isSet = (data & (1 << bit)) != 0;
                    uint color = isSet ? DisplayColors.GreenPhosphor : DisplayColors.Black;

                    // Write 2×2 block for each source pixel
                    int x = pixelX + (bit * 2);
                    SetPixel2x2(pixels, x, pixelY, color);
                }
            }
        }
    }

    /// <summary>
    /// Renders the 4-line text window at the bottom of the screen for mixed modes.
    /// </summary>
    private void RenderTextWindow(
        Span<uint> pixels,
        Func<ushort, byte> readMemory,
        ReadOnlySpan<byte> characterRomData,
        bool useAltCharSet,
        bool isPage2,
        bool flashState)
    {
        // Mixed mode: text rows 20-23 (bottom 4 lines)
        for (int row = 20; row < TextRows; row++)
        {
            ushort rowAddr = (ushort)(TextRowAddresses[row] + (isPage2 ? 0x0400 : 0));

            for (int col = 0; col < Text40Columns; col++)
            {
                byte charCode = readMemory((ushort)(rowAddr + col));
                RenderCharacter40(pixels, characterRomData, charCode, row, col, useAltCharSet, flashState);
            }
        }
    }

    /// <summary>
    /// Sets a 2×2 pixel block at the specified position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetPixel2x2(Span<uint> pixels, int x, int y, uint color)
    {
        int stride = CanonicalWidth;

        if (x >= 0 && x + 1 < stride && y >= 0 && y + 1 < CanonicalHeight)
        {
            int offset = (y * stride) + x;
            pixels[offset] = color;
            pixels[offset + 1] = color;
            pixels[offset + stride] = color;
            pixels[offset + stride + 1] = color;
        }
    }

    /// <summary>
    /// Fills a rectangular block with the specified color.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FillBlock(Span<uint> pixels, int x, int y, int width, int height, uint color)
    {
        int stride = CanonicalWidth;

        for (int dy = 0; dy < height && y + dy < CanonicalHeight; dy++)
        {
            int rowOffset = ((y + dy) * stride) + x;
            for (int dx = 0; dx < width && x + dx < stride; dx++)
            {
                pixels[rowOffset + dx] = color;
            }
        }
    }
}