// <copyright file="ISignalBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Signal fabric interface for IRQ, NMI, DMA, and other control lines.
/// </summary>
/// <remarks>
/// <para>
/// The signal bus manages hardware signal lines that coordinate between
/// devices and the CPU. Rather than devices directly calling CPU methods
/// like <c>cpu.RaiseIrq()</c>, they assert and deassert lines through
/// the signal fabric, which records transitions and allows the CPU to
/// sample line states at defined boundaries.
/// </para>
/// <para>
/// This architecture makes timing bugs debuggable by providing a clear
/// record of who asserted what and when, avoiding "spooky action at a distance"
/// in interrupt handling.
/// </para>
/// <para>
/// The signal bus also tracks CPU cycle signals, allowing the scheduler
/// to be driven by CPU instruction execution. The CPU signals when it
/// fetches and executes instructions, providing cycle counts that the
/// scheduler uses to advance time.
/// </para>
/// </remarks>
public interface ISignalBus
{
    /// <summary>
    /// Gets a value indicating whether the IRQ line is currently asserted.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if IRQ is asserted; otherwise, <see langword="false"/>.
    /// </value>
    bool IsIrqAsserted { get; }

    /// <summary>
    /// Gets a value indicating whether the NMI line is currently asserted.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if NMI is asserted; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// NMI is edge-triggered, so implementers should track both the current
    /// level and whether an edge has been detected since the last acknowledgment.
    /// </remarks>
    bool IsNmiAsserted { get; }

    /// <summary>
    /// Gets a value indicating whether the RDY line is deasserted (CPU should wait).
    /// </summary>
    /// <value>
    /// <see langword="true"/> if RDY is low (CPU should wait);
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool IsWaiting { get; }

    /// <summary>
    /// Gets a value indicating whether a DMA request is pending.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if DMA is requested; otherwise, <see langword="false"/>.
    /// </value>
    bool IsDmaRequested { get; }

    /// <summary>
    /// Gets the total number of cycles consumed by instruction fetches.
    /// </summary>
    /// <value>The cumulative cycle count from all instruction fetch signals.</value>
    /// <remarks>
    /// This counter accumulates cycles reported via <see cref="SignalInstructionFetched"/>
    /// and is reset when <see cref="Reset"/> or <see cref="ResetCycleCounters"/> is called.
    /// </remarks>
    ulong TotalFetchCycles { get; }

    /// <summary>
    /// Gets the total number of cycles consumed by instruction execution.
    /// </summary>
    /// <value>The cumulative cycle count from all instruction execution signals.</value>
    /// <remarks>
    /// This counter accumulates cycles reported via <see cref="SignalInstructionExecuted"/>
    /// and is reset when <see cref="Reset"/> or <see cref="ResetCycleCounters"/> is called.
    /// </remarks>
    ulong TotalExecuteCycles { get; }

    /// <summary>
    /// Gets the total number of CPU cycles (fetch + execute combined).
    /// </summary>
    /// <value>The sum of <see cref="TotalFetchCycles"/> and <see cref="TotalExecuteCycles"/>.</value>
    ulong TotalCpuCycles { get; }

    /// <summary>
    /// Asserts a signal line.
    /// </summary>
    /// <param name="line">The signal line to assert.</param>
    /// <param name="deviceId">The structural ID of the device asserting the signal.</param>
    /// <param name="cycle">The current machine cycle.</param>
    /// <remarks>
    /// <para>
    /// If the line is already asserted by another device, the assertion is
    /// counted (allowing multiple devices to hold IRQ low, for example).
    /// </para>
    /// <para>
    /// Signal transitions are recorded for tracing when enabled.
    /// </para>
    /// </remarks>
    void Assert(SignalLine line, int deviceId, ulong cycle);

    /// <summary>
    /// Clears (deasserts) a signal line.
    /// </summary>
    /// <param name="line">The signal line to clear.</param>
    /// <param name="deviceId">The structural ID of the device clearing the signal.</param>
    /// <param name="cycle">The current machine cycle.</param>
    /// <remarks>
    /// <para>
    /// For lines that support multiple asserters (like IRQ), the line remains
    /// asserted until all devices have cleared their assertions.
    /// </para>
    /// <para>
    /// Signal transitions are recorded for tracing when enabled.
    /// </para>
    /// </remarks>
    void Clear(SignalLine line, int deviceId, ulong cycle);

    /// <summary>
    /// Samples the current state of a signal line.
    /// </summary>
    /// <param name="line">The signal line to sample.</param>
    /// <returns>The current state of the signal line.</returns>
    /// <remarks>
    /// The CPU typically samples signal lines at instruction boundaries
    /// or at specific points within instruction execution.
    /// </remarks>
    SignalState Sample(SignalLine line);

    /// <summary>
    /// Acknowledges an NMI, clearing the edge-detected flag.
    /// </summary>
    /// <param name="cycle">The current machine cycle.</param>
    /// <remarks>
    /// Called by the CPU when it begins processing an NMI. This allows
    /// the signal fabric to track that the interrupt has been serviced
    /// and prepare for the next edge detection.
    /// </remarks>
    void AcknowledgeNmi(ulong cycle);

    /// <summary>
    /// Resets all signal lines to their default (deasserted) state.
    /// </summary>
    /// <remarks>
    /// Called during system reset to ensure all signals start in a known state.
    /// </remarks>
    void Reset();

    /// <summary>
    /// Signals that the CPU has fetched an instruction.
    /// </summary>
    /// <param name="cycles">The number of cycles consumed by the fetch operation.</param>
    /// <remarks>
    /// <para>
    /// Called by the CPU after fetching an instruction's opcode and operands.
    /// The cycle count represents the memory access cycles required for the fetch.
    /// </para>
    /// <para>
    /// This signal is used by the scheduler to track time progression and
    /// maintain cycle-accurate emulation.
    /// </para>
    /// </remarks>
    void SignalInstructionFetched(ulong cycles);

    /// <summary>
    /// Signals that the CPU has executed an instruction.
    /// </summary>
    /// <param name="cycles">The number of cycles consumed by the execution.</param>
    /// <remarks>
    /// <para>
    /// Called by the CPU after completing instruction execution.
    /// The cycle count represents the execution cycles (which may include
    /// additional memory accesses for addressing modes).
    /// </para>
    /// <para>
    /// This signal is used by the scheduler to track time progression and
    /// maintain cycle-accurate emulation. The scheduler will advance its
    /// current cycle based on these signals.
    /// </para>
    /// </remarks>
    void SignalInstructionExecuted(ulong cycles);

    /// <summary>
    /// Resets the CPU cycle counters without affecting signal line states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method resets <see cref="TotalFetchCycles"/>, <see cref="TotalExecuteCycles"/>,
    /// and <see cref="TotalCpuCycles"/> to zero without clearing signal line assertions.
    /// </para>
    /// <para>
    /// Use this when you need to measure cycles for a specific execution span
    /// without disturbing interrupt handling state.
    /// </para>
    /// </remarks>
    void ResetCycleCounters();
}