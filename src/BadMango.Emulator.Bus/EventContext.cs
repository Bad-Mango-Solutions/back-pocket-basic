// <copyright file="EventContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Core.Interfaces.Signaling;

using Core;

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
/// <para>
/// The event context also serves as a component bucket, allowing arbitrary components
/// to be registered and retrieved by type. This provides extensibility without
/// modifying the core interface.
/// </para>
/// </remarks>
public sealed class EventContext : IEventContext
{
    private readonly List<object> components = [];

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
    public Cycle Now
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Scheduler.Now;
    }

    /// <inheritdoc />
    public IScheduler Scheduler { get; }

    /// <inheritdoc />
    public ISignalBus Signals { get; }

    /// <inheritdoc />
    public IMemoryBus Bus { get; }

    /// <inheritdoc />
    public T? GetComponent<T>()
        where T : class
    {
        return components.OfType<T>().FirstOrDefault();
    }

    /// <inheritdoc />
    public IEnumerable<T> GetComponents<T>()
        where T : class
    {
        return components.OfType<T>();
    }

    /// <inheritdoc />
    public bool HasComponent<T>()
        where T : class
    {
        return components.OfType<T>().Any();
    }

    /// <summary>
    /// Adds a component to the context's component bucket.
    /// </summary>
    /// <typeparam name="T">The type of component being added.</typeparam>
    /// <param name="component">The component instance to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="component"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Components can be retrieved later using <see cref="GetComponent{T}"/>,
    /// <see cref="GetComponents{T}"/>, or checked via <see cref="HasComponent{T}"/>.
    /// </remarks>
    public void AddComponent<T>(T component)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(component, nameof(component));
        components.Add(component);
    }
}