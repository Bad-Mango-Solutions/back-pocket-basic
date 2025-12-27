// <copyright file="MachineStateChangedEvent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Events;

/// <summary>
/// Event raised when a machine's state changes.
/// </summary>
/// <param name="MachineId">The unique identifier of the machine.</param>
/// <param name="NewState">The new state of the machine.</param>
public record MachineStateChangedEvent(string MachineId, string NewState);