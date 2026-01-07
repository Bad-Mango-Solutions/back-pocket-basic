// <copyright file="MemoryRegionProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines a single memory region within a machine profile.
/// </summary>
/// <remarks>
/// <para>
/// Memory regions describe how segments of the CPU's virtual address space map to
/// physical memory blocks. Each region specifies its type, location, size, and
/// which physical memory block backs it.
/// </para>
/// <para>
/// Regions must be 4KB page-aligned (start, size, and source-offset must all be
/// multiples of 0x1000) for compatibility with the bus architecture.
/// </para>
/// <para>
/// RAM and ROM regions must specify a "source" that references a physical memory
/// block by name. Composite regions use a "handler" instead.
/// </para>
/// </remarks>
public sealed class MemoryRegionProfile
{
    /// <summary>
    /// Gets or sets the unique name of this region.
    /// </summary>
    /// <remarks>
    /// The name is used to identify the region in logs, debugging output,
    /// and when referencing the region from other parts of the system.
    /// Examples: "main-ram", "system-rom", "io-page".
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of this memory region.
    /// </summary>
    /// <remarks>
    /// Valid values are:
    /// <list type="bullet">
    /// <item><description>"ram" - Read/write memory (requires source)</description></item>
    /// <item><description>"rom" - Read-only memory (requires source)</description></item>
    /// <item><description>"composite" - Composite region with custom handler</description></item>
    /// </list>
    /// The "io" type is reserved for future use.
    /// </remarks>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the starting address of this region as a hex string.
    /// </summary>
    /// <remarks>
    /// The address must be 4KB page-aligned (multiple of 0x1000).
    /// Format: "0x0000" or "0xC000" (with or without leading zeros).
    /// </remarks>
    [JsonPropertyName("start")]
    public required string Start { get; set; }

    /// <summary>
    /// Gets or sets the size of this region in bytes as a hex string.
    /// </summary>
    /// <remarks>
    /// The size must be 4KB page-aligned (multiple of 0x1000).
    /// Format: "0x10000" for 64KB, "0x4000" for 16KB, etc.
    /// </remarks>
    [JsonPropertyName("size")]
    public required string Size { get; set; }

    /// <summary>
    /// Gets or sets the access permissions for this region.
    /// </summary>
    /// <remarks>
    /// A string containing permission characters:
    /// <list type="bullet">
    /// <item><description>'r' - Read permission</description></item>
    /// <item><description>'w' - Write permission</description></item>
    /// <item><description>'x' - Execute permission</description></item>
    /// <item><description>'-' - No permission (explicit placeholder)</description></item>
    /// </list>
    /// Examples: "rwx" (full access), "rx" (ROM), "rw" (no execute).
    /// Defaults to "rwx" if not specified.
    /// </remarks>
    [JsonPropertyName("permissions")]
    public string Permissions { get; set; } = "rwx";

    /// <summary>
    /// Gets or sets the reference to a physical memory block.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For "ram" and "rom" type regions, this references the name of a physical
    /// memory block defined in the profile's "physical" array.
    /// </para>
    /// <para>
    /// Not used for "composite" regions which use the Handler property instead.
    /// </para>
    /// </remarks>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the offset within the physical memory backing store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The offset as a hex string (e.g., "0x0000", "0x4000").
    /// Must be 4KB page-aligned (multiple of 0x1000).
    /// Defaults to "0x0000" if not specified.
    /// </para>
    /// <para>
    /// This allows multiple regions to share the same physical memory block
    /// at different offsets.
    /// </para>
    /// </remarks>
    [JsonPropertyName("source-offset")]
    public string? SourceOffset { get; set; }

    /// <summary>
    /// Gets or sets the handler identifier for composite regions.
    /// </summary>
    /// <remarks>
    /// For "composite" type regions, this specifies the handler that manages
    /// the region. The builder looks up a handler factory by this name.
    /// Examples: "pocket2e-io", "slot-rom-handler".
    /// </remarks>
    [JsonPropertyName("handler")]
    public string? Handler { get; set; }
}