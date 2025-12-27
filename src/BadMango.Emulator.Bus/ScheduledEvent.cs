// <copyright file="ScheduledEvent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

/// <summary>
/// Represents a scheduled event in the discrete-event scheduler.
/// </summary>
/// <remarks>
/// <para>
/// An event is: "At cycle N, actor X wants to run."
/// Events are one-shot, ordered by cycle, and deterministic.
/// </para>
/// <para>
/// The <see cref="Sequence"/> property ensures stable ordering when multiple events
/// share the same cycle. This is critical for reproducibility and debugging.
/// </para>
/// </remarks>
/// <param name="Cycle">The absolute cycle at which this event is scheduled.</param>
/// <param name="Sequence">A tie-breaker for deterministic ordering when events share a cycle.</param>
/// <param name="Actor">The actor to execute when this event fires.</param>
public readonly record struct ScheduledEvent(
    ulong Cycle,
    long Sequence,
    ISchedulable Actor) : IComparable<ScheduledEvent>
{
    /// <summary>
    /// Compares this event to another for ordering in the event queue.
    /// </summary>
    /// <param name="other">The other event to compare to.</param>
    /// <returns>A negative value if this event comes before <paramref name="other"/>,
    /// zero if they are equal, or a positive value if this event comes after.</returns>
    /// <remarks>
    /// Events are ordered first by cycle, then by sequence number.
    /// This ensures deterministic ordering even when events share the same cycle.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(ScheduledEvent other)
    {
        int cycleCompare = Cycle.CompareTo(other.Cycle);
        return cycleCompare != 0 ? cycleCompare : Sequence.CompareTo(other.Sequence);
    }
}