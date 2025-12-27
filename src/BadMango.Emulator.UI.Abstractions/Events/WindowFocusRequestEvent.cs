// <copyright file="WindowFocusRequestEvent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Events;

/// <summary>
/// Event raised to request focus on a specific component window.
/// </summary>
/// <param name="ComponentType">The type of component to focus.</param>
/// <param name="MachineId">The optional machine ID for machine-specific windows.</param>
public record WindowFocusRequestEvent(PopOutComponent ComponentType, string? MachineId);