// <copyright file="ScheduledEventTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Moq;

/// <summary>
/// Unit tests for the <see cref="ScheduledEvent"/> struct.
/// </summary>
[TestFixture]
public class ScheduledEventTests
{
    /// <summary>
    /// Verifies that ScheduledEvent can be created with all properties.
    /// </summary>
    [Test]
    public void ScheduledEvent_CanBeCreatedWithProperties()
    {
        var mockActor = new Mock<ISchedulable>();
        var evt = new ScheduledEvent(100ul, 5L, mockActor.Object);

        Assert.Multiple(() =>
        {
            Assert.That(evt.Cycle, Is.EqualTo(100ul));
            Assert.That(evt.Sequence, Is.EqualTo(5L));
            Assert.That(evt.Actor, Is.SameAs(mockActor.Object));
        });
    }

    /// <summary>
    /// Verifies CompareTo orders by cycle first.
    /// </summary>
    [Test]
    public void ScheduledEvent_CompareTo_OrdersByCycleFirst()
    {
        var mockActor = new Mock<ISchedulable>();
        var earlier = new ScheduledEvent(100ul, 0L, mockActor.Object);
        var later = new ScheduledEvent(200ul, 0L, mockActor.Object);

        Assert.Multiple(() =>
        {
            Assert.That(earlier.CompareTo(later), Is.LessThan(0));
            Assert.That(later.CompareTo(earlier), Is.GreaterThan(0));
        });
    }

    /// <summary>
    /// Verifies CompareTo uses sequence as tie-breaker when cycles are equal.
    /// </summary>
    [Test]
    public void ScheduledEvent_CompareTo_UsesSequenceAsTieBreaker()
    {
        var mockActor = new Mock<ISchedulable>();
        var first = new ScheduledEvent(100ul, 1L, mockActor.Object);
        var second = new ScheduledEvent(100ul, 2L, mockActor.Object);

        Assert.Multiple(() =>
        {
            Assert.That(first.CompareTo(second), Is.LessThan(0));
            Assert.That(second.CompareTo(first), Is.GreaterThan(0));
        });
    }

    /// <summary>
    /// Verifies CompareTo returns zero for equal events.
    /// </summary>
    [Test]
    public void ScheduledEvent_CompareTo_ReturnsZeroForEqual()
    {
        var mockActor = new Mock<ISchedulable>();
        var event1 = new ScheduledEvent(100ul, 5L, mockActor.Object);
        var event2 = new ScheduledEvent(100ul, 5L, mockActor.Object);

        Assert.That(event1.CompareTo(event2), Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies record equality works correctly.
    /// </summary>
    [Test]
    public void ScheduledEvent_RecordEquality_Works()
    {
        var mockActor = new Mock<ISchedulable>();
        var event1 = new ScheduledEvent(100ul, 5L, mockActor.Object);
        var event2 = new ScheduledEvent(100ul, 5L, mockActor.Object);
        var event3 = new ScheduledEvent(200ul, 5L, mockActor.Object);

        Assert.Multiple(() =>
        {
            Assert.That(event1, Is.EqualTo(event2));
            Assert.That(event1, Is.Not.EqualTo(event3));
        });
    }
}