// <copyright file="SignalEdge.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core.Signaling;

/// <summary>
/// Represents a signal edge transition event.
/// </summary>
/// <remarks>
/// Signal edge events are recorded by the signal fabric for tracing
/// and debugging purposes. They capture when a signal transitioned,
/// which device caused the transition, and the machine cycle.
/// </remarks>
/// <param name="Line">The signal line that transitioned.</param>
/// <param name="NewState">The state after the transition.</param>
/// <param name="DeviceId">The structural ID of the device that caused the transition.</param>
/// <param name="Cycle">The machine cycle when the transition occurred.</param>
public readonly record struct SignalEdge(
    SignalLine Line,
    SignalState NewState,
    int DeviceId,
    ulong Cycle)
{
    /// <summary>
    /// Gets a value indicating whether this is a rising edge (clear to asserted).
    /// </summary>
    public bool IsRisingEdge => NewState == SignalState.Asserted;

    /// <summary>
    /// Gets a value indicating whether this is a falling edge (asserted to clear).
    /// </summary>
    public bool IsFallingEdge => NewState == SignalState.Clear;
}