// <copyright file="MotherboardDeviceEntry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Defines a motherboard device entry in the profile.
/// </summary>
/// <remarks>
/// <para>
/// Motherboard devices are built-in devices that are not slot-based, such as
/// the speaker, keyboard, video controller, and game I/O. Each device has a
/// type identifier and optional configuration.
/// </para>
/// <para>
/// The device type is matched against registered device factories when the
/// profile is loaded. If no factory is registered for a device type, the
/// device is silently skipped (allowing profiles to declare devices that
/// may not be supported by all hosts).
/// </para>
/// </remarks>
public sealed class MotherboardDeviceEntry
{
    /// <summary>
    /// Gets or sets the device type identifier.
    /// </summary>
    /// <remarks>
    /// Examples: "speaker", "keyboard", "video", "gameio".
    /// This is matched against registered device factories.
    /// </remarks>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the optional device name for identification.
    /// </summary>
    /// <remarks>
    /// If not specified, the device type is used as the name.
    /// </remarks>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the device is enabled.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true"/>. Set to <see langword="false"/> to
    /// disable a device without removing it from the profile.
    /// </remarks>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the device-specific configuration.
    /// </summary>
    /// <remarks>
    /// The structure of this object depends on the device type. For example,
    /// a speaker device might have volume settings, while a keyboard device
    /// might have layout configuration.
    /// </remarks>
    [JsonPropertyName("config")]
    public JsonElement? Config { get; set; }
}