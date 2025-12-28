// <copyright file="EventAggregator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Infrastructure.Events;

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

/// <summary>
/// Default implementation of <see cref="IEventAggregator"/> for cross-component communication.
/// </summary>
public class EventAggregator : IEventAggregator
{
    private readonly ConcurrentDictionary<Type, List<object>> subscriptions = new();
    private readonly ILogger<EventAggregator>? logger;
    private readonly object subscriptionLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventAggregator"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for event aggregator operations.</param>
    public EventAggregator(ILogger<EventAggregator>? logger = null)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent eventData)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(eventData);

        var eventType = typeof(TEvent);
        logger?.LogDebug("Publishing event of type {EventType}", eventType.Name);

        if (!subscriptions.TryGetValue(eventType, out var handlers))
        {
            logger?.LogDebug("No subscribers for event type {EventType}", eventType.Name);
            return;
        }

        List<Action<TEvent>> handlersCopy;
        lock (subscriptionLock)
        {
            handlersCopy = handlers.OfType<Action<TEvent>>().ToList();
        }

        foreach (var handler in handlersCopy)
        {
            try
            {
                handler(eventData);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error invoking handler for event type {EventType}", eventType.Name);
            }
        }

        logger?.LogDebug("Published event of type {EventType} to {Count} subscribers", eventType.Name, handlersCopy.Count);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        logger?.LogDebug("Subscribing to event type {EventType}", eventType.Name);

        lock (subscriptionLock)
        {
            if (!subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers = [];
                subscriptions[eventType] = handlers;
            }

            handlers.Add(handler);
        }

        return new Subscription<TEvent>(this, handler);
    }

    private void Unsubscribe<TEvent>(Action<TEvent> handler)
        where TEvent : class
    {
        var eventType = typeof(TEvent);
        logger?.LogDebug("Unsubscribing from event type {EventType}", eventType.Name);

        lock (subscriptionLock)
        {
            if (subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    subscriptions.TryRemove(eventType, out _);
                }
            }
        }
    }

    private sealed class Subscription<TEvent> : IDisposable
        where TEvent : class
    {
        private readonly EventAggregator aggregator;
        private readonly Action<TEvent> handler;
        private bool disposed;

        public Subscription(EventAggregator aggregator, Action<TEvent> handler)
        {
            this.aggregator = aggregator;
            this.handler = handler;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                aggregator.Unsubscribe(handler);
                disposed = true;
            }
        }
    }
}