// <copyright file="MemoryProfileSection.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Memory configuration section of a machine profile.
/// </summary>
public sealed class MemoryProfileSection
{
    /// <summary>
    /// Gets or sets the memory size in bytes.
    /// </summary>
    /// <remarks>
    /// Use values from <see cref="MemorySizes"/> for common sizes.
    /// </remarks>
    [JsonPropertyName("size")]
    public required uint Size { get; set; }

    /// <summary>
    /// Gets or sets the memory implementation type.
    /// </summary>
    /// <remarks>
    /// Valid values:  "basic" (simple RAM).
    /// Future:  "banked", "apple2", etc.
    /// </remarks>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "basic";
}