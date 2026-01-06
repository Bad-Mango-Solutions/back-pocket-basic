// <copyright file="MemoryControllerProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Defines a memory controller that manages dynamic regions at runtime.
/// </summary>
/// <remarks>
/// <para>
/// Memory controllers are responsible for spawning and managing memory regions
/// at runtime. They handle complex memory configurations like auxiliary memory
/// which can be bank-switched into the main address space through soft switches.
/// </para>
/// <para>
/// For example, the "pocket2e-aux-controller" manages 64KB of auxiliary RAM
/// that can be selectively mapped into the 16-bit address space via soft switches
/// like ALTZP, RAMRD, RAMWRT, etc.
/// </para>
/// </remarks>
public sealed class MemoryControllerProfile
{
    /// <summary>
    /// Gets or sets the unique name for this controller.
    /// </summary>
    /// <remarks>
    /// The name is used to identify the controller in logs and when referenced
    /// by swap groups. Examples: "aux-memory", "language-card-controller".
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the controller type identifier.
    /// </summary>
    /// <remarks>
    /// The type determines which controller implementation to instantiate.
    /// Examples: "pocket2e-aux-controller", "language-card-controller".
    /// </remarks>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the total size of memory managed by this controller as a hex string.
    /// </summary>
    /// <remarks>
    /// For auxiliary memory controllers, this is typically "0x10000" (64KB).
    /// </remarks>
    [JsonPropertyName("size")]
    public string? Size { get; set; }

    /// <summary>
    /// Gets or sets an optional comment explaining this controller.
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets sub-regions managed by this controller.
    /// </summary>
    /// <remarks>
    /// Some controllers may define specific regions they manage. If not specified,
    /// the controller creates regions internally based on its type.
    /// </remarks>
    [JsonPropertyName("regions")]
    public List<MemoryRegionProfile>? Regions { get; set; }

    /// <summary>
    /// Gets or sets controller-specific configuration.
    /// </summary>
    /// <remarks>
    /// This is a loosely-typed configuration object that allows controller-specific
    /// options without enumerating every possible configuration in the schema.
    /// </remarks>
    [JsonPropertyName("config")]
    public JsonElement? Config { get; set; }
}