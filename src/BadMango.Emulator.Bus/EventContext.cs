// <copyright file="EventContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core.Interfaces.Signaling;

using Interfaces;

/// <summary>
/// Standard implementation of <see cref="IEventContext"/> for device initialization and event handling.
/// </summary>
/// <remarks>
/// <para>
/// The event context bundles references to the scheduler, signal bus, and memory bus,
/// providing devices with a unified access point to system services. This is created
/// during machine assembly after all components are wired up.
/// </para>
/// <para>
/// The initialization order is:
/// </para>
/// <list type="number">
/// <item><description>Create infrastructure (registry, scheduler, signals)</description></item>
/// <item><description>Create memory bus</description></item>
/// <item><description>Create devices (RAM, ROM, I/O controllers)</description></item>
/// <item><description>Wire devices to bus (map pages)</description></item>
/// <item><description>Create CPU</description></item>
/// <item><description>Create event context</description></item>
/// <item><description>Initialize devices with event context</description></item>
/// <item><description>Assemble machine</description></item>
/// </list>
/// </remarks>
public sealed class EventContext : IEventContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventContext"/> class.
    /// </summary>
    /// <param name="scheduler">The cycle-accurate event scheduler.</param>
    /// <param name="signals">The signal bus for interrupt management.</param>
    /// <param name="bus">The main memory bus.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <see langword="null"/>.</exception>
    public EventContext(IScheduler scheduler, ISignalBus signals, IMemoryBus bus)
    {
        ArgumentNullException.ThrowIfNull(scheduler, nameof(scheduler));
        ArgumentNullException.ThrowIfNull(signals, nameof(signals));
        ArgumentNullException.ThrowIfNull(bus, nameof(bus));

        Scheduler = scheduler;
        Signals = signals;
        Bus = bus;
    }

    /// <inheritdoc />
    public ulong CurrentCycle => Scheduler.CurrentCycle;

    /// <inheritdoc />
    public IScheduler Scheduler { get; }

    /// <inheritdoc />
    public ISignalBus Signals { get; }

    /// <inheritdoc />
    public IMemoryBus Bus { get; }
}