// <copyright file="SignalBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Signaling;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Core.Interfaces.Signaling;

/// <summary>
/// Implementation tracks multiple asserters per line.
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
/// A line remains asserted as long as at least one device holds it asserted.
/// </para>
/// <para>
/// NMI is edge-triggered: the signal edge is detected when transitioning from deasserted to asserted,
/// and the CPU must consume the edge before another edge can be detected.
/// </para>
/// </remarks>
public sealed class SignalBus : ISignalBus
{
    private readonly HashSet<int>[] asserters;
    private bool nmiEdgePending;

    /// <summary>
    /// Cached <c>nmiEdgePending || IRQ asserted</c>, maintained on every state change
    /// so the CPU can test a single field per instruction.
    /// </summary>
    private bool anyInterruptPending;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalBus"/> class.
    /// </summary>
    public SignalBus()
    {
        asserters = new HashSet<int>[Enum.GetValues<SignalLine>().Length];
        for (int i = 0; i < asserters.Length; i++)
        {
            asserters[i] = [];
        }
    }

    /// <inheritdoc />
    public event Action<SignalLine, bool, int, Cycle>? SignalChanged;

    /// <summary>
    /// Gets a value indicating whether the CPU may have an interrupt to service:
    /// an unconsumed NMI edge is pending, or the IRQ line is asserted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a cheap per-instruction pre-check. When it is <see langword="false"/>,
    /// <see cref="ConsumeNmiEdge"/> would return <see langword="false"/> and
    /// <see cref="IsAsserted"/> for <see cref="SignalLine.IRQ"/> would return
    /// <see langword="false"/>, so the CPU can skip its interrupt check entirely.
    /// </para>
    /// <para>
    /// It does not consider the CPU's I flag: an asserted but masked IRQ still
    /// reports <see langword="true"/>, and the CPU performs the full check.
    /// </para>
    /// </remarks>
    public bool AnyInterruptPending
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => anyInterruptPending;
    }

    /// <inheritdoc />
    public void Assert(SignalLine line, int deviceId, Cycle cycle)
    {
        bool wasAsserted = IsAsserted(line);
        asserters[(int)line].Add(deviceId);
        bool nowAsserted = IsAsserted(line);

        // NMI edge detection (deasserted-to-asserted transition)
        if (line == SignalLine.NMI && !wasAsserted && nowAsserted)
        {
            nmiEdgePending = true;
        }

        UpdateAnyInterruptPending();

        if (wasAsserted != nowAsserted)
        {
            SignalChanged?.Invoke(line, nowAsserted, deviceId, cycle);
        }
    }

    /// <inheritdoc />
    public void Deassert(SignalLine line, int deviceId, Cycle cycle)
    {
        bool wasAsserted = IsAsserted(line);
        asserters[(int)line].Remove(deviceId);
        bool nowAsserted = IsAsserted(line);
        UpdateAnyInterruptPending();

        if (wasAsserted != nowAsserted)
        {
            SignalChanged?.Invoke(line, nowAsserted, deviceId, cycle);
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAsserted(SignalLine line)
        => asserters[(int)line].Count > 0;

    /// <inheritdoc />
    public bool ConsumeNmiEdge()
    {
        bool pending = nmiEdgePending;
        nmiEdgePending = false;
        UpdateAnyInterruptPending();
        return pending;
    }

    /// <summary>
    /// Resets all signal lines to their default (deasserted) state.
    /// </summary>
    /// <remarks>
    /// Called during system reset to ensure all signals start in a known state.
    /// </remarks>
    public void Reset()
    {
        foreach (var set in asserters)
        {
            set.Clear();
        }

        nmiEdgePending = false;
        anyInterruptPending = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateAnyInterruptPending()
        => anyInterruptPending = nmiEdgePending || asserters[(int)SignalLine.IRQ].Count > 0;
}