// <copyright file="EventAggregatorTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Infrastructure.Tests;

using BadMango.Emulator.Infrastructure.Events;

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
        aggregator = new();
    }

    /// <summary>
    /// Tests that publishing an event invokes the subscriber.
    /// </summary>
    [Test]
    public void Publish_InvokesSubscriber()
    {
        // Arrange
        TestEventA? receivedEvent = null;
        aggregator.Subscribe<TestEventA>(e => receivedEvent = e);

        var testEvent = new TestEventA("test-1", "Value1");

        // Act
        aggregator.Publish(testEvent);

        // Assert
        Assert.That(receivedEvent, Is.Not.Null);
        Assert.That(receivedEvent!.Id, Is.EqualTo("test-1"));
        Assert.That(receivedEvent.Data, Is.EqualTo("Value1"));
    }

    /// <summary>
    /// Tests that multiple subscribers all receive the event.
    /// </summary>
    [Test]
    public void Publish_InvokesMultipleSubscribers()
    {
        // Arrange
        int callCount = 0;
        aggregator.Subscribe<TestEventA>(_ => callCount++);
        aggregator.Subscribe<TestEventA>(_ => callCount++);

        var testEvent = new TestEventA("test-1", "Value1");

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
        var subscription = aggregator.Subscribe<TestEventA>(_ => callCount++);

        // Act
        aggregator.Publish(new TestEventA("test-1", "Value1"));
        subscription.Dispose();
        aggregator.Publish(new TestEventA("test-2", "Value2"));

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
        var testEvent = new TestEventA("test-1", "Value1");

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
        TestEventA? eventA = null;
        TestEventB? eventB = null;

        aggregator.Subscribe<TestEventA>(e => eventA = e);
        aggregator.Subscribe<TestEventB>(e => eventB = e);

        // Act
        aggregator.Publish(new TestEventA("test-1", "Value1"));

        // Assert
        Assert.That(eventA, Is.Not.Null);
        Assert.That(eventB, Is.Null);
    }

    /// <summary>
    /// Tests that disposing a subscription multiple times is safe.
    /// </summary>
    [Test]
    public void Subscribe_DoubleDispose_IsSafe()
    {
        // Arrange
        var subscription = aggregator.Subscribe<TestEventA>(_ => { });

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
        aggregator.Subscribe<TestEventA>(_ => throw new InvalidOperationException("Test error"));
        aggregator.Subscribe<TestEventA>(_ => callCount++);

        // Act
        aggregator.Publish(new TestEventA("test-1", "Value1"));

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
        Assert.Throws<ArgumentNullException>(() => aggregator.Publish<TestEventA>(null!));
    }

    /// <summary>
    /// Tests that null handler throws ArgumentNullException.
    /// </summary>
    [Test]
    public void Subscribe_NullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => aggregator.Subscribe<TestEventA>(null!));
    }

    /// <summary>
    /// Test event A for testing event aggregator.
    /// </summary>
    /// <param name="Id">The event identifier.</param>
    /// <param name="Data">The event data.</param>
    private record TestEventA(string Id, string Data);

    /// <summary>
    /// Test event B for testing event type isolation.
    /// </summary>
    /// <param name="Id">The event identifier.</param>
    /// <param name="Value">The event value.</param>
    private record TestEventB(string Id, int Value);
}