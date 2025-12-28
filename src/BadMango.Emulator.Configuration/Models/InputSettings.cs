// <copyright file="InputSettings.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.Models;

/// <summary>
/// Input and control settings for keyboard, mouse, and joystick.
/// </summary>
public record InputSettings
{
    /// <summary>
    /// Gets the keyboard mapping mode (Standard, Positional, Custom).
    /// </summary>
    public string KeyboardMapping { get; init; } = "Standard";

    /// <summary>
    /// Gets the path to a custom key mapping file, if any.
    /// </summary>
    public string? CustomKeyMapFile { get; init; }

    /// <summary>
    /// Gets a value indicating whether to auto-capture the mouse in the display.
    /// </summary>
    public bool MouseCapture { get; init; }

    /// <summary>
    /// Gets a value indicating whether joystick/gamepad input is enabled.
    /// </summary>
    public bool JoystickEnabled { get; init; }

    /// <summary>
    /// Gets the selected joystick device name ("Auto" for automatic selection).
    /// </summary>
    public string JoystickDevice { get; init; } = "Auto";

    /// <summary>
    /// Gets the analog paddle sensitivity (1-100).
    /// </summary>
    public int PaddleSensitivity { get; init; } = 50;
}