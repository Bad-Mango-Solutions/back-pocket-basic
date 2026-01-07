// <copyright file="MachineProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines a machine configuration profile for the emulator.
/// </summary>
/// <remarks>
/// <para>
/// Machine profiles describe the hardware configuration of an emulated system,
/// including CPU type, memory layout, expansion slots, devices, and boot options.
/// Profiles are loaded from JSON files and support complex machines like the Pocket2e.
/// </para>
/// <para>
/// The memory configuration uses a three-tier model:
/// <list type="number">
/// <item><description>ROM Images - External binary files to load</description></item>
/// <item><description>Physical Memory - Backing stores that hold the actual data</description></item>
/// <item><description>Regions - Virtual address mappings to physical memory</description></item>
/// </list>
/// </para>
/// <para>
/// For 6502/65C02 systems, the address space is 16 bits (64KB). Bank switching
/// (Language Card, Auxiliary Memory) is achieved through soft switches that remap
/// regions within that 64KB space—NOT through extended addressing.
/// </para>
/// </remarks>
public sealed class MachineProfile
{
    /// <summary>
    /// Gets or sets the JSON schema reference.
    /// </summary>
    /// <remarks>
    /// Example: "../schemas/machine-profile.schema.json".
    /// </remarks>
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this profile.
    /// </summary>
    /// <remarks>
    /// Used as the key when referencing profiles (e.g., "simple-65c02", "pocket2e").
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
    /// Defaults to 16 bits (64KB) if not specified. For 6502/65C02 systems,
    /// this should always be 16. Bank switching is achieved via soft switches,
    /// NOT extended addressing.
    /// </para>
    /// </remarks>
    [JsonPropertyName("addressSpace")]
    public int AddressSpace { get; set; } = 16;

    /// <summary>
    /// Gets or sets the memory configuration.
    /// </summary>
    [JsonPropertyName("memory")]
    public required MemoryProfileSection Memory { get; set; }

    /// <summary>
    /// Gets or sets the expansion slot system configuration.
    /// </summary>
    /// <remarks>
    /// Defines slot cards for slots 1-7 and slot ROM behavior options.
    /// </remarks>
    [JsonPropertyName("slots")]
    public SlotSystemProfile? Slots { get; set; }

    /// <summary>
    /// Gets or sets the device configuration.
    /// </summary>
    /// <remarks>
    /// Configures built-in motherboard devices: keyboard, speaker, video, game I/O.
    /// </remarks>
    [JsonPropertyName("devices")]
    public DevicesProfile? Devices { get; set; }

    /// <summary>
    /// Gets or sets the boot configuration.
    /// </summary>
    /// <remarks>
    /// Controls auto-start behavior and startup slot selection.
    /// </remarks>
    [JsonPropertyName("boot")]
    public BootProfile? Boot { get; set; }
}