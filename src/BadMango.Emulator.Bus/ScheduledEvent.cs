// <copyright file="ScheduledEvent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using Interfaces;

/// <summary>
/// Represents a scheduled event in the discrete-event scheduler.
/// </summary>
/// <remarks>
/// <para>
/// An event is: "At cycle N, invoke callback X."
/// Events are one-shot, ordered by cycle and priority, and deterministic.
/// </para>
/// <para>
/// The <see cref="Sequence"/> property ensures stable ordering when multiple events
/// share the same cycle and priority. This is critical for reproducibility and debugging.
/// </para>
/// </remarks>
/// <param name="Handle">The unique handle for this event, used for cancellation.</param>
/// <param name="Cycle">The absolute cycle at which this event is scheduled.</param>
/// <param name="Priority">The priority for tie-breaking (lower values run first).</param>
/// <param name="Sequence">A tie-breaker for deterministic ordering when events share cycle and priority.</param>
/// <param name="Kind">The kind of scheduled event for categorization.</param>
/// <param name="Callback">The callback to invoke when this event fires.</param>
/// <param name="Tag">An optional tag for identifying the event source.</param>
public readonly record struct ScheduledEvent(
    EventHandle Handle,
    ulong Cycle,
    int Priority,
    long Sequence,
    ScheduledEventKind Kind,
    Action<IEventContext> Callback,
    object? Tag) : IComparable<ScheduledEvent>
{
    /// <summary>
    /// Compares this event to another for ordering in the event queue.
    /// </summary>
    /// <param name="other">The other event to compare to.</param>
    /// <returns>A negative value if this event comes before <paramref name="other"/>,
    /// zero if they are equal, or a positive value if this event comes after.</returns>
    /// <remarks>
    /// Events are ordered first by cycle, then by priority (lower first), then by sequence number.
    /// This ensures deterministic ordering even when events share the same cycle.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(ScheduledEvent other)
    {
        int cycleCompare = Cycle.CompareTo(other.Cycle);
        if (cycleCompare != 0)
        {
            return cycleCompare;
        }

        int priorityCompare = Priority.CompareTo(other.Priority);
        return priorityCompare != 0 ? priorityCompare : Sequence.CompareTo(other.Sequence);
    }
}