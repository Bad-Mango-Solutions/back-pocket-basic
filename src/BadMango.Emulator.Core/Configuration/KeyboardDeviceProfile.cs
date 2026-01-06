// <copyright file="KeyboardDeviceProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Configuration for the keyboard device.
/// </summary>
public sealed class KeyboardDeviceProfile
{
    /// <summary>
    /// Gets or sets the keyboard preset name.
    /// </summary>
    /// <remarks>
    /// Available presets: "standard" (Apple II/II+), "enhanced" (Apple IIe enhanced keyboard).
    /// </remarks>
    [JsonPropertyName("preset")]
    public string? Preset { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether key auto-repeat is enabled.
    /// </summary>
    [JsonPropertyName("autoRepeat")]
    public bool? AutoRepeat { get; set; }
}