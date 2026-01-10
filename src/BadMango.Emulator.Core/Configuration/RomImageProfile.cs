// <copyright file="RomImageProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines a ROM image that can be loaded into physical memory.
/// </summary>
/// <remarks>
/// <para>
/// ROM images define external binary files or embedded resources that contain firmware,
/// operating system code, or other read-only data. They are loaded into physical memory
/// blocks at specified offsets during machine initialization.
/// </para>
/// <para>
/// ROM sources support multiple path schemes:
/// <list type="bullet">
/// <item><description>"library://path" - Relative to the library root (~/.backpocket)</description></item>
/// <item><description>"app://path" - Relative to the application directory</description></item>
/// <item><description>"embedded://AssemblyName/Resource.Name" - Embedded resource in an assembly</description></item>
/// <item><description>Absolute path - Used as-is</description></item>
/// <item><description>Relative path - Relative to the profile file location</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class RomImageProfile
{
    /// <summary>
    /// Gets or sets the unique name for this ROM image.
    /// </summary>
    /// <remarks>
    /// This name is used to reference the ROM image from physical memory source entries.
    /// Examples: "monitor", "basic", "character-rom".
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the source file path or embedded resource URI.
    /// </summary>
    /// <remarks>
    /// The path can use various schemes:
    /// <list type="bullet">
    /// <item><description>"library://roms/monitor.rom" - File in user's library</description></item>
    /// <item><description>"app://roms/basic.bin" - File relative to application</description></item>
    /// <item><description>"embedded://BadMango.Emulator.Devices/BadMango.Emulator.Devices.Resources.charset.rom" - Embedded resource</description></item>
    /// <item><description>"/absolute/path/to/rom.bin" - Absolute file path</description></item>
    /// <item><description>"relative/path/rom.bin" - Relative to profile location</description></item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("source")]
    public required string Source { get; set; }

    /// <summary>
    /// Gets or sets the expected size of the ROM image as a hex string.
    /// </summary>
    /// <remarks>
    /// Used for validation and to determine how much data to read.
    /// If the loaded ROM differs in size, an error may be generated.
    /// </remarks>
    [JsonPropertyName("size")]
    public required string Size { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this ROM is required for boot.
    /// </summary>
    /// <remarks>
    /// Required ROMs must be present and valid for the machine to boot.
    /// Defaults to <see langword="true"/>.
    /// </remarks>
    [JsonPropertyName("required")]
    public bool Required { get; set; } = true;

    /// <summary>
    /// Gets or sets the action to take when hash verification fails.
    /// </summary>
    /// <remarks>
    /// Valid values:
    /// <list type="bullet">
    /// <item><description>"stop" - Fail to boot if verification fails</description></item>
    /// <item><description>"fallback" - Use blank ROM and continue</description></item>
    /// </list>
    /// Defaults to "stop".
    /// </remarks>
    [JsonPropertyName("on_verification_fail")]
    public string OnVerificationFail { get; set; } = "stop";

    /// <summary>
    /// Gets or sets hash values for ROM verification.
    /// </summary>
    /// <remarks>
    /// If specified, the loaded ROM's hash is compared against these values
    /// to verify integrity. Both SHA256 and MD5 are supported.
    /// </remarks>
    [JsonPropertyName("hash")]
    public RomHashProfile? Hash { get; set; }
}