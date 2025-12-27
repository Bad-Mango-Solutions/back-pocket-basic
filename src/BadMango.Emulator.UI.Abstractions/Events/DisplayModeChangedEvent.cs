// <copyright file="DisplayModeChangedEvent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Events;

/// <summary>
/// Event raised when the display mode changes.
/// </summary>
/// <param name="MachineId">The unique identifier of the machine.</param>
/// <param name="NewMode">The new display mode.</param>
public record DisplayModeChangedEvent(string MachineId, string NewMode);