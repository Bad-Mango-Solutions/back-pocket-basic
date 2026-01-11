// <copyright file="VideoWindowContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Rendering;

/// <summary>
/// Context data for video window operations.
/// </summary>
public sealed class VideoWindowContext
{
    /// <summary>
    /// Gets or sets the display scale factor (1-4).
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to toggle FPS display.
    /// </summary>
    public bool ToggleFps { get; set; }

    /// <summary>
    /// Gets or sets the explicit FPS display state.
    /// </summary>
    public bool? ShowFps { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to force a display refresh.
    /// </summary>
    public bool ForceRefresh { get; set; }

    /// <summary>
    /// Gets or sets the display color mode.
    /// </summary>
    public DisplayColorMode? ColorMode { get; set; }
}