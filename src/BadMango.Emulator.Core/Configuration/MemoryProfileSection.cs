// <copyright file="MemoryProfileSection.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Memory configuration section of a machine profile.
/// </summary>
/// <remarks>
/// <para>
/// Memory configuration can be specified using either the new bus-oriented regions format
/// or the legacy size/type format. The new format uses the <see cref="Regions"/> property
/// to define individual memory regions with precise control over location, permissions,
/// and initialization.
/// </para>
/// <para>
/// When <see cref="Regions"/> is specified, it takes precedence over legacy properties.
/// </para>
/// </remarks>
public sealed class MemoryProfileSection
{
    /// <summary>
    /// Gets or sets the memory regions for bus-oriented configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each region defines a segment of the address space with its type (RAM, ROM, I/O),
    /// location, size, permissions, and optional initialization data.
    /// </para>
    /// <para>
    /// When this property is set, the legacy <see cref="Size"/> and <see cref="Type"/>
    /// properties are ignored.
    /// </para>
    /// </remarks>
    [JsonPropertyName("regions")]
    public List<MemoryRegionProfile>? Regions { get; set; }

    /// <summary>
    /// Gets a value indicating whether this configuration uses the bus-oriented regions format.
    /// </summary>
    [JsonIgnore]
    public bool UsesRegions => Regions is { Count: > 0 };
}