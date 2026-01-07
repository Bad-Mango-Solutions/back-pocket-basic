// <copyright file="PhysicalMemorySourceProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines a source to load into physical memory at a specific offset.
/// </summary>
/// <remarks>
/// <para>
/// Physical memory sources specify ROM images to be loaded into a physical memory
/// block during initialization. Each source has an offset that determines where
/// the data is placed within the physical memory.
/// </para>
/// <para>
/// Currently only "rom-image" type is supported, which references a ROM image
/// defined in the profile's rom-images array.
/// </para>
/// </remarks>
public sealed class PhysicalMemorySourceProfile
{
    /// <summary>
    /// Gets or sets the source type.
    /// </summary>
    /// <remarks>
    /// Currently only "rom-image" is supported.
    /// </remarks>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets a descriptive name for this source entry.
    /// </summary>
    /// <remarks>
    /// Used for documentation and error messages.
    /// Example: "basic", "monitor".
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the reference to a ROM image defined in rom-images.
    /// </summary>
    /// <remarks>
    /// This must match the "name" property of a ROM image in the profile's
    /// rom-images array. Only used when type is "rom-image".
    /// </remarks>
    [JsonPropertyName("rom-image")]
    public string? RomImage { get; set; }

    /// <summary>
    /// Gets or sets the offset within the physical memory where this source is loaded.
    /// </summary>
    /// <remarks>
    /// The offset as a hex string (e.g., "0x0000", "0x2800").
    /// The source data is written starting at this offset within the physical memory block.
    /// </remarks>
    [JsonPropertyName("offset")]
    public required string Offset { get; set; }
}
