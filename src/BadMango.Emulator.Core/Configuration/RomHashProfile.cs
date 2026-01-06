// <copyright file="RomHashProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Contains hash values for ROM verification.
/// </summary>
/// <remarks>
/// <para>
/// ROM hashes allow verification of ROM integrity before loading.
/// This is important for ensuring correct behavior as different ROM
/// versions may have subtle differences that affect emulation accuracy.
/// </para>
/// <para>
/// Both SHA-256 and MD5 hashes are supported. SHA-256 is preferred for
/// security, but MD5 is included for compatibility with existing ROM
/// databases and tools.
/// </para>
/// </remarks>
public sealed class RomHashProfile
{
    /// <summary>
    /// Gets or sets the SHA-256 hash of the ROM file.
    /// </summary>
    /// <remarks>
    /// A 64-character hexadecimal string representing the SHA-256 digest.
    /// This is the preferred hash algorithm for verification.
    /// </remarks>
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    /// <summary>
    /// Gets or sets the MD5 hash of the ROM file.
    /// </summary>
    /// <remarks>
    /// A 32-character hexadecimal string representing the MD5 digest.
    /// Included for compatibility with existing ROM databases.
    /// </remarks>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }
}