// <copyright file="BreakpointHitEvent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Events;

/// <summary>
/// Event raised when a breakpoint is hit during execution.
/// </summary>
/// <param name="MachineId">The unique identifier of the machine.</param>
/// <param name="Address">The address where the breakpoint was hit.</param>
public record BreakpointHitEvent(string MachineId, int Address);