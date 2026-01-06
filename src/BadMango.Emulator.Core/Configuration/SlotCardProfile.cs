// <copyright file="SlotCardProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Defines a card installed in an expansion slot within a machine profile.
/// </summary>
/// <remarks>
/// <para>
/// Slot cards provide peripheral functionality through I/O handlers and optional ROM.
/// Each card type knows its own ROM requirements and creates appropriate mappings
/// when installed.
/// </para>
/// <para>
/// Supported card types include:
/// <list type="bullet">
/// <item><description>"pocketwatch" - Real-time clock (Thunderclock-compatible)</description></item>
/// <item><description>"disk-ii-compatible" - Disk drive controller</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class SlotCardProfile
{
    /// <summary>
    /// Gets or sets the slot number (1-7).
    /// </summary>
    /// <remarks>
    /// Slot 0 is reserved for the Language Card and cannot be used.
    /// Slots 1-7 are available for expansion cards.
    /// </remarks>
    [JsonPropertyName("slot")]
    public required int Slot { get; set; }

    /// <summary>
    /// Gets or sets the card type identifier.
    /// </summary>
    /// <remarks>
    /// The type determines which card implementation to instantiate.
    /// Examples: "pocketwatch", "disk-ii-compatible", "serial".
    /// </remarks>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the preset configuration name.
    /// </summary>
    /// <remarks>
    /// Presets provide default configurations for common scenarios.
    /// The card type defines available presets.
    /// </remarks>
    [JsonPropertyName("preset")]
    public string? Preset { get; set; }

    /// <summary>
    /// Gets or sets card-specific configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a loosely-typed configuration object that allows card-specific
    /// options. The configuration is merged with preset defaults if both are specified.
    /// </para>
    /// <para>
    /// Example configurations:
    /// <list type="bullet">
    /// <item><description>PocketWatch: timeSource, timezoneOffset, frozenTime, ntpServer</description></item>
    /// <item><description>Disk II: drive1, drive2 (disk image paths)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [JsonPropertyName("config")]
    public JsonElement? Config { get; set; }
}