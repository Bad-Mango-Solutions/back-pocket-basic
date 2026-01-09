// <copyright file="CpuProfileSection.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// CPU configuration section of a machine profile.
/// </summary>
public sealed class CpuProfileSection
{
    /// <summary>
    /// Gets or sets the CPU type identifier.
    /// </summary>
    /// <remarks>
    /// Valid values:  "6502", "65C02", "65816", "65832".
    /// Currently only "65C02" is fully implemented.
    /// </remarks>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the CPU clock speed in Hz.
    /// </summary>
    /// <remarks>
    /// Optional. Used for cycle-accurate timing calculations.
    /// Default is 1 MHz (1,000,000 Hz) if not specified.
    /// </remarks>
    [JsonPropertyName("clockSpeed")]
    public long? ClockSpeed { get; set; }
}