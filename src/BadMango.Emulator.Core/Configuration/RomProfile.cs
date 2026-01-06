// <copyright file="RomProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines a ROM image configuration within a machine profile.
/// </summary>
/// <remarks>
/// <para>
/// ROM profiles define ROM images that can be loaded and mapped into the machine's
/// address space. ROMs can be referenced by name from memory regions and swap variants
/// using the <see cref="Name"/> property.
/// </para>
/// <para>
/// ROM sources support multiple path schemes:
/// <list type="bullet">
/// <item><description>"library://path" - Relative to the library root</description></item>
/// <item><description>"app://path" - Relative to the application directory</description></item>
/// <item><description>Absolute path - Used as-is</description></item>
/// <item><description>Relative path - Relative to the profile file location</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class RomProfile
{
    /// <summary>
    /// Gets or sets the unique name for this ROM.
    /// </summary>
    /// <remarks>
    /// The name is used to reference this ROM from memory regions and swap variants
    /// via <see cref="SwapVariantProfile.SourceRef"/> or <see cref="MemoryRegionProfile.SourceRef"/>.
    /// Examples: "system-rom", "video-rom", "character-rom".
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    /// <remarks>
    /// The path can use various schemes:
    /// <list type="bullet">
    /// <item><description>"library://roms/pocket2e-system.rom"</description></item>
    /// <item><description>"app://roms/monitor.bin"</description></item>
    /// <item><description>"/absolute/path/to/rom.bin"</description></item>
    /// <item><description>"relative/path/rom.bin"</description></item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("source")]
    public required string Source { get; set; }

    /// <summary>
    /// Gets or sets the expected size of the ROM as a hex string.
    /// </summary>
    /// <remarks>
    /// Used for validation. If the loaded ROM differs in size, a warning or error
    /// may be generated depending on configuration.
    /// </remarks>
    [JsonPropertyName("size")]
    public string? Size { get; set; }

    /// <summary>
    /// Gets or sets named regions within this ROM.
    /// </summary>
    /// <remarks>
    /// Allows a single ROM file to be split into multiple regions that map to
    /// different addresses. Useful for ROMs that contain multiple functional areas.
    /// </remarks>
    [JsonPropertyName("regions")]
    public List<RomRegionProfile>? Regions { get; set; }

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
    /// <item><description>"fallback" - Use stub ROM and continue</description></item>
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