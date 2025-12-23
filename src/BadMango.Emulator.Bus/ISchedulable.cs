// <copyright file="ISchedulable.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents an actor that can be scheduled for execution in the discrete-event scheduler.
/// </summary>
/// <remarks>
/// <para>
/// Anything that can "do work" in time implements this interface:
/// CPU, DMA engine, video beam, disk controller, timers, serial ports, etc.
/// </para>
/// <para>
/// Actors never run themselves. They are invoked by the scheduler at their scheduled cycle.
/// The actor is told what time it is and returns how much time it consumed.
/// Actors may schedule future events during execution.
/// </para>
/// </remarks>
public interface ISchedulable
{
    /// <summary>
    /// Called by the scheduler when this actor is due to run.
    /// </summary>
    /// <param name="currentCycle">The current cycle when execution begins.</param>
    /// <param name="scheduler">The scheduler, allowing the actor to schedule future events.</param>
    /// <returns>The number of cycles consumed by this execution.</returns>
    /// <remarks>
    /// <para>
    /// The actor is told what time it is and returns how much time it consumed.
    /// The scheduler advances time based on the return value.
    /// </para>
    /// <para>
    /// An actor may return 0 if its work is "free" in terms of cycle consumption
    /// (e.g., a beam advance that doesn't steal bus cycles).
    /// </para>
    /// </remarks>
    ulong Execute(ulong currentCycle, IScheduler scheduler);
}