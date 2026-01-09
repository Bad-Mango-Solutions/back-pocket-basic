// <copyright file="DevicesProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines the devices configuration section within a machine profile.
/// </summary>
/// <remarks>
/// <para>
/// The devices section contains all hardware devices for the machine:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Motherboard"/> - Built-in devices like speaker, keyboard, video</description></item>
/// <item><description><see cref="Slots"/> - Expansion slot system and installed cards</description></item>
/// </list>
/// <para>
/// This structure allows for flexible device configuration without hard-coding
/// specific device types in the schema. New device types can be added by
/// registering device factories with the machine builder.
/// </para>
/// </remarks>
public sealed class DevicesProfile
{
    /// <summary>
    /// Gets or sets the motherboard devices configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Motherboard devices are built-in devices that are not slot-based:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Speaker ($C030)</description></item>
    /// <item><description>Keyboard ($C000-$C010)</description></item>
    /// <item><description>Video controller ($C050-$C057)</description></item>
    /// <item><description>Game I/O ($C060-$C070)</description></item>
    /// </list>
    /// <para>
    /// Each entry specifies a device type and optional configuration.
    /// </para>
    /// </remarks>
    [JsonPropertyName("motherboard")]
    public List<MotherboardDeviceEntry>? Motherboard { get; set; }

    /// <summary>
    /// Gets or sets the expansion slot system configuration.
    /// </summary>
    /// <remarks>
    /// Defines slot cards for slots 1-7 and slot ROM behavior options.
    /// If <see langword="null"/>, the slot system is disabled.
    /// </remarks>
    [JsonPropertyName("slots")]
    public SlotSystemProfile? Slots { get; set; }
}