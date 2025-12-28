// <copyright file="SignalBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

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
    public event Action<SignalLine, bool, int, ulong>? SignalChanged;

    /// <inheritdoc />
    public void Assert(SignalLine line, int deviceId)
    {
        bool wasAsserted = IsAsserted(line);
        asserters[(int)line].Add(deviceId);
        bool nowAsserted = IsAsserted(line);

        // NMI edge detection (low-to-high transition)
        if (line == SignalLine.NMI && !wasAsserted && nowAsserted)
        {
            nmiEdgePending = true;
        }

        if (wasAsserted != nowAsserted)
        {
            SignalChanged?.Invoke(line, nowAsserted, deviceId, 0);
        }
    }

    /// <inheritdoc />
    public void Deassert(SignalLine line, int deviceId)
    {
        bool wasAsserted = IsAsserted(line);
        asserters[(int)line].Remove(deviceId);
        bool nowAsserted = IsAsserted(line);

        if (wasAsserted != nowAsserted)
        {
            SignalChanged?.Invoke(line, nowAsserted, deviceId, 0);
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
    }
}