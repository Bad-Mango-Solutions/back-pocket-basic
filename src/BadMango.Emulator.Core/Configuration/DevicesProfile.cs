// <copyright file="DevicesProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Defines device configuration section within a machine profile.
/// </summary>
/// <remarks>
/// <para>
/// The devices section configures built-in motherboard devices such as
/// keyboard, speaker, video, and game I/O. Each device can have a preset
/// and individual configuration overrides.
/// </para>
/// <para>
/// Device configuration supports a preset system where named presets provide
/// default values that can be overridden by individual properties.
/// </para>
/// </remarks>
public sealed class DevicesProfile
{
    /// <summary>
    /// Gets or sets the keyboard device configuration.
    /// </summary>
    [JsonPropertyName("keyboard")]
    public KeyboardDeviceProfile? Keyboard { get; set; }

    /// <summary>
    /// Gets or sets the speaker device configuration.
    /// </summary>
    [JsonPropertyName("speaker")]
    public SpeakerDeviceProfile? Speaker { get; set; }

    /// <summary>
    /// Gets or sets the video device configuration.
    /// </summary>
    [JsonPropertyName("video")]
    public VideoDeviceProfile? Video { get; set; }

    /// <summary>
    /// Gets or sets the game I/O (joystick/paddle) configuration.
    /// </summary>
    [JsonPropertyName("gameIO")]
    public GameIODeviceProfile? GameIO { get; set; }

    /// <summary>
    /// Gets or sets additional device-specific configurations.
    /// </summary>
    /// <remarks>
    /// This allows extending device configuration without modifying the schema.
    /// Keys are device type names, values are device-specific configuration objects.
    /// </remarks>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalDevices { get; set; }
}