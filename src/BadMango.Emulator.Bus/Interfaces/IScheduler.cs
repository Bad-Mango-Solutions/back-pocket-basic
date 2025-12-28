// <copyright file="IScheduler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Central authority for discrete-event scheduling with a single authoritative cycle counter.
/// </summary>
/// <remarks>
/// <para>
/// The scheduler is a discrete-event arbiter. It does not tick, poll devices, run continuously,
/// or own time. It tracks the current cycle, knows which actors want to run at which cycle,
/// dispatches them in deterministic order, and allows actors to consume cycles explicitly.
/// </para>
/// <para>
/// Time is represented as a single <see cref="ulong"/> cycle counter that is monotonic,
/// never inferred, and never implicit. There is no "clock" driving execution—time advances
/// only as work is performed.
/// </para>
/// <para>
/// Events are one-shot, ordered by cycle, and deterministic. Same inputs yield the same
/// event order and the same behavior, making the system fully reproducible and debuggable.
/// </para>
/// </remarks>
public interface IScheduler
{
    /// <summary>
    /// Gets the current cycle in the timeline.
    /// </summary>
    /// <value>The current cycle, which only advances as actors consume cycles.</value>
    ulong CurrentCycle { get; }

    /// <summary>
    /// Schedules an actor to run at an absolute cycle.
    /// </summary>
    /// <param name="actor">The actor to schedule.</param>
    /// <param name="cycle">The absolute cycle at which the actor should run.</param>
    /// <remarks>
    /// If the requested cycle is in the past or at the current cycle, the actor
    /// will be run on the next <see cref="Drain"/> or <see cref="RunUntil"/> call.
    /// </remarks>
    void Schedule(ISchedulable actor, ulong cycle);

    /// <summary>
    /// Schedules an actor to run relative to the current cycle.
    /// </summary>
    /// <param name="actor">The actor to schedule.</param>
    /// <param name="deltaCycles">The number of cycles from now when the actor should run.</param>
    /// <remarks>
    /// This is a convenience method equivalent to <c>Schedule(actor, CurrentCycle + deltaCycles)</c>.
    /// </remarks>
    void ScheduleAfter(ISchedulable actor, ulong deltaCycles);

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
    /// </remarks>
    void Drain();

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
    /// </remarks>
    void RunUntil(ulong targetCycle);

    /// <summary>
    /// Cancels all scheduled events for the specified actor.
    /// </summary>
    /// <param name="actor">The actor whose events should be cancelled.</param>
    /// <returns><see langword="true"/> if any events were cancelled; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This allows actors to cancel their pending work, for example when
    /// a device is reset or a DMA transfer is aborted.
    /// </remarks>
    bool Cancel(ISchedulable actor);

    /// <summary>
    /// Advances the scheduler's current cycle by the specified number of cycles.
    /// </summary>
    /// <param name="cycles">The number of cycles to advance.</param>
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
    void AdvanceCycles(ulong cycles);
}