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
/// Memory configuration defines the address space layout using a three-tier model:
/// </para>
/// <list type="number">
/// <item><description>
/// <b>ROM Images</b>: External binary files containing firmware, BASIC, monitor, etc.
/// These are loaded from disk and written into physical memory at specified offsets.
/// </description></item>
/// <item><description>
/// <b>Physical Memory</b>: Backing stores that represent actual memory chips.
/// Physical memory blocks can be RAM (read-write) or ROM (read-only after initialization).
/// ROM images are loaded into physical memory during initialization.
/// </description></item>
/// <item><description>
/// <b>Regions</b>: Virtual address mappings that map ranges of the CPU's address space
/// to physical memory blocks at specified offsets. Multiple regions can reference
/// the same physical memory, enabling bank-switching scenarios.
/// </description></item>
/// </list>
/// <para>
/// For complex machines, swap groups and controllers handle bank-switched memory.
/// </para>
/// </remarks>
public sealed class MemoryProfileSection
{
    /// <summary>
    /// Gets or sets the ROM image definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ROM images define external binary files that can be loaded into physical memory.
    /// Each ROM image has a name, source path, and expected size.
    /// </para>
    /// <para>
    /// ROM images are referenced by name from physical memory source entries.
    /// </para>
    /// </remarks>
    [JsonPropertyName("rom-images")]
    public List<RomImageProfile>? RomImages { get; set; }

    /// <summary>
    /// Gets or sets the physical memory definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Physical memory blocks represent the actual memory chips in the emulated system.
    /// Each block has a name, size, optional fill pattern, and optional sources that
    /// load ROM images at specified offsets.
    /// </para>
    /// <para>
    /// Physical memory blocks are referenced by name from region "source" properties.
    /// </para>
    /// </remarks>
    [JsonPropertyName("physical")]
    public List<PhysicalMemoryProfile>? Physical { get; set; }

    /// <summary>
    /// Gets or sets the memory regions for bus-oriented configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each region defines a segment of the CPU's virtual address space and how it
    /// maps to a physical memory block. Regions must specify a source (physical memory
    /// name) and optionally a source-offset.
    /// </para>
    /// <para>
    /// Supported region types:
    /// <list type="bullet">
    /// <item><description>"ram" - Read/write memory (requires source)</description></item>
    /// <item><description>"rom" - Read-only memory (requires source)</description></item>
    /// <item><description>"composite" - Composite region with handler (e.g., I/O page)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// All region start addresses, sizes, and source-offsets must be 4KB page-aligned.
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

    /// <summary>
    /// Gets a value indicating whether this configuration uses the physical memory format.
    /// </summary>
    [JsonIgnore]
    public bool UsesPhysicalMemory => Physical is { Count: > 0 };
}