// <copyright file="IEventContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Provides context for scheduled device event callbacks, giving access to system services.
/// </summary>
/// <remarks>
/// <para>
/// The event context bundles references to core system services that devices need
/// during initialization and when processing scheduled events. This pattern allows
/// devices to interact with the scheduler, signal bus, and memory bus without
/// requiring direct constructor injection of all dependencies.
/// </para>
/// <para>
/// Devices are expected to cache this context (and its underlying services)
/// for use across their lifecycle, including multiple scheduled callbacks.
/// However, the owning system may recreate or replace these services during a
/// full system reset or reinitialization, at which point devices must obtain
/// a fresh context instead of reusing a previously cached instance.
/// </para>
/// </remarks>
public interface IEventContext
{
    /// <summary>
    /// Gets the current cycle from the scheduler.
    /// </summary>
    /// <value>The current cycle in the timeline.</value>
    /// <remarks>
    /// This is a convenience property equivalent to <c>Scheduler.CurrentCycle</c>.
    /// </remarks>
    ulong CurrentCycle { get; }

    /// <summary>
    /// Gets the cycle-accurate event scheduler.
    /// </summary>
    /// <value>The scheduler for scheduling future events.</value>
    /// <remarks>
    /// Devices use this to schedule timer events, periodic callbacks, and
    /// deferred work. The scheduler is the single source of truth for timing.
    /// </remarks>
    IScheduler Scheduler { get; }

    /// <summary>
    /// Gets the signal bus for interrupt and control line management.
    /// </summary>
    /// <value>The signal bus for asserting/deasserting interrupt lines.</value>
    /// <remarks>
    /// Devices use this to assert and clear interrupt lines (IRQ, NMI, etc.)
    /// and to query the current state of control signals.
    /// </remarks>
    ISignalBus Signals { get; }

    /// <summary>
    /// Gets the main memory bus for memory operations.
    /// </summary>
    /// <value>The memory bus for reading and writing memory.</value>
    /// <remarks>
    /// Devices use this for DMA operations and to access memory-mapped regions.
    /// The bus handles address translation and permission enforcement.
    /// </remarks>
    IMemoryBus Bus { get; }
}