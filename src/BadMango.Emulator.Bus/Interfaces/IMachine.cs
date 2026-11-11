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
/// <para>
/// The machine's CPU, scheduler, signal bus and devices are owned by its machine thread
/// (see <see cref="IMachineDispatcher"/>). Lifecycle methods are safe to call from any
/// thread; other host-side mutation must be marshalled with <see cref="IMachineDispatcher.Post"/>.
/// </para>
/// </remarks>
public interface IMachine : IEventContext, IMachineDispatcher
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

    // ─── Timing ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the target CPU clock speed in Hz for cycle-accurate throttling.
    /// </summary>
    /// <value>
    /// The target clock speed in Hz. A value of <c>0</c> disables throttling and
    /// allows the CPU to run at maximum host speed.
    /// </value>
    /// <remarks>
    /// <para>
    /// The Apple IIe uses a ~1.020484 MHz clock (1,020,484 Hz). When throttling is
    /// enabled, the execution loop paces CPU cycles to match wall-clock time at
    /// the configured rate. This ensures that cycle-dependent firmware (such as
    /// the 80-column card's PAGE1/PAGE2 switching) operates within its designed
    /// timing window, preventing display artifacts.
    /// </para>
    /// <para>
    /// This property can be changed at runtime to adjust the emulated CPU speed
    /// (e.g., for Apple IIgs emulation where the user could switch between
    /// 1 MHz and 2.8 MHz modes via the control panel).
    /// </para>
    /// </remarks>
    long ClockSpeedHz { get; set; }

    // ─── Lifecycle ──────────────────────────────────────────────────────

    /// <summary>
    /// Gets the current machine state.
    /// </summary>
    /// <value>The current lifecycle state of the machine.</value>
    MachineState State { get; }

    /// <summary>
    /// Resets the machine by asserting the RESET signal.
    /// </summary>
    /// <param name="hardReset">
    /// When <see langword="true"/>, clears all registered hard-reset RAM backing stores
    /// before device and CPU reset. When <see langword="false"/>, preserves RAM contents.
    /// </param>
    /// <remarks>
    /// <para>
    /// Reset is modeled as a hardware-like RESET signal assertion and release, using the
    /// machine's own RESET source. It works like the real button: if the machine is running,
    /// it keeps running through the reset sequence into the ROM reset handler; if it is paused
    /// or stopped, the state is unchanged and execution stops with PC at the reset vector
    /// after the hold window and the reset sequence.
    /// </para>
    /// <para>
    /// After reset release, the CPU reads the reset vector through the post-reset banking and
    /// continues from the reset handler address. The first reset after the machine is built
    /// (and every hard reset) is a power-on reset: it initializes the CPU registers to their
    /// power-on state instead of running the warm reset sequence.
    /// </para>
    /// <para>
    /// Safe to call from any thread: while the machine is running, the reset is handed to the
    /// run loop and applied at the next instruction boundary, and this method returns without
    /// waiting for it.
    /// </para>
    /// </remarks>
    void Reset(bool hardReset = false);

    /// <summary>
    /// Pulses RESET through an external source (for example the keyboard's Reset key).
    /// </summary>
    /// <param name="assertLine">Drives the RESET line low through the source.</param>
    /// <param name="releaseLine">Releases the source.</param>
    /// <param name="hardReset">When <see langword="true"/>, performs a power-on reset while the line is held.</param>
    /// <remarks>
    /// <para>
    /// Both callbacks run on the machine thread. The line is held for the minimum hold window
    /// and then released, following the same run-state rules as <see cref="Reset"/>. If
    /// <paramref name="assertLine"/> does not actually drive the line low (for example the
    /// keyboard requires Control), no reset occurs and the CPU keeps executing.
    /// </para>
    /// </remarks>
    void PulseReset(Action assertLine, Action releaseLine, bool hardReset = false);

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
    /// instruction execution at the next safe boundary. Waits for the run loop to exit,
    /// unless called from the machine thread itself (for example a scheduled callback),
    /// where it only requests the stop.
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
    /// it transitions to <see cref="MachineState.Paused"/> after the step, and the
    /// step waits until the run loop has exited.
    /// </para>
    /// <para>
    /// When called from inside a CPU step (a scheduled callback), the CPU cannot be stepped
    /// re-entrantly: the machine pauses, the step is performed once the current step has
    /// unwound, and the returned result reports zero cycles.
    /// </para>
    /// <para>
    /// The scheduler is advanced by the number of cycles consumed, and any
    /// events that become due are dispatched.
    /// </para>
    /// </remarks>
    CpuStepResult Step();

    /// <summary>
    /// Starts continuous execution asynchronously with periodic yields.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>A task that completes when execution stops.</returns>
    /// <remarks>
    /// <para>
    /// This method is similar to <see cref="Run"/> but uses an async/cooperative
    /// execution model that yields periodically, allowing the caller to remain
    /// responsive to commands while the machine is running.
    /// </para>
    /// <para>
    /// The machine transitions to <see cref="MachineState.Running"/> at start and
    /// to <see cref="MachineState.Stopped"/> when execution ends (via cancellation,
    /// halt instruction, or breakpoint).
    /// </para>
    /// <para>
    /// If the machine is already running this method has no effect; if a previous run loop is
    /// still winding down, it waits for that loop to exit first. Calls from the machine thread
    /// itself (a scheduled callback or posted action) have no effect.
    /// </para>
    /// </remarks>
    Task RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses execution without fully stopping.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transitions the machine from <see cref="MachineState.Running"/> to
    /// <see cref="MachineState.Paused"/>. The CPU and scheduler state are
    /// preserved, allowing execution to be resumed with <see cref="ResumeAsync"/>.
    /// Waits for the run loop to exit, unless called from the machine thread itself.
    /// </para>
    /// <para>
    /// If the machine is not running, this method has no effect.
    /// </para>
    /// </remarks>
    void Pause();

    /// <summary>
    /// Resumes execution from a paused state.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>A task that completes when execution stops.</returns>
    /// <remarks>
    /// <para>
    /// Transitions the machine from <see cref="MachineState.Paused"/> to
    /// <see cref="MachineState.Running"/> and resumes execution from where
    /// it was paused.
    /// </para>
    /// <para>
    /// If the machine is not paused, this method has no effect. If the previous run loop is
    /// still winding down, it waits for that loop to exit first, so two loops never execute
    /// one CPU. Like <see cref="RunAsync"/>, it has no effect when called from the machine
    /// thread itself.
    /// </para>
    /// </remarks>
    Task ResumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Boots the machine by performing a reset and starting execution.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>A task that completes when execution stops.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method that combines <see cref="Reset"/> and
    /// <see cref="RunAsync"/>. It resets the machine to its initial state
    /// and immediately starts execution.
    /// </para>
    /// </remarks>
    Task BootAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Halts the machine completely, equivalent to the STP instruction effect.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Forces the CPU into a halted state (similar to executing STP).
    /// The machine transitions to <see cref="MachineState.Stopped"/> and
    /// can only be restarted via <see cref="Reset"/>.
    /// </para>
    /// <para>
    /// Unlike <see cref="Stop"/> which simply pauses execution, <see cref="Halt"/>
    /// represents a hard stop that requires a reset to recover from.
    /// </para>
    /// </remarks>
    void Halt();
}