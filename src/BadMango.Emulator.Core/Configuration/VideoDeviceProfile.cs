// <copyright file="VideoDeviceProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Configuration for the video device.
/// </summary>
public sealed class VideoDeviceProfile
{
    /// <summary>
    /// Gets or sets the video preset name.
    /// </summary>
    /// <remarks>
    /// Available presets: "standard" (Apple II/II+), "enhanced" (Apple IIe with 80-column).
    /// </remarks>
    [JsonPropertyName("preset")]
    public string? Preset { get; set; }

    /// <summary>
    /// Gets or sets the color mode.
    /// </summary>
    /// <remarks>
    /// Available modes: "mono" (monochrome), "ntsc" (NTSC artifact color), "rgb" (RGB).
    /// </remarks>
    [JsonPropertyName("colorMode")]
    public string? ColorMode { get; set; }
}