// <copyright file="SwapGroupProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines a swap group for bank-switched memory regions within a machine profile.
/// </summary>
/// <remarks>
/// <para>
/// Swap groups enable bank switching where multiple memory regions can occupy
/// the same virtual address range, with only one active at a time. This is used
/// for features like the Language Card which can swap between ROM, Bank1 RAM,
/// and Bank2 RAM at $D000-$DFFF.
/// </para>
/// <para>
/// Bank switching in 6502/65C02 systems is achieved through soft switches that
/// remap regions within the 16-bit address space—NOT through extended addressing.
/// </para>
/// </remarks>
public sealed class SwapGroupProfile
{
    /// <summary>
    /// Gets or sets the unique name for this swap group.
    /// </summary>
    /// <remarks>
    /// The name is used to identify the swap group in logs and when referenced
    /// by controllers. Examples: "language-card-d000", "language-card-e000".
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the controller that manages this swap group.
    /// </summary>
    /// <remarks>
    /// The controller name references a device or component that controls
    /// which variant is active. Examples: "language-card-controller".
    /// </remarks>
    [JsonPropertyName("controller")]
    public required string Controller { get; set; }

    /// <summary>
    /// Gets or sets the base address in virtual address space as a hex string.
    /// </summary>
    /// <remarks>
    /// The address must be page-aligned (multiple of 0x1000 / 4096).
    /// Format: "0xD000", "0xE000", etc.
    /// </remarks>
    [JsonPropertyName("virtualBase")]
    public required string VirtualBase { get; set; }

    /// <summary>
    /// Gets or sets the size of the swapped region in bytes as a hex string.
    /// </summary>
    /// <remarks>
    /// The size must be page-aligned (multiple of 0x1000 / 4096).
    /// Format: "0x1000" for 4KB, "0x2000" for 8KB, etc.
    /// </remarks>
    [JsonPropertyName("size")]
    public required string Size { get; set; }

    /// <summary>
    /// Gets or sets an optional comment explaining this swap group.
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets the available variants for this swap group.
    /// </summary>
    /// <remarks>
    /// Each variant defines a different memory region that can occupy this
    /// virtual address range. At least one variant is required.
    /// </remarks>
    [JsonPropertyName("variants")]
    public required List<SwapVariantProfile> Variants { get; set; }

    /// <summary>
    /// Gets or sets the name of the default variant to select at initialization.
    /// </summary>
    /// <remarks>
    /// If not specified, the first variant in the list is selected by default.
    /// </remarks>
    [JsonPropertyName("defaultVariant")]
    public string? DefaultVariant { get; set; }
}