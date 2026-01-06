// <copyright file="RomRegionProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines a named region within a ROM file.
/// </summary>
/// <remarks>
/// <para>
/// ROM regions allow a single ROM file to be logically divided into multiple
/// segments that can be mapped to different addresses. This is useful for
/// ROM files that contain multiple functional areas (e.g., monitor ROM,
/// BASIC ROM, character generator).
/// </para>
/// <para>
/// Each region specifies:
/// <list type="bullet">
/// <item><description>Name: Identifier for referencing this region</description></item>
/// <item><description>Start: Memory address where this region is mapped</description></item>
/// <item><description>SourceOffset: Byte offset within the ROM file</description></item>
/// <item><description>Size: Number of bytes in this region</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class RomRegionProfile
{
    /// <summary>
    /// Gets or sets the name for this region.
    /// </summary>
    /// <remarks>
    /// The name identifies this region within the ROM.
    /// Examples: "monitor", "basic-interpreter", "character-generator".
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the starting address where this region is mapped as a hex string.
    /// </summary>
    /// <remarks>
    /// This is the virtual address in the machine's address space.
    /// Format: "0xC000", "0xD000", etc.
    /// </remarks>
    [JsonPropertyName("start")]
    public required string Start { get; set; }

    /// <summary>
    /// Gets or sets the offset within the ROM file as a hex string.
    /// </summary>
    /// <remarks>
    /// This is the byte position within the ROM file where this region's data begins.
    /// Format: "0x0000", "0x1000", etc.
    /// </remarks>
    [JsonPropertyName("sourceOffset")]
    public required string SourceOffset { get; set; }

    /// <summary>
    /// Gets or sets the size of this region in bytes as a hex string.
    /// </summary>
    /// <remarks>
    /// Format: "0x1000" for 4KB, "0x2000" for 8KB, etc.
    /// </remarks>
    [JsonPropertyName("size")]
    public required string Size { get; set; }
}