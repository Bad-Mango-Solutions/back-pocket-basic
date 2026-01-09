// <copyright file="SoftSwitchState.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents a named soft switch state value.
/// </summary>
/// <param name="Name">The display name of the soft switch.</param>
/// <param name="Address">The base address of the soft switch (e.g., $C080 for Language Card).</param>
/// <param name="Value">The boolean state of the switch.</param>
/// <param name="Description">A brief description of what this switch controls.</param>
public readonly record struct SoftSwitchState(
    string Name,
    ushort Address,
    bool Value,
    string? Description = null);