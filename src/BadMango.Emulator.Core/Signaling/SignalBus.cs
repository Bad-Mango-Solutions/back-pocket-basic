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
/// <para>
/// RESET is level-sensitive while held and edge-triggered on release: the deasserted transition
/// latches a release edge that the CPU consumes (<see cref="ConsumeResetReleaseEdge"/>) to run its
/// reset sequence, even if the line was asserted and released between two instruction boundaries.
/// </para>
/// <para>
/// <b>Threading contract.</b> The signal bus is single-threaded: it is owned by the thread that runs
/// the machine (the run loop while the machine is running, or the caller while it is quiescent).
/// <see cref="Assert"/>, <see cref="Deassert"/>, <see cref="ConsumeNmiEdge"/>,
/// <see cref="ConsumeResetReleaseEdge"/>, <see cref="Reset"/> and <see cref="ServiceRequests"/> must
/// only be called from that thread, and <see cref="SignalChanged"/> is raised on it. Host threads
/// (UI, REPL) must marshal line changes onto the machine thread, for example through the machine's
/// request queue. The only members that are safe to use from any thread are
/// <see cref="RequestService"/> and the <see cref="AttentionPending"/> read.
/// </para>
/// </remarks>
public sealed class SignalBus : ISignalBus
{
    /// <summary>
    /// <see cref="attention"/> bit set while an interrupt, RESET hold or RESET release edge is pending.
    /// Only written on the machine thread.
    /// </summary>
    private const int SignalAttentionBit = 1;

    /// <summary>
    /// <see cref="attention"/> bit set when a service request has been posted (from any thread).
    /// </summary>
    private const int ServiceAttentionBit = 2;

    private readonly HashSet<int>[] asserters;
    private bool nmiEdgePending;
    private bool resetReleaseEdgePending;
    private Action? serviceHandler;

    /// <summary>
    /// Cached attention word read by the CPU once per instruction. Bit
    /// <see cref="SignalAttentionBit"/> is <c>nmiEdgePending || IRQ asserted || RESET asserted ||
    /// resetReleaseEdgePending</c>, maintained on every state change; bit <see cref="ServiceAttentionBit"/>
    /// is set by <see cref="RequestService"/>. Both bits are updated atomically so a service request
    /// posted from another thread is never lost when the machine thread updates the signal bit.
    /// </summary>
    private int attention;

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
        get => nmiEdgePending || asserters[(int)SignalLine.IRQ].Count > 0;
    }

    /// <summary>
    /// Gets a value indicating whether the CPU has anything to service at the next instruction
    /// boundary: a pending interrupt (as <see cref="AnyInterruptPending"/>), an asserted RESET line,
    /// an unconsumed RESET release edge, or a pending service request.
    /// </summary>
    /// <remarks>
    /// This is the CPU's single per-instruction fast-path check. When it is <see langword="false"/>
    /// the CPU skips interrupt, RESET and service-request handling entirely. It is safe to read from
    /// any thread.
    /// </remarks>
    public bool AttentionPending
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref attention) != 0;
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

        UpdateSignalAttention();

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

        // RESET release edge (asserted-to-deasserted transition). Latched before
        // SignalChanged is raised so listeners see a consistent state.
        if (line == SignalLine.Reset && wasAsserted && !nowAsserted)
        {
            resetReleaseEdgePending = true;
        }

        UpdateSignalAttention();

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
        UpdateSignalAttention();
        return pending;
    }

    /// <summary>
    /// Consumes a pending RESET release edge.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the RESET line was released since the edge was last consumed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// The CPU calls this at an instruction boundary to decide whether to run its reset
    /// sequence. A power-on CPU reset also consumes the edge, because it replaces the sequence.
    /// </remarks>
    public bool ConsumeResetReleaseEdge()
    {
        bool pending = resetReleaseEdgePending;
        if (pending)
        {
            resetReleaseEdgePending = false;
            UpdateSignalAttention();
        }

        return pending;
    }

    /// <summary>
    /// Sets the handler that <see cref="ServiceRequests"/> invokes when a service request is pending.
    /// </summary>
    /// <param name="handler">The handler, or <see langword="null"/> to remove it.</param>
    /// <remarks>
    /// The machine registers a handler that drains its cross-thread request queue, so requests
    /// posted while the machine is running execute on the run-loop thread at an instruction boundary.
    /// </remarks>
    public void SetServiceHandler(Action? handler)
    {
        serviceHandler = handler;
    }

    /// <summary>
    /// Requests that the CPU call <see cref="ServiceRequests"/> at its next instruction boundary.
    /// </summary>
    /// <remarks>
    /// Safe to call from any thread. This only raises the attention flag; the work itself is
    /// queued by the caller (normally the machine) and run by the registered service handler.
    /// </remarks>
    public void RequestService()
    {
        Interlocked.Or(ref attention, ServiceAttentionBit);
    }

    /// <summary>
    /// Runs the registered service handler if a service request is pending.
    /// </summary>
    /// <remarks>
    /// Called by the CPU on the machine thread at an instruction boundary. The request flag is
    /// cleared before the handler runs, so a request posted while the handler executes is
    /// serviced at the next boundary rather than lost.
    /// </remarks>
    public void ServiceRequests()
    {
        if ((Volatile.Read(ref attention) & ServiceAttentionBit) == 0)
        {
            return;
        }

        Interlocked.And(ref attention, ~ServiceAttentionBit);
        serviceHandler?.Invoke();
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
        resetReleaseEdgePending = false;
        UpdateSignalAttention();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateSignalAttention()
    {
        bool pending = nmiEdgePending
            || resetReleaseEdgePending
            || asserters[(int)SignalLine.IRQ].Count > 0
            || asserters[(int)SignalLine.Reset].Count > 0;
        bool current = (Volatile.Read(ref attention) & SignalAttentionBit) != 0;
        if (pending == current)
        {
            return;
        }

        if (pending)
        {
            Interlocked.Or(ref attention, SignalAttentionBit);
        }
        else
        {
            Interlocked.And(ref attention, ~SignalAttentionBit);
        }
    }
}