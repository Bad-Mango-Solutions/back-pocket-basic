// <copyright file="GameIODeviceProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Configuration for the game I/O device (joystick/paddles).
/// </summary>
public sealed class GameIODeviceProfile
{
    /// <summary>
    /// Gets or sets a value indicating whether game I/O is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the joystick deadzone.
    /// </summary>
    /// <remarks>
    /// A value between 0.0 and 1.0 representing the percentage of stick movement
    /// that is ignored. Helps prevent drift from centered position.
    /// </remarks>
    [JsonPropertyName("joystickDeadzone")]
    public double JoystickDeadzone { get; set; } = 0.1;
}