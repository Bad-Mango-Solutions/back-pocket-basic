// <copyright file="EventAggregatorTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using BadMango.Emulator.UI.Abstractions.Events;
using BadMango.Emulator.UI.Services;

/// <summary>
/// Tests for <see cref="EventAggregator"/>.
/// </summary>
[TestFixture]
public class EventAggregatorTests
{
    private EventAggregator aggregator = null!;

    /// <summary>
    /// Sets up the test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        aggregator = new EventAggregator();
    }

    /// <summary>
    /// Tests that publishing an event invokes the subscriber.
    /// </summary>
    [Test]
    public void Publish_InvokesSubscriber()
    {
        // Arrange
        MachineStateChangedEvent? receivedEvent = null;
        aggregator.Subscribe<MachineStateChangedEvent>(e => receivedEvent = e);

        var testEvent = new MachineStateChangedEvent("machine-1", "Running");

        // Act
        aggregator.Publish(testEvent);

        // Assert
        Assert.That(receivedEvent, Is.Not.Null);
        Assert.That(receivedEvent!.MachineId, Is.EqualTo("machine-1"));
        Assert.That(receivedEvent.NewState, Is.EqualTo("Running"));
    }

    /// <summary>
    /// Tests that multiple subscribers all receive the event.
    /// </summary>
    [Test]
    public void Publish_InvokesMultipleSubscribers()
    {
        // Arrange
        int callCount = 0;
        aggregator.Subscribe<MachineStateChangedEvent>(_ => callCount++);
        aggregator.Subscribe<MachineStateChangedEvent>(_ => callCount++);

        var testEvent = new MachineStateChangedEvent("machine-1", "Running");

        // Act
        aggregator.Publish(testEvent);

        // Assert
        Assert.That(callCount, Is.EqualTo(2));
    }

    /// <summary>
    /// Tests that disposing the subscription stops receiving events.
    /// </summary>
    [Test]
    public void Subscribe_DisposingUnsubscribes()
    {
        // Arrange
        int callCount = 0;
        var subscription = aggregator.Subscribe<MachineStateChangedEvent>(_ => callCount++);

        // Act
        aggregator.Publish(new MachineStateChangedEvent("machine-1", "Running"));
        subscription.Dispose();
        aggregator.Publish(new MachineStateChangedEvent("machine-2", "Stopped"));

        // Assert
        Assert.That(callCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that publishing to non-existing subscribers does not throw.
    /// </summary>
    [Test]
    public void Publish_NoSubscribers_DoesNotThrow()
    {
        // Arrange
        var testEvent = new MachineStateChangedEvent("machine-1", "Running");

        // Act & Assert
        Assert.DoesNotThrow(() => aggregator.Publish(testEvent));
    }

    /// <summary>
    /// Tests that different event types are isolated.
    /// </summary>
    [Test]
    public void Subscribe_DifferentEventTypes_AreIsolated()
    {
        // Arrange
        MachineStateChangedEvent? machineEvent = null;
        BreakpointHitEvent? breakpointEvent = null;

        aggregator.Subscribe<MachineStateChangedEvent>(e => machineEvent = e);
        aggregator.Subscribe<BreakpointHitEvent>(e => breakpointEvent = e);

        // Act
        aggregator.Publish(new MachineStateChangedEvent("machine-1", "Running"));

        // Assert
        Assert.That(machineEvent, Is.Not.Null);
        Assert.That(breakpointEvent, Is.Null);
    }

    /// <summary>
    /// Tests that disposing a subscription multiple times is safe.
    /// </summary>
    [Test]
    public void Subscribe_DoubleDispose_IsSafe()
    {
        // Arrange
        var subscription = aggregator.Subscribe<MachineStateChangedEvent>(_ => { });

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            subscription.Dispose();
            subscription.Dispose();
        });
    }

    /// <summary>
    /// Tests that errors in one subscriber do not prevent other subscribers from receiving events.
    /// </summary>
    [Test]
    public void Publish_SubscriberThrows_OtherSubscribersStillReceive()
    {
        // Arrange
        int callCount = 0;
        aggregator.Subscribe<MachineStateChangedEvent>(_ => throw new InvalidOperationException("Test error"));
        aggregator.Subscribe<MachineStateChangedEvent>(_ => callCount++);

        // Act
        aggregator.Publish(new MachineStateChangedEvent("machine-1", "Running"));

        // Assert
        Assert.That(callCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that null event throws ArgumentNullException.
    /// </summary>
    [Test]
    public void Publish_NullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => aggregator.Publish<MachineStateChangedEvent>(null!));
    }

    /// <summary>
    /// Tests that null handler throws ArgumentNullException.
    /// </summary>
    [Test]
    public void Subscribe_NullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => aggregator.Subscribe<MachineStateChangedEvent>(null!));
    }
}