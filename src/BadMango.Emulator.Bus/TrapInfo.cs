// <copyright file="TrapInfo.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core;

/// <summary>
/// Metadata for a registered trap.
/// </summary>
/// <remarks>
/// <para>
/// This provides information about a trap for tooling, debugging, and diagnostics.
/// Each trap has an address, name, category, optional description, and enabled state.
/// </para>
/// </remarks>
/// <param name="Address">The ROM address where the trap is registered.</param>
/// <param name="Name">Human-readable name for the trap (e.g., "HOME", "COUT").</param>
/// <param name="Category">Classification of the trap type.</param>
/// <param name="Description">Optional description for tooling.</param>
/// <param name="Enabled"><see langword="true"/> if the trap is currently enabled.</param>
public readonly record struct TrapInfo(
    Addr Address,
    string Name,
    TrapCategory Category,
    string? Description,
    bool Enabled);