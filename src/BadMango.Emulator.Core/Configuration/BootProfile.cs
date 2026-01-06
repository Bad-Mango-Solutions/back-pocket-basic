// <copyright file="BootProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines boot configuration for a machine profile.
/// </summary>
/// <remarks>
/// <para>
/// The boot section controls how the machine starts up, including whether
/// to automatically start the machine after loading and which slot to boot from.
/// </para>
/// </remarks>
public sealed class BootProfile
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically start the machine after loading.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the machine begins execution immediately after
    /// configuration is complete. Defaults to <see langword="true"/>.
    /// </remarks>
    [JsonPropertyName("autoStart")]
    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// Gets or sets the slot to boot from.
    /// </summary>
    /// <remarks>
    /// If specified, the machine attempts to boot from this slot (1-7).
    /// Typically slot 6 for disk drives. If <see langword="null"/>, the machine
    /// uses the default boot sequence.
    /// </remarks>
    [JsonPropertyName("startupSlot")]
    public int? StartupSlot { get; set; }
}