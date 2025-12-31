// <copyright file="Scheduler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using Core;

using Interfaces;

/// <summary>
/// Cycle-accurate event scheduler implementation for discrete-event emulation.
/// </summary>
/// <remarks>
/// <para>
/// The scheduler is the single source of truth for timing. It maintains a priority queue
/// of events ordered by cycle, priority, and sequence number, ensuring deterministic
/// event dispatch.
/// </para>
/// <para>
/// Time is represented as a single <see cref="ulong"/> cycle counter that is monotonic
/// and never decreases. There is no "clock" driving execution—time advances only as
/// explicitly requested.
/// </para>
/// <para>
/// Events are one-shot, ordered by cycle and priority, and deterministic. Same inputs yield the same
/// event order and the same behavior, making the system fully reproducible and debuggable.
/// </para>
/// </remarks>
public sealed class Scheduler : IScheduler
{
    /// <summary>
    /// Priority queue of scheduled events, ordered by cycle, priority, then sequence.
    /// </summary>
    private readonly PriorityQueue<ScheduledEvent, ScheduledEvent> eventQueue;

    /// <summary>
    /// Set of cancelled event handles for efficient cancellation checking.
    /// </summary>
    private readonly HashSet<ulong> cancelledHandles;

    /// <summary>
    /// The event context used when dispatching callbacks.
    /// </summary>
    private IEventContext? eventContext;

    /// <summary>
    /// Sequence number for deterministic tie-breaking when events share the same cycle and priority.
    /// </summary>
    private long nextSequence;

    /// <summary>
    /// The next handle ID to assign.
    /// </summary>
    private ulong nextHandleId;

    /// <summary>
    /// Backing field for <see cref="Now"/>.
    /// </summary>
    private Cycle now;

    /// <summary>
    /// Initializes a new instance of the <see cref="Scheduler"/> class.
    /// </summary>
    public Scheduler()
    {
        eventQueue = new PriorityQueue<ScheduledEvent, ScheduledEvent>(
            Comparer<ScheduledEvent>.Create(static (a, b) => a.CompareTo(b)));
        cancelledHandles = new();
    }

    /// <inheritdoc />
    public Cycle Now
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => now;
    }

    /// <inheritdoc />
    public int PendingEventCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => eventQueue.Count;
    }

    /// <summary>
    /// Sets the event context used when dispatching callbacks.
    /// </summary>
    /// <param name="context">The event context to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This must be called before any events are dispatched. The event context is
    /// typically created after all system components are wired up.
    /// </remarks>
    public void SetEventContext(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        eventContext = context;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandle ScheduleAt(Cycle due, ScheduledEventKind kind, int priority, Action<IEventContext> callback, object? tag = null)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        var handle = new EventHandle(nextHandleId++);
        var scheduledEvent = new ScheduledEvent(handle, due, priority, nextSequence++, kind, callback, tag);
        eventQueue.Enqueue(scheduledEvent, scheduledEvent);

        return handle;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventHandle ScheduleAfter(Cycle delta, ScheduledEventKind kind, int priority, Action<IEventContext> callback, object? tag = null)
    {
        return ScheduleAt(now + delta, kind, priority, callback, tag);
    }

    /// <inheritdoc />
    public bool Cancel(EventHandle handle)
    {
        // Mark the handle as cancelled - it will be skipped during dispatch
        return cancelledHandles.Add(handle.Id);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(Cycle delta)
    {
        if (delta == 0)
        {
            return;
        }

        Cycle targetCycle = now + delta;

        // Process any events that are now due
        DispatchEventsUntil(targetCycle);

        // Advance to the target cycle
        now = targetCycle;
    }

    /// <inheritdoc />
    public void DispatchDue()
    {
        DispatchEventsUntil(now);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cycle? PeekNextDue()
    {
        // Skip cancelled events to find the next valid one
        while (eventQueue.TryPeek(out var nextEvent, out _))
        {
            if (!cancelledHandles.Contains(nextEvent.Handle.Id))
            {
                return nextEvent.Cycle;
            }

            // Remove cancelled event
            eventQueue.Dequeue();
            cancelledHandles.Remove(nextEvent.Handle.Id);
        }

        return null;
    }

    /// <inheritdoc />
    public bool JumpToNextEventAndDispatch()
    {
        var nextDue = PeekNextDue();
        if (nextDue is null)
        {
            return false;
        }

        // Advance to the event's cycle
        if (nextDue.Value > now)
        {
            now = nextDue.Value;
        }

        // Dispatch all events due at this cycle
        DispatchEventsUntil(now);

        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        eventQueue.Clear();
        cancelledHandles.Clear();
        now = 0;
        nextSequence = 0;
        nextHandleId = 0;
    }

    /// <summary>
    /// Dispatches all events due at or before the specified cycle.
    /// </summary>
    /// <param name="targetCycle">The target cycle up to which events should be dispatched.</param>
    private void DispatchEventsUntil(Cycle targetCycle)
    {
        while (eventQueue.TryPeek(out var nextEvent, out _) && nextEvent.Cycle <= targetCycle)
        {
            eventQueue.Dequeue();

            // Skip cancelled events
            if (cancelledHandles.Remove(nextEvent.Handle.Id))
            {
                continue;
            }

            // Advance to the event's cycle if it's in the future
            if (nextEvent.Cycle > now)
            {
                now = nextEvent.Cycle;
            }

            // Invoke the callback with the event context
            if (eventContext is null)
            {
                throw new InvalidOperationException("Event context is not set. Call SetEventContext before dispatching events.");
            }

            nextEvent.Callback(eventContext);
        }

        // Clean up the cancelled handles set periodically to prevent unbounded growth
        // Clear when we have a significant number of cancelled handles and either:
        // 1. The queue is empty, or
        // 2. The number of cancelled handles exceeds a threshold (1000)
        if (cancelledHandles.Count > 0 && (eventQueue.Count == 0 || cancelledHandles.Count > 1000))
        {
            cancelledHandles.Clear();
        }
    }
}