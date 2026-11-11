// <copyright file="ResetKind.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

using System.Text.Json.Serialization;

/// <summary>
/// The reset mode for a machine.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ResetKind>))]
public enum ResetKind
{
    /// <summary>
    /// A warm reset that preserves program state where possible.
    /// </summary>
    Warm,

    /// <summary>
    /// A hard reset that clears machine state.
    /// </summary>
    Hard,

    /// <summary>
    /// A power-on reset that reinitializes the machine from boot state.
    /// </summary>
    PowerOn,
}