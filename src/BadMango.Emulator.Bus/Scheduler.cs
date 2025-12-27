// <copyright file="Scheduler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

/// <summary>
/// Cycle-accurate event scheduler implementation for discrete-event emulation.
/// </summary>
/// <remarks>
/// <para>
/// The scheduler is the single source of truth for timing. It maintains a priority queue
/// of events ordered by cycle (and sequence number as a tie-breaker), ensuring deterministic
/// event dispatch.
/// </para>
/// <para>
/// Time is represented as a single <see cref="ulong"/> cycle counter that is monotonic
/// and never decreases. There is no "clock" driving execution—time advances only as
/// work is performed by actors.
/// </para>
/// <para>
/// Events are one-shot, ordered by cycle, and deterministic. Same inputs yield the same
/// event order and the same behavior, making the system fully reproducible and debuggable.
/// </para>
/// <para>
/// The scheduler can be driven by CPU cycle signals from the <see cref="ISignalBus"/>,
/// allowing it to advance time based on actual CPU instruction execution.
/// </para>
/// </remarks>
public sealed class Scheduler : IScheduler
{
    /// <summary>
    /// Priority queue of scheduled events, ordered by cycle then sequence.
    /// </summary>
    private readonly PriorityQueue<ScheduledEvent, ScheduledEvent> eventQueue;

    /// <summary>
    /// Sequence number for deterministic tie-breaking when events share the same cycle.
    /// </summary>
    private long nextSequence;

    /// <summary>
    /// Backing field for <see cref="CurrentCycle"/>.
    /// </summary>
    private ulong currentCycle;

    /// <summary>
    /// Initializes a new instance of the <see cref="Scheduler"/> class.
    /// </summary>
    public Scheduler()
    {
        eventQueue = new PriorityQueue<ScheduledEvent, ScheduledEvent>(
            Comparer<ScheduledEvent>.Create(static (a, b) => a.CompareTo(b)));
    }

    /// <summary>
    /// Gets the current cycle in the timeline.
    /// </summary>
    /// <value>The current cycle, which only advances as actors consume cycles.</value>
    public ulong CurrentCycle
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => currentCycle;
        private set => currentCycle = value;
    }

    /// <summary>
    /// Gets the number of pending events in the scheduler.
    /// </summary>
    /// <value>The count of events waiting to be dispatched.</value>
    public int PendingEventCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => eventQueue.Count;
    }

    /// <summary>
    /// Schedules an actor to run at an absolute cycle.
    /// </summary>
    /// <param name="actor">The actor to schedule.</param>
    /// <param name="cycle">The absolute cycle at which the actor should run.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actor"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// If the requested cycle is in the past or at the current cycle, the actor
    /// will be run on the next <see cref="Drain"/> or <see cref="RunUntil"/> call.
    /// </para>
    /// <para>
    /// Events scheduled for the same cycle are ordered by their sequence number,
    /// which is assigned at scheduling time, ensuring deterministic dispatch order.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Schedule(ISchedulable actor, ulong cycle)
    {
        ArgumentNullException.ThrowIfNull(actor, nameof(actor));

        var scheduledEvent = new ScheduledEvent(cycle, nextSequence++, actor);
        eventQueue.Enqueue(scheduledEvent, scheduledEvent);
    }

    /// <summary>
    /// Schedules an actor to run relative to the current cycle.
    /// </summary>
    /// <param name="actor">The actor to schedule.</param>
    /// <param name="deltaCycles">The number of cycles from now when the actor should run.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actor"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This is a convenience method equivalent to <c>Schedule(actor, CurrentCycle + deltaCycles)</c>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ScheduleAfter(ISchedulable actor, ulong deltaCycles)
    {
        ArgumentNullException.ThrowIfNull(actor, nameof(actor));

        Schedule(actor, currentCycle + deltaCycles);
    }

    /// <summary>
    /// Runs until no events remain at or before the current cycle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This drains all due work without advancing the cycle beyond what actors consume.
    /// Use this when you want to process all pending work at the current time.
    /// </para>
    /// <para>
    /// The scheduler dispatches events in deterministic order. When multiple events
    /// share the same cycle, they are ordered by a sequence number for reproducibility.
    /// </para>
    /// <para>
    /// Actors may schedule new events during dispatch, including events at the current
    /// cycle. These will be processed in the same drain pass, respecting the sequence
    /// order.
    /// </para>
    /// </remarks>
    public void Drain()
    {
        while (eventQueue.TryPeek(out var nextEvent, out _) && nextEvent.Cycle <= currentCycle)
        {
            eventQueue.Dequeue();
            ulong consumedCycles = nextEvent.Actor.Execute(currentCycle, this);

            // Time advances based on consumed cycles (but CurrentCycle can never decrease)
            if (consumedCycles > 0)
            {
                currentCycle += consumedCycles;
            }
        }
    }

    /// <summary>
    /// Advances execution until the given cycle is reached.
    /// </summary>
    /// <param name="targetCycle">The target cycle to advance to.</param>
    /// <remarks>
    /// <para>
    /// This is the main execution method. It runs all scheduled actors until
    /// the current cycle reaches or exceeds the target cycle.
    /// </para>
    /// <para>
    /// The cycle counter advances as actors return their consumed cycles.
    /// Host throttling (e.g., syncing to real time) lives outside the machine,
    /// not in this method.
    /// </para>
    /// <para>
    /// If there are no pending events and the target cycle has not been reached,
    /// the current cycle is advanced directly to the target.
    /// </para>
    /// </remarks>
    public void RunUntil(ulong targetCycle)
    {
        // Process all events due at or before the target cycle
        while (eventQueue.TryPeek(out var nextEvent, out _) && nextEvent.Cycle <= targetCycle)
        {
            eventQueue.Dequeue();

            // Advance current cycle to the event's due time if it's in the future
            if (nextEvent.Cycle > currentCycle)
            {
                currentCycle = nextEvent.Cycle;
            }

            ulong consumedCycles = nextEvent.Actor.Execute(currentCycle, this);

            // Time advances based on consumed cycles
            if (consumedCycles > 0)
            {
                currentCycle += consumedCycles;
            }
        }

        // Ensure we reach the target cycle even if no events or all events were earlier
        if (currentCycle < targetCycle)
        {
            currentCycle = targetCycle;
        }
    }

    /// <summary>
    /// Advances the scheduler's current cycle by the specified number of cycles.
    /// </summary>
    /// <param name="cycles">The number of cycles to advance.</param>
    /// <remarks>
    /// <para>
    /// This method is used to advance the scheduler's time based on CPU cycle signals
    /// from the <see cref="ISignalBus"/>. It processes any events that become due
    /// as a result of the time advancement.
    /// </para>
    /// <para>
    /// This is the preferred method for CPU-driven scheduling, where the CPU
    /// signals instruction execution and the scheduler advances accordingly.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceCycles(ulong cycles)
    {
        if (cycles == 0)
        {
            return;
        }

        ulong targetCycle = currentCycle + cycles;

        // Process any events that are now due
        while (eventQueue.TryPeek(out var nextEvent, out _) && nextEvent.Cycle <= targetCycle)
        {
            eventQueue.Dequeue();

            // Advance to the event's cycle
            if (nextEvent.Cycle > currentCycle)
            {
                currentCycle = nextEvent.Cycle;
            }

            ulong consumedCycles = nextEvent.Actor.Execute(currentCycle, this);

            if (consumedCycles > 0)
            {
                currentCycle += consumedCycles;
            }
        }

        // Advance to the target cycle
        currentCycle = targetCycle;
    }

    /// <summary>
    /// Cancels all scheduled events for the specified actor.
    /// </summary>
    /// <param name="actor">The actor whose events should be cancelled.</param>
    /// <returns><see langword="true"/> if any events were cancelled; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actor"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This allows actors to cancel their pending work, for example when
    /// a device is reset or a DMA transfer is aborted.
    /// </para>
    /// <para>
    /// Since <see cref="PriorityQueue{TElement, TPriority}"/> does not support efficient
    /// removal by element, this method rebuilds the queue excluding events for the
    /// specified actor. For better performance with frequent cancellations, consider
    /// using a more sophisticated data structure.
    /// </para>
    /// </remarks>
    public bool Cancel(ISchedulable actor)
    {
        ArgumentNullException.ThrowIfNull(actor, nameof(actor));

        // Since PriorityQueue doesn't support efficient removal, we need to rebuild
        // the queue without the cancelled actor's events
        var remainingEvents = new List<ScheduledEvent>();
        bool cancelled = false;

        while (eventQueue.TryDequeue(out var evt, out _))
        {
            if (ReferenceEquals(evt.Actor, actor))
            {
                cancelled = true;
            }
            else
            {
                remainingEvents.Add(evt);
            }
        }

        // Re-add the remaining events
        foreach (var evt in remainingEvents)
        {
            eventQueue.Enqueue(evt, evt);
        }

        return cancelled;
    }

    /// <summary>
    /// Resets the scheduler to cycle 0 and cancels all pending events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called during machine reset to return the scheduler to its initial state.
    /// All pending events are discarded and the cycle counter is reset to zero.
    /// </para>
    /// <para>
    /// The sequence counter is also reset to ensure deterministic behavior
    /// after reset.
    /// </para>
    /// </remarks>
    public void Reset()
    {
        eventQueue.Clear();
        currentCycle = 0;
        nextSequence = 0;
    }
}