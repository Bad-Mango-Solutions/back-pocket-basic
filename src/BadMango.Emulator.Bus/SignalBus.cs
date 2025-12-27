// <copyright file="SignalBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

/// <summary>
/// Default implementation of <see cref="ISignalBus"/> for signal fabric management.
/// </summary>
/// <remarks>
/// <para>
/// The signal bus manages hardware signal lines that coordinate between devices and the CPU.
/// Rather than devices directly calling CPU methods, they assert and deassert lines through
/// this signal fabric, which records transitions and allows the CPU to sample line states
/// at defined boundaries.
/// </para>
/// <para>
/// This implementation supports multiple devices asserting the same line simultaneously.
/// A line remains asserted as long as at least one device holds it low.
/// </para>
/// <para>
/// NMI is edge-triggered: the signal edge is detected when transitioning from clear to asserted,
/// and the CPU must acknowledge the NMI before another edge can be detected.
/// </para>
/// <para>
/// The signal bus also tracks CPU cycle signals for scheduler integration. When the CPU
/// signals instruction fetch or execution, the cycle counts are accumulated and can be
/// used by the scheduler to advance time.
/// </para>
/// </remarks>
public sealed class SignalBus : ISignalBus
{
    private readonly Dictionary<SignalLine, HashSet<int>> asserters = [];
    private bool nmiEdgeDetected;
    private ulong totalFetchCycles;
    private ulong totalExecuteCycles;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalBus"/> class.
    /// </summary>
    public SignalBus()
    {
        foreach (SignalLine line in Enum.GetValues<SignalLine>())
        {
            asserters[line] = [];
        }
    }

    /// <inheritdoc />
    public bool IsIrqAsserted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => asserters[SignalLine.Irq].Count > 0;
    }

    /// <inheritdoc />
    public bool IsNmiAsserted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => nmiEdgeDetected || asserters[SignalLine.Nmi].Count > 0;
    }

    /// <inheritdoc />
    public bool IsWaiting
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => asserters[SignalLine.Rdy].Count > 0;
    }

    /// <inheritdoc />
    public bool IsDmaRequested
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => asserters[SignalLine.DmaReq].Count > 0;
    }

    /// <inheritdoc />
    public ulong TotalFetchCycles
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => totalFetchCycles;
    }

    /// <inheritdoc />
    public ulong TotalExecuteCycles
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => totalExecuteCycles;
    }

    /// <inheritdoc />
    public ulong TotalCpuCycles
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => totalFetchCycles + totalExecuteCycles;
    }

    /// <inheritdoc />
    public void Assert(SignalLine line, int deviceId, ulong cycle)
    {
        var lineAsserters = asserters[line];
        bool wasAsserted = lineAsserters.Count > 0;
        lineAsserters.Add(deviceId);

        // NMI edge detection: detect rising edge (clear to asserted)
        if (line == SignalLine.Nmi && !wasAsserted)
        {
            nmiEdgeDetected = true;
        }
    }

    /// <inheritdoc />
    public void Clear(SignalLine line, int deviceId, ulong cycle)
    {
        asserters[line].Remove(deviceId);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SignalState Sample(SignalLine line)
    {
        if (line == SignalLine.Nmi)
        {
            return nmiEdgeDetected || asserters[line].Count > 0
                ? SignalState.Asserted
                : SignalState.Clear;
        }

        return asserters[line].Count > 0
            ? SignalState.Asserted
            : SignalState.Clear;
    }

    /// <inheritdoc />
    public void AcknowledgeNmi(ulong cycle)
    {
        nmiEdgeDetected = false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        foreach (var set in asserters.Values)
        {
            set.Clear();
        }

        nmiEdgeDetected = false;
        totalFetchCycles = 0;
        totalExecuteCycles = 0;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SignalInstructionFetched(ulong cycles)
    {
        totalFetchCycles += cycles;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SignalInstructionExecuted(ulong cycles)
    {
        totalExecuteCycles += cycles;
    }

    /// <inheritdoc />
    public void ResetCycleCounters()
    {
        totalFetchCycles = 0;
        totalExecuteCycles = 0;
    }
}