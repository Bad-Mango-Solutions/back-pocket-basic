// <copyright file="IScheduler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

using Core;

/// <summary>
/// Central authority for discrete-event scheduling with a single authoritative cycle counter.
/// </summary>
/// <remarks>
/// <para>
/// The scheduler is a discrete-event arbiter. It does not tick, poll devices, run continuously,
/// or own time. It tracks the current cycle, knows which callbacks want to run at which cycle,
/// dispatches them in deterministic order, and advances time explicitly.
/// </para>
/// <para>
/// Time is represented as a single <see cref="ulong"/> cycle counter that is monotonic,
/// never inferred, and never implicit. There is no "clock" driving execution—time advances
/// only as work is performed.
/// </para>
/// <para>
/// Events are one-shot, ordered by cycle and priority, and deterministic. Same inputs yield the same
/// event order and the same behavior, making the system fully reproducible and debuggable.
/// </para>
/// </remarks>
public interface IScheduler
{
    /// <summary>
    /// Gets the current cycle in the timeline.
    /// </summary>
    /// <value>The current cycle, which only advances as time is explicitly advanced.</value>
    Cycle Now { get; }

    /// <summary>
    /// Gets the number of pending events (for diagnostics).
    /// </summary>
    /// <value>The count of events waiting to be dispatched.</value>
    int PendingEventCount { get; }

    /// <summary>
    /// Schedules an event at an absolute cycle.
    /// </summary>
    /// <param name="due">The absolute cycle at which the event should fire.</param>
    /// <param name="kind">The kind of scheduled event for categorization.</param>
    /// <param name="priority">The priority for tie-breaking when events share the same cycle (lower values run first).</param>
    /// <param name="callback">The callback to invoke when the event fires.</param>
    /// <param name="tag">An optional tag for identifying the event source.</param>
    /// <returns>A handle that can be used to cancel the event.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// If the requested cycle is in the past or at the current cycle, the event
    /// will fire on the next <see cref="DispatchDue"/> or <see cref="Advance"/> call.
    /// </para>
    /// <para>
    /// Events scheduled for the same cycle are ordered by priority (lower values first),
    /// then by sequence number for deterministic dispatch order.
    /// </para>
    /// </remarks>
    EventHandle ScheduleAt(Cycle due, ScheduledEventKind kind, int priority, Action<IEventContext> callback, object? tag = null);

    /// <summary>
    /// Schedules an event relative to the current cycle.
    /// </summary>
    /// <param name="delta">The number of cycles from now when the event should fire.</param>
    /// <param name="kind">The kind of scheduled event for categorization.</param>
    /// <param name="priority">The priority for tie-breaking when events share the same cycle (lower values run first).</param>
    /// <param name="callback">The callback to invoke when the event fires.</param>
    /// <param name="tag">An optional tag for identifying the event source.</param>
    /// <returns>A handle that can be used to cancel the event.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This is a convenience method equivalent to <c>ScheduleAt(Now + delta, kind, priority, callback, tag)</c>.
    /// </remarks>
    EventHandle ScheduleAfter(Cycle delta, ScheduledEventKind kind, int priority, Action<IEventContext> callback, object? tag = null);

    /// <summary>
    /// Cancels a pending event.
    /// </summary>
    /// <param name="handle">The handle of the event to cancel.</param>
    /// <returns><see langword="true"/> if the event was cancelled; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This allows callers to cancel their pending work, for example when
    /// a device is reset or a DMA transfer is aborted.
    /// </remarks>
    bool Cancel(EventHandle handle);

    /// <summary>
    /// Advances the scheduler's current cycle by the specified number of cycles, dispatching due events.
    /// </summary>
    /// <param name="delta">The number of cycles to advance.</param>
    /// <remarks>
    /// <para>
    /// This method is used to advance the scheduler's time based on CPU cycle signals.
    /// It processes any events that become due as a result of the time advancement.
    /// </para>
    /// <para>
    /// This is the preferred method for CPU-driven scheduling, where the CPU
    /// signals instruction execution and the scheduler advances accordingly.
    /// </para>
    /// </remarks>
    void Advance(Cycle delta);

    /// <summary>
    /// Dispatches all events due at the current cycle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This dispatches all due work without advancing the cycle.
    /// Use this when you want to process all pending work at the current time.
    /// </para>
    /// <para>
    /// The scheduler dispatches events in deterministic order. When multiple events
    /// share the same cycle, they are ordered by priority then by sequence number
    /// for reproducibility.
    /// </para>
    /// </remarks>
    void DispatchDue();

    /// <summary>
    /// Gets the next event time (for WAI fast-forward).
    /// </summary>
    /// <returns>The cycle of the next scheduled event, or <see langword="null"/> if no events are pending.</returns>
    /// <remarks>
    /// This is useful for implementing WAI (Wait for Interrupt) fast-forward,
    /// where the CPU wants to skip ahead to the next event without executing NOPs.
    /// </remarks>
    Cycle? PeekNextDue();

    /// <summary>
    /// Jumps to the next event and dispatches it (WAI support).
    /// </summary>
    /// <returns><see langword="true"/> if an event was dispatched; <see langword="false"/> if no events are pending.</returns>
    /// <remarks>
    /// <para>
    /// This is useful for implementing WAI (Wait for Interrupt) fast-forward,
    /// where the CPU wants to skip ahead to the next event without executing NOPs.
    /// </para>
    /// <para>
    /// If an event is pending, the scheduler advances to that event's cycle and dispatches it.
    /// All events due at that cycle will be dispatched.
    /// </para>
    /// </remarks>
    bool JumpToNextEventAndDispatch();

    /// <summary>
    /// Resets the scheduler to cycle 0 and cancels all pending events.
    /// </summary>
    /// <remarks>
    /// Called during machine reset to return the scheduler to its initial state.
    /// All pending events are discarded and the cycle counter is reset to zero.
    /// </remarks>
    void Reset();
}