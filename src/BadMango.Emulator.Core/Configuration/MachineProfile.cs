// <copyright file="MachineProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines a machine configuration profile for the emulator.
/// </summary>
/// <remarks>
/// Machine profiles describe the hardware configuration of an emulated system,
/// including CPU type, memory size, and other hardware characteristics.
/// Profiles are loaded from JSON files and can be extended as the emulator evolves.
/// </remarks>
public sealed class MachineProfile
{
    /// <summary>
    /// Gets or sets the unique identifier for this profile.
    /// </summary>
    /// <remarks>
    /// Used as the key when referencing profiles (e.g., "simple-65c02", "apple2e").
    /// Should be lowercase with hyphens, no spaces.
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the human-readable display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the profile.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the CPU configuration.
    /// </summary>
    [JsonPropertyName("cpu")]
    public required CpuProfileSection Cpu { get; set; }

    /// <summary>
    /// Gets or sets the address space size in bits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Common values:
    /// </para>
    /// <list type="bullet">
    /// <item><description>16 bits = 64KB (6502, 65C02)</description></item>
    /// <item><description>24 bits = 16MB (65C816)</description></item>
    /// <item><description>32 bits = 4GB (65832)</description></item>
    /// </list>
    /// <para>
    /// Defaults to 16 bits (64KB) if not specified.
    /// </para>
    /// </remarks>
    [JsonPropertyName("addressSpace")]
    public int AddressSpace { get; set; } = 16;

    /// <summary>
    /// Gets or sets the memory configuration.
    /// </summary>
    [JsonPropertyName("memory")]
    public required MemoryProfileSection Memory { get; set; }

    // Additional hardware profile sections (for example, ROM, I/O, or display configuration)
    // can be introduced here in future versions of the emulator.
}