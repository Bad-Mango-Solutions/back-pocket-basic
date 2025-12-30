// <copyright file="ScheduledEventTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

/// <summary>
/// Unit tests for the <see cref="ScheduledEvent"/> struct.
/// </summary>
[TestFixture]
public class ScheduledEventTests
{
    private static readonly Action<IEventContext> NoOpCallback = _ => { };

    /// <summary>
    /// Verifies that ScheduledEvent can be created with all properties.
    /// </summary>
    [Test]
    public void ScheduledEvent_CanBeCreatedWithProperties()
    {
        var handle = new EventHandle(1);
        var callback = NoOpCallback;
        var tag = "TestTag";

        var evt = new ScheduledEvent(handle, 100ul, 5, 10L, ScheduledEventKind.DeviceTimer, callback, tag);

        Assert.Multiple(() =>
        {
            Assert.That(evt.Handle, Is.EqualTo(handle));
            Assert.That(evt.Cycle, Is.EqualTo(100ul));
            Assert.That(evt.Priority, Is.EqualTo(5));
            Assert.That(evt.Sequence, Is.EqualTo(10L));
            Assert.That(evt.Kind, Is.EqualTo(ScheduledEventKind.DeviceTimer));
            Assert.That(evt.Callback, Is.SameAs(callback));
            Assert.That(evt.Tag, Is.EqualTo(tag));
        });
    }

    /// <summary>
    /// Verifies CompareTo orders by cycle first.
    /// </summary>
    [Test]
    public void ScheduledEvent_CompareTo_OrdersByCycleFirst()
    {
        var earlier = new ScheduledEvent(new EventHandle(1), 100ul, 0, 0L, ScheduledEventKind.DeviceTimer, NoOpCallback, null);
        var later = new ScheduledEvent(new EventHandle(2), 200ul, 0, 0L, ScheduledEventKind.DeviceTimer, NoOpCallback, null);

        Assert.Multiple(() =>
        {
            Assert.That(earlier.CompareTo(later), Is.LessThan(0));
            Assert.That(later.CompareTo(earlier), Is.GreaterThan(0));
        });
    }

    /// <summary>
    /// Verifies CompareTo uses priority as second ordering criterion.
    /// </summary>
    [Test]
    public void ScheduledEvent_CompareTo_UsesPrioritySecond()
    {
        var highPriority = new ScheduledEvent(new EventHandle(1), 100ul, 0, 0L, ScheduledEventKind.DeviceTimer, NoOpCallback, null);
        var lowPriority = new ScheduledEvent(new EventHandle(2), 100ul, 10, 0L, ScheduledEventKind.DeviceTimer, NoOpCallback, null);

        Assert.Multiple(() =>
        {
            Assert.That(highPriority.CompareTo(lowPriority), Is.LessThan(0));
            Assert.That(lowPriority.CompareTo(highPriority), Is.GreaterThan(0));
        });
    }

    /// <summary>
    /// Verifies CompareTo uses sequence as tie-breaker when cycle and priority are equal.
    /// </summary>
    [Test]
    public void ScheduledEvent_CompareTo_UsesSequenceAsTieBreaker()
    {
        var first = new ScheduledEvent(new EventHandle(1), 100ul, 0, 1L, ScheduledEventKind.DeviceTimer, NoOpCallback, null);
        var second = new ScheduledEvent(new EventHandle(2), 100ul, 0, 2L, ScheduledEventKind.DeviceTimer, NoOpCallback, null);

        Assert.Multiple(() =>
        {
            Assert.That(first.CompareTo(second), Is.LessThan(0));
            Assert.That(second.CompareTo(first), Is.GreaterThan(0));
        });
    }

    /// <summary>
    /// Verifies CompareTo returns zero for events with same cycle, priority, and sequence.
    /// </summary>
    [Test]
    public void ScheduledEvent_CompareTo_ReturnsZeroForEqualOrdering()
    {
        var event1 = new ScheduledEvent(new EventHandle(1), 100ul, 5, 10L, ScheduledEventKind.DeviceTimer, NoOpCallback, null);
        var event2 = new ScheduledEvent(new EventHandle(2), 100ul, 5, 10L, ScheduledEventKind.AudioTick, NoOpCallback, "different");

        // Different handle, kind, callback, tag - but same cycle, priority, sequence should compare equal
        Assert.That(event1.CompareTo(event2), Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies record equality works correctly.
    /// </summary>
    [Test]
    public void ScheduledEvent_RecordEquality_Works()
    {
        var callback = NoOpCallback;
        var event1 = new ScheduledEvent(new EventHandle(1), 100ul, 5, 10L, ScheduledEventKind.DeviceTimer, callback, null);
        var event2 = new ScheduledEvent(new EventHandle(1), 100ul, 5, 10L, ScheduledEventKind.DeviceTimer, callback, null);
        var event3 = new ScheduledEvent(new EventHandle(2), 200ul, 5, 10L, ScheduledEventKind.DeviceTimer, callback, null);

        Assert.Multiple(() =>
        {
            Assert.That(event1, Is.EqualTo(event2));
            Assert.That(event1, Is.Not.EqualTo(event3));
        });
    }

    /// <summary>
    /// Verifies that null tag is allowed.
    /// </summary>
    [Test]
    public void ScheduledEvent_NullTag_IsAllowed()
    {
        var evt = new ScheduledEvent(new EventHandle(1), 100ul, 0, 0L, ScheduledEventKind.DeviceTimer, NoOpCallback, null);

        Assert.That(evt.Tag, Is.Null);
    }

    /// <summary>
    /// Verifies that different event kinds can be used.
    /// </summary>
    [Test]
    public void ScheduledEvent_DifferentKinds_CanBeCreated()
    {
        var kinds = Enum.GetValues<ScheduledEventKind>();

        foreach (var kind in kinds)
        {
            var evt = new ScheduledEvent(new EventHandle(1), 100ul, 0, 0L, kind, NoOpCallback, null);
            Assert.That(evt.Kind, Is.EqualTo(kind));
        }
    }
}