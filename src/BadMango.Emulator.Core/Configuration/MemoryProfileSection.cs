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
/// Memory configuration defines the address space layout including RAM regions,
/// ROM mappings, I/O areas, swap groups for bank switching, and memory controllers
/// that manage dynamic regions at runtime.
/// </para>
/// <para>
/// The configuration uses the bus-oriented regions format where each region
/// specifies its type, location, size, and permissions. For complex machines,
/// swap groups and controllers handle bank-switched memory.
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
    /// Supported region types:
    /// <list type="bullet">
    /// <item><description>"ram" - Read/write random access memory</description></item>
    /// <item><description>"rom" - Read-only memory</description></item>
    /// <item><description>"io" - I/O space</description></item>
    /// <item><description>"composite" - Composite region with handler</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [JsonPropertyName("regions")]
    public List<MemoryRegionProfile>? Regions { get; set; }

    /// <summary>
    /// Gets or sets the swap groups for bank-switched memory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Swap groups enable bank switching where multiple memory regions can occupy
    /// the same virtual address range, with only one active at a time. This is used
    /// for features like the Language Card.
    /// </para>
    /// <para>
    /// Bank switching in 6502/65C02 systems is achieved through soft switches that
    /// remap regions within the 16-bit address space—NOT through extended addressing.
    /// </para>
    /// </remarks>
    [JsonPropertyName("swapGroups")]
    public List<SwapGroupProfile>? SwapGroups { get; set; }

    /// <summary>
    /// Gets or sets the memory controllers that manage dynamic regions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Memory controllers spawn and manage memory regions at runtime. They handle
    /// complex configurations like auxiliary memory which can be bank-switched into
    /// the main address space through soft switches.
    /// </para>
    /// </remarks>
    [JsonPropertyName("controllers")]
    public List<MemoryControllerProfile>? Controllers { get; set; }

    /// <summary>
    /// Gets a value indicating whether this configuration uses the bus-oriented regions format.
    /// </summary>
    [JsonIgnore]
    public bool UsesRegions => Regions is { Count: > 0 };
}