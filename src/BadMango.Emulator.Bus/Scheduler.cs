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
public sealed class Scheduler : IScheduler, ISchedulerObserver
{
    /// <summary>
    /// Maximum number of cancelled handles to accumulate before forcing cleanup.
    /// </summary>
    private const int CancelledHandlesCleanupThreshold = 1000;

    /// <summary>
    /// Priority queue of scheduled events, ordered by cycle, priority, then sequence.
    /// Using value tuple key for reliable default ordering (avoids custom comparer heap issues).
    /// </summary>
    private readonly PriorityQueue<ScheduledEvent, (ulong Cycle, int Priority, long Sequence)> eventQueue;

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
    /// Reentrancy guard for Dispatch to protect PriorityQueue from nested modifications during
    /// callbacks (e.g. VBlank scheduling more events or device code advancing time).
    /// </summary>
    private bool isDispatching;

    /// <summary>
    /// Initializes a new instance of the <see cref="Scheduler"/> class.
    /// </summary>
    public Scheduler()
    {
        // Default comparer for tuple (cycle, pri, seq) gives correct min-heap order (earliest first)
        eventQueue = new();
        cancelledHandles = new();
    }

    /// <inheritdoc cref="ISchedulerObserver.EventScheduled" />
    public event Action<EventHandle, Cycle, ScheduledEventKind, int, object?>? EventScheduled;

    /// <inheritdoc cref="ISchedulerObserver.EventConsumed" />
    public event Action<EventHandle, Cycle, ScheduledEventKind>? EventConsumed;

    /// <inheritdoc cref="ISchedulerObserver.EventCancelled" />
    public event Action<EventHandle>? EventCancelled;

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
        eventQueue.Enqueue(scheduledEvent, (due, priority, scheduledEvent.Sequence));

        EventScheduled?.Invoke(handle, due, kind, priority, tag);

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
        var cancelled = cancelledHandles.Add(handle.Id);
        if (cancelled)
        {
            EventCancelled?.Invoke(handle);
        }

        return cancelled;
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
            if (!eventQueue.TryDequeue(out _, out _))
            {
                break; // safety
            }
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

        // Do NOT reset nextHandleId or nextSequence. Handle IDs and sequence
        // numbers must remain monotonically increasing across resets to prevent
        // stale cancellation collisions. If a device cancels an old handle after
        // a reset and the handle ID counter were restarted at 0, newly scheduled
        // events could receive the same ID as the cancelled handle, causing them
        // to be incorrectly skipped during dispatch.
    }

    /// <summary>
    /// Dispatches all events due at or before the specified cycle.
    /// </summary>
    /// <param name="targetCycle">The target cycle up to which events should be dispatched.</param>
    private void DispatchEventsUntil(Cycle targetCycle)
    {
        // Safer dispatch: drain the PQ into a list (no callbacks during queue mutation), invoke
        // callbacks with now set to each event's cycle, then repeat so events scheduled during
        // callbacks are still eligible in this Advance pass. This prevents reentrancy corruption
        // of the PriorityQueue while preserving periodic timers and VBlank rescheduling.
        if (isDispatching)
        {
            return;
        }

        isDispatching = true;
        try
        {
            var toDispatch = new System.Collections.Generic.List<ScheduledEvent>();
            int safety = 0;
            const int MaxEventsPerDispatch = 100000;

            while (true)
            {
                toDispatch.Clear();

                while (safety++ < MaxEventsPerDispatch && eventQueue.TryPeek(out var peeked, out _) && peeked.Cycle <= targetCycle)
                {
                    ScheduledEvent nextEvent;
                    try
                    {
                        if (!eventQueue.TryDequeue(out nextEvent, out _))
                        {
                            break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.Error.WriteLine($"[SCHED PQ] TryDequeue failed (possible transient corruption from high event volume): {ex.Message}");
                        break;
                    }

                    if (cancelledHandles.Remove(nextEvent.Handle.Id))
                    {
                        continue;
                    }

                    toDispatch.Add(nextEvent);
                }

                if (toDispatch.Count == 0)
                {
                    break;
                }

                foreach (var ev in toDispatch)
                {
                    if (eventContext is null)
                    {
                        throw new InvalidOperationException("Event context is not set. Call SetEventContext before dispatching events.");
                    }

                    if (ev.Cycle > now)
                    {
                        now = ev.Cycle;
                    }

                    try
                    {
                        ev.Callback(eventContext);
                    }
                    catch (Exception ex)
                    {
                        System.Console.Error.WriteLine($"[SCHED CALLBACK ERR] Kind={ev.Kind} method={ev.Callback?.Method?.Name} ex={ex.Message}");
                    }

                    EventConsumed?.Invoke(ev.Handle, now, ev.Kind);
                }
            }

            if (safety >= MaxEventsPerDispatch)
            {
                System.Console.Error.WriteLine("[SCHED] safety limit hit in dispatch - possible event loop or drift");
            }
        }
        finally
        {
            isDispatching = false;
        }

        if (cancelledHandles.Count > 0 && (eventQueue.Count == 0 || cancelledHandles.Count > CancelledHandlesCleanupThreshold))
        {
            cancelledHandles.Clear();
        }
    }
}