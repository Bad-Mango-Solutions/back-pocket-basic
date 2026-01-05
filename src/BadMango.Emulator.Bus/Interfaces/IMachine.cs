// <copyright file="IMachine.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

using BadMango.Emulator.Core.Cpu;

using Core.Interfaces.Cpu;

/// <summary>
/// A running machine instance — a component bucket with lifecycle.
/// Extends <see cref="IEventContext"/> with CPU access, lifecycle control, and extended queries.
/// </summary>
/// <remarks>
/// <para>
/// The machine interface provides a unified abstraction for the assembled emulator system.
/// It exposes the CPU and memory bus for high-level machine control and debugging.
/// </para>
/// <para>
/// The machine is a component bucket, not a typed hierarchy. Machine-specific configuration
/// lives in extension methods. Any component can be registered and retrieved by type.
/// </para>
/// <para>
/// Lifecycle operations (Run, Stop, Step, Reset) affect the machine state and trigger
/// the <see cref="StateChanged"/> event when the state transitions.
/// </para>
/// </remarks>
public interface IMachine : IEventContext
{
    // ─── Events ─────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when the machine state changes.
    /// </summary>
    /// <remarks>
    /// The event provides the new <see cref="MachineState"/> value. This event
    /// is raised after the state transition is complete.
    /// </remarks>
    event Action<MachineState>? StateChanged;

    // ─── Core Components ────────────────────────────────────────────────

    /// <summary>
    /// Gets the CPU instance.
    /// </summary>
    /// <value>The CPU attached to this machine.</value>
    ICpu Cpu { get; }

    /// <summary>
    /// Gets the device registry.
    /// </summary>
    /// <value>The registry containing all device metadata for this machine.</value>
    IDeviceRegistry Devices { get; }

    // ─── Lifecycle ──────────────────────────────────────────────────────

    /// <summary>
    /// Gets the current machine state.
    /// </summary>
    /// <value>The current lifecycle state of the machine.</value>
    MachineState State { get; }

    /// <summary>
    /// Resets the machine by asserting the RESET signal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reset transitions the machine to <see cref="MachineState.Stopped"/> and
    /// reinitializes the CPU and devices. The RESET signal is asserted through
    /// the signal bus, allowing devices to respond to the reset appropriately.
    /// </para>
    /// <para>
    /// After reset, the CPU will read the reset vector and be ready to execute
    /// from the reset handler address.
    /// </para>
    /// </remarks>
    void Reset();

    /// <summary>
    /// Starts continuous execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transitions the machine to <see cref="MachineState.Running"/> and begins
    /// executing instructions in a loop until stopped via <see cref="Stop"/> or
    /// a breakpoint is hit.
    /// </para>
    /// <para>
    /// If the machine is already running, this method has no effect.
    /// </para>
    /// </remarks>
    void Run();

    /// <summary>
    /// Stops execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transitions the machine to <see cref="MachineState.Stopped"/> and halts
    /// instruction execution at the next safe boundary.
    /// </para>
    /// <para>
    /// If the machine is already stopped, this method has no effect.
    /// </para>
    /// </remarks>
    void Stop();

    /// <summary>
    /// Executes a single instruction.
    /// </summary>
    /// <returns>A <see cref="CpuStepResult"/> containing the run state and cycles consumed.</returns>
    /// <remarks>
    /// <para>
    /// Single-steps the CPU through one instruction. If the machine was running,
    /// it transitions to <see cref="MachineState.Paused"/> after the step.
    /// </para>
    /// <para>
    /// The scheduler is advanced by the number of cycles consumed, and any
    /// events that become due are dispatched.
    /// </para>
    /// </remarks>
    CpuStepResult Step();
}