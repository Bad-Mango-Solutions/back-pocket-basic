// <copyright file="DisplaySettings.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.Models;

/// <summary>
/// Display and video output settings.
/// </summary>
public record DisplaySettings
{
    /// <summary>
    /// Gets the scaling mode (Integer, AspectCorrect, Fill, Custom).
    /// </summary>
    public string ScalingMode { get; init; } = "Integer";

    /// <summary>
    /// Gets the display scale factor (1-8).
    /// </summary>
    public int ScaleFactor { get; init; } = 2;

    /// <summary>
    /// Gets a value indicating whether to simulate CRT scanlines.
    /// </summary>
    public bool ScanlineEffect { get; init; }

    /// <summary>
    /// Gets the color palette (NTSC, RGB, Monochrome, Amber, Custom).
    /// </summary>
    public string ColorPalette { get; init; } = "NTSC";

    /// <summary>
    /// Gets the maximum frame rate.
    /// </summary>
    public int FrameRateCap { get; init; } = 60;

    /// <summary>
    /// Gets a value indicating whether vertical sync is enabled.
    /// </summary>
    public bool VSync { get; init; } = true;

    /// <summary>
    /// Gets the monitor index for full screen mode.
    /// </summary>
    public int FullScreenMonitor { get; init; }
}