// <copyright file="DisplayColors.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

/// <summary>
/// Provides standard display colors in BGRA format for use with pixel buffers.
/// </summary>
/// <remarks>
/// All color values are in Bgra8888 format (0xAARRGGBB when viewed as ARGB, but stored as BGRA).
/// The format is: 0xAABBGGRR where AA=Alpha, BB=Blue, GG=Green, RR=Red.
/// </remarks>
public static class DisplayColors
{
    /// <summary>
    /// Classic green phosphor foreground color (P1 phosphor).
    /// </summary>
    public const uint GreenPhosphor = 0xFF33FF33;

    /// <summary>
    /// Amber phosphor foreground color (P3 phosphor).
    /// </summary>
    public const uint AmberPhosphor = 0xFF00BFFF;

    /// <summary>
    /// White phosphor foreground color (P4 phosphor).
    /// </summary>
    public const uint WhitePhosphor = 0xFFFFFFFF;

    /// <summary>
    /// Standard black background color.
    /// </summary>
    public const uint Black = 0xFF000000;

    /// <summary>
    /// Dark gray background color for UI elements.
    /// </summary>
    public const uint DarkGray = 0xFF1A1A1A;

    /// <summary>
    /// Slightly lighter gray for cell backgrounds.
    /// </summary>
    public const uint CellBackground = 0xFF222222;

    /// <summary>
    /// Lo-Res color 0: Black.
    /// </summary>
    public const uint LoResBlack = 0xFF000000;

    /// <summary>
    /// Lo-Res color 1: Magenta (Deep Red).
    /// </summary>
    public const uint LoResMagenta = 0xFF6A0090;

    /// <summary>
    /// Lo-Res color 2: Dark Blue.
    /// </summary>
    public const uint LoResDarkBlue = 0xFF9D0040;

    /// <summary>
    /// Lo-Res color 3: Purple (Violet).
    /// </summary>
    public const uint LoResPurple = 0xFFFF00D0;

    /// <summary>
    /// Lo-Res color 4: Dark Green.
    /// </summary>
    public const uint LoResDarkGreen = 0xFF006400;

    /// <summary>
    /// Lo-Res color 5: Gray 1 (Dark Gray).
    /// </summary>
    public const uint LoResGray1 = 0xFF808080;

    /// <summary>
    /// Lo-Res color 6: Medium Blue.
    /// </summary>
    public const uint LoResMediumBlue = 0xFFFF2800;

    /// <summary>
    /// Lo-Res color 7: Light Blue.
    /// </summary>
    public const uint LoResLightBlue = 0xFFFFAA80;

    /// <summary>
    /// Lo-Res color 8: Brown.
    /// </summary>
    public const uint LoResBrown = 0xFF005580;

    /// <summary>
    /// Lo-Res color 9: Orange.
    /// </summary>
    public const uint LoResOrange = 0xFF0080FF;

    /// <summary>
    /// Lo-Res color 10: Gray 2 (Light Gray).
    /// </summary>
    public const uint LoResGray2 = 0xFF808080;

    /// <summary>
    /// Lo-Res color 11: Pink.
    /// </summary>
    public const uint LoResPink = 0xFFAAA0FF;

    /// <summary>
    /// Lo-Res color 12: Light Green (Green).
    /// </summary>
    public const uint LoResLightGreen = 0xFF14E000;

    /// <summary>
    /// Lo-Res color 13: Yellow.
    /// </summary>
    public const uint LoResYellow = 0xFF55FFFF;

    /// <summary>
    /// Lo-Res color 14: Aqua (Cyan).
    /// </summary>
    public const uint LoResAqua = 0xFFFFE040;

    /// <summary>
    /// Lo-Res color 15: White.
    /// </summary>
    public const uint LoResWhite = 0xFFFFFFFF;

    /// <summary>
    /// Gets the Lo-Res color for the specified color index.
    /// </summary>
    /// <param name="colorIndex">The color index (0-15).</param>
    /// <returns>The BGRA color value.</returns>
    public static uint GetLoResColor(int colorIndex)
    {
        return (colorIndex & 0x0F) switch
        {
            0 => LoResBlack,
            1 => LoResMagenta,
            2 => LoResDarkBlue,
            3 => LoResPurple,
            4 => LoResDarkGreen,
            5 => LoResGray1,
            6 => LoResMediumBlue,
            7 => LoResLightBlue,
            8 => LoResBrown,
            9 => LoResOrange,
            10 => LoResGray2,
            11 => LoResPink,
            12 => LoResLightGreen,
            13 => LoResYellow,
            14 => LoResAqua,
            15 => LoResWhite,
            _ => LoResBlack,
        };
    }
}