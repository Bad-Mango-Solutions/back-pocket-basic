// <copyright file="PhysicalMemoryProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines a physical memory block that backs virtual memory regions.
/// </summary>
/// <remarks>
/// <para>
/// Physical memory blocks represent the actual memory chips in the emulated system.
/// They can be initialized with a fill pattern and optionally have ROM images
/// loaded into them at specified offsets.
/// </para>
/// <para>
/// Multiple virtual regions can reference the same physical memory block at
/// different offsets, enabling scenarios like banked memory where the same
/// physical RAM can appear at different virtual addresses.
/// </para>
/// </remarks>
public sealed class PhysicalMemoryProfile
{
    /// <summary>
    /// Gets or sets the unique name for this physical memory block.
    /// </summary>
    /// <remarks>
    /// This name is used as a reference from memory region "source" properties.
    /// Examples: "main-ram-48k", "system-rom-12k", "aux-memory-64k".
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the size of this physical memory block as a hex string.
    /// </summary>
    /// <remarks>
    /// The size does not need to be 4KB page-aligned, but should preferably
    /// be at least 256-byte aligned for efficient memory mapping.
    /// Examples: "0xC000" for 48KB, "0x10000" for 64KB, "0x3000" for 12KB.
    /// </remarks>
    [JsonPropertyName("size")]
    public required string Size { get; set; }

    /// <summary>
    /// Gets or sets the fill pattern for initialization.
    /// </summary>
    /// <remarks>
    /// A hex string representing a single byte value to fill the memory with.
    /// Examples: "0x00" for zeros, "0xFF" for all ones, "0xAA" for alternating bits.
    /// If not specified, memory is zero-initialized.
    /// </remarks>
    [JsonPropertyName("fill")]
    public string? Fill { get; set; }

    /// <summary>
    /// Gets or sets the sources to load into this physical memory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sources define ROM images to load at specific offsets within this physical memory.
    /// Multiple sources can be loaded into the same physical memory block at different offsets.
    /// </para>
    /// <para>
    /// Sources are processed in order, so later sources will overwrite earlier ones
    /// if their ranges overlap.
    /// </para>
    /// </remarks>
    [JsonPropertyName("sources")]
    public List<PhysicalMemorySourceProfile>? Sources { get; set; }
}