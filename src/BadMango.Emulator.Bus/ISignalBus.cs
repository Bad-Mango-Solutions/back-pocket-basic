// <copyright file="ISignalBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Signal hub manages device-to-CPU lines. Devices assert/deassert; CPU samples.
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
/// </remarks>
public interface ISignalBus
{
    /// <summary>
    /// Occurs when a signal line changes state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The event provides: the signal line, the new state (true = asserted),
    /// the device ID that caused the change, and the cycle count.
    /// </para>
    /// <para>
    /// This event is fired only when the overall line state changes,
    /// not when individual devices assert or deassert if the line was
    /// already in that state.
    /// </para>
    /// </remarks>
    event Action<SignalLine, bool, int, ulong>? SignalChanged;

    /// <summary>
    /// Asserts a signal line.
    /// </summary>
    /// <param name="line">The signal line to assert.</param>
    /// <param name="deviceId">The structural ID of the device asserting the signal.</param>
    /// <remarks>
    /// <para>
    /// If the line is already asserted by another device, the assertion is
    /// counted (allowing multiple devices to hold IRQ low, for example).
    /// </para>
    /// <para>
    /// Signal transitions are recorded for tracing when enabled.
    /// </para>
    /// </remarks>
    void Assert(SignalLine line, int deviceId);

    /// <summary>
    /// Deasserts a signal line.
    /// </summary>
    /// <param name="line">The signal line to deassert.</param>
    /// <param name="deviceId">The structural ID of the device deasserting the signal.</param>
    /// <remarks>
    /// <para>
    /// For lines that support multiple asserters (like IRQ), the line remains
    /// asserted until all devices have cleared their assertions.
    /// </para>
    /// <para>
    /// Signal transitions are recorded for tracing when enabled.
    /// </para>
    /// </remarks>
    void Deassert(SignalLine line, int deviceId);

    /// <summary>
    /// Determines whether a signal line is currently asserted.
    /// </summary>
    /// <param name="line">The signal line to check.</param>
    /// <returns><see langword="true"/> if the line is asserted; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// The CPU typically samples signal lines at instruction boundaries
    /// or at specific points within instruction execution.
    /// </remarks>
    bool IsAsserted(SignalLine line);

    /// <summary>
    /// Consumes a pending NMI edge, clearing the edge-detected flag.
    /// </summary>
    /// <returns><see langword="true"/> if an NMI edge was pending; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// NMI is edge-triggered: an NMI is recognized when the line transitions
    /// from deasserted to asserted. This method is called by the CPU when
    /// it checks for pending NMI interrupts.
    /// </para>
    /// <para>
    /// The edge flag is cleared after this call, so subsequent calls will
    /// return <see langword="false"/> until another rising edge occurs.
    /// </para>
    /// </remarks>
    bool ConsumeNmiEdge();
}