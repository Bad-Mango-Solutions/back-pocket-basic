// <copyright file="SlotSystemProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Defines the expansion slot system configuration for a machine profile.
/// </summary>
/// <remarks>
/// <para>
/// The slot system manages peripheral expansion cards in slots 1-7. Each slot
/// provides 16 bytes of I/O space ($C0n0-$C0nF), 256 bytes of ROM space
/// ($Cn00-$CnFF), and shared access to the 2KB expansion ROM ($C800-$CFFF).
/// </para>
/// <para>
/// The slot system supports internal ROM modes that can override slot ROM:
/// <list type="bullet">
/// <item><description>InternalC3Rom: Use internal 80-column ROM for $C300 region</description></item>
/// <item><description>InternalCxRom: Use internal ROM for entire $C100-$CFFF region</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class SlotSystemProfile
{
    /// <summary>
    /// Gets or sets the name of the composite region that handles I/O.
    /// </summary>
    /// <remarks>
    /// This references a composite region defined in the memory regions array.
    /// The slot manager uses this to find and configure the I/O page handler.
    /// Example: "io-page".
    /// </remarks>
    [JsonPropertyName("io-region")]
    public string? IoRegion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the slot system is enabled.
    /// </summary>
    /// <remarks>
    /// When disabled, no expansion cards can be installed and the I/O region
    /// may behave differently. Defaults to <see langword="true"/>.
    /// </remarks>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use internal ROM for the $C300 region.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the internal 80-column firmware is used for
    /// slot 3's ROM region ($C300-$C3FF). This is the default for enhanced IIe.
    /// </remarks>
    [JsonPropertyName("internalC3Rom")]
    public bool InternalC3Rom { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use internal ROM for entire $Cx region.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the internal ROM is used for the entire
    /// $C100-$CFFF region, overriding all slot ROMs. Defaults to <see langword="false"/>.
    /// </remarks>
    [JsonPropertyName("internalCxRom")]
    public bool InternalCxRom { get; set; } = false;

    /// <summary>
    /// Gets or sets the cards installed in slots.
    /// </summary>
    /// <remarks>
    /// Each card specifies a slot number (1-7), card type, and optional configuration.
    /// Multiple cards can be defined, but each slot can only have one card.
    /// </remarks>
    [JsonPropertyName("cards")]
    public List<SlotCardProfile>? Cards { get; set; }
}