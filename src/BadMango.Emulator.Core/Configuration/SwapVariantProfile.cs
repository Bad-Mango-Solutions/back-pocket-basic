// <copyright file="SwapVariantProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines a variant within a swap group.
/// </summary>
/// <remarks>
/// <para>
/// Each variant represents a different memory region that can be swapped into
/// the virtual address space defined by the parent <see cref="SwapGroupProfile"/>.
/// Variants can be ROM (read-only) or RAM (read-write).
/// </para>
/// <para>
/// Variants reference physical memory blocks by name and optionally specify
/// an offset within that physical memory.
/// </para>
/// </remarks>
public sealed class SwapVariantProfile
{
    /// <summary>
    /// Gets or sets the unique name for this variant within the swap group.
    /// </summary>
    /// <remarks>
    /// The name is used to select this variant. Examples: "rom", "bank1", "bank2", "ram".
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the memory type for this variant.
    /// </summary>
    /// <remarks>
    /// Valid values are:
    /// <list type="bullet">
    /// <item><description>"ram" - Read/write random access memory</description></item>
    /// <item><description>"rom" - Read-only memory</description></item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the size override for this variant as a hex string.
    /// </summary>
    /// <remarks>
    /// If not specified, the variant uses the size from the parent swap group.
    /// Format: "0x1000" for 4KB, "0x2000" for 8KB, etc.
    /// </remarks>
    [JsonPropertyName("size")]
    public string? Size { get; set; }

    /// <summary>
    /// Gets or sets the reference to a physical memory block.
    /// </summary>
    /// <remarks>
    /// This references the name of a physical memory block defined in the
    /// profile's "physical" array.
    /// </remarks>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the offset within the physical memory block as a hex string.
    /// </summary>
    /// <remarks>
    /// Specifies the byte offset within the referenced physical memory where this
    /// variant's data begins. Format: "0x1000", "0x2000", etc.
    /// Must be 4KB page-aligned.
    /// </remarks>
    [JsonPropertyName("source-offset")]
    public string? SourceOffset { get; set; }

    /// <summary>
    /// Gets or sets the access permissions for this variant.
    /// </summary>
    /// <remarks>
    /// A string containing permission characters:
    /// <list type="bullet">
    /// <item><description>'r' - Read permission</description></item>
    /// <item><description>'w' - Write permission</description></item>
    /// <item><description>'x' - Execute permission</description></item>
    /// </list>
    /// Examples: "rwx" (full access), "rx" (ROM-like), "rw" (no execute).
    /// Defaults to "rwx" if not specified.
    /// </remarks>
    [JsonPropertyName("permissions")]
    public string Permissions { get; set; } = "rwx";
}