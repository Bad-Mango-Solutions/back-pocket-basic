// <copyright file="IEventAggregator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Infrastructure.Events;

/// <summary>
/// Coordinates communication between components using a publish-subscribe pattern.
/// This is the core eventing infrastructure for the entire emulator system.
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish.</typeparam>
    /// <param name="eventData">The event data to publish.</param>
    void Publish<TEvent>(TEvent eventData)
        where TEvent : class;

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when the event is published.</param>
    /// <returns>A disposable that unsubscribes from the event when disposed.</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : class;
}