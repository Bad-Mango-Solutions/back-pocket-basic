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
/// Memory regions describe individual segments of the machine's address space,
/// including RAM, ROM, and I/O areas. Each region specifies its type, location,
/// size, and access permissions.
/// </para>
/// <para>
/// Regions must be page-aligned (4KB boundaries) for compatibility with the
/// bus architecture. The <see cref="Start"/> and <see cref="Size"/> properties
/// use hexadecimal strings to support large address values clearly.
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
    /// <item><description>"ram" - Read/write random access memory</description></item>
    /// <item><description>"rom" - Read-only memory</description></item>
    /// <item><description>"io" - I/O space (for future device mapping)</description></item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the starting address of this region as a hex string.
    /// </summary>
    /// <remarks>
    /// The address must be page-aligned (multiple of 0x1000 / 4096).
    /// Format: "0x0000" or "0x10000" (with or without leading zeros).
    /// </remarks>
    [JsonPropertyName("start")]
    public required string Start { get; set; }

    /// <summary>
    /// Gets or sets the size of this region in bytes as a hex string.
    /// </summary>
    /// <remarks>
    /// The size must be page-aligned (multiple of 0x1000 / 4096).
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
    /// Gets or sets the fill pattern for RAM initialization.
    /// </summary>
    /// <remarks>
    /// A hex string representing a single byte value to fill the region with.
    /// Only applicable to RAM regions. Examples: "0x00", "0xFF", "0xAA".
    /// If not specified, RAM is zero-initialized.
    /// </remarks>
    [JsonPropertyName("fill")]
    public string? Fill { get; set; }

    /// <summary>
    /// Gets or sets the source file path for ROM or RAM initialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The path can be specified using several schemes:
    /// </para>
    /// <list type="bullet">
    /// <item><description>"library://path" - Relative to the library root</description></item>
    /// <item><description>"app://path" - Relative to the application directory</description></item>
    /// <item><description>Absolute path - Used as-is</description></item>
    /// <item><description>Relative path - Relative to the profile file location</description></item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the offset within the source file to start loading from.
    /// </summary>
    /// <remarks>
    /// A hex string specifying the byte offset within the source file.
    /// Useful when loading a portion of a larger ROM image.
    /// Defaults to 0 if not specified.
    /// </remarks>
    [JsonPropertyName("sourceOffset")]
    public string? SourceOffset { get; set; }
}