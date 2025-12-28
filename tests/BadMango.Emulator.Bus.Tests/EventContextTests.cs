// <copyright file="EventContextTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Core.Interfaces.Signaling;

using Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="EventContext"/> class.
/// </summary>
[TestFixture]
public class EventContextTests
{
    /// <summary>
    /// Verifies that EventContext can be created with valid parameters.
    /// </summary>
    [Test]
    public void EventContext_CanBeCreatedWithValidParameters()
    {
        var mockScheduler = new Mock<IScheduler>();
        var mockSignals = new Mock<ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();

        var context = new EventContext(mockScheduler.Object, mockSignals.Object, mockBus.Object);

        Assert.Multiple(() =>
        {
            Assert.That(context.Scheduler, Is.SameAs(mockScheduler.Object));
            Assert.That(context.Signals, Is.SameAs(mockSignals.Object));
            Assert.That(context.Bus, Is.SameAs(mockBus.Object));
        });
    }

    /// <summary>
    /// Verifies that EventContext throws for null scheduler.
    /// </summary>
    [Test]
    public void EventContext_NullScheduler_ThrowsArgumentNullException()
    {
        var mockSignals = new Mock<ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();

        Assert.Throws<ArgumentNullException>(() => new EventContext(null!, mockSignals.Object, mockBus.Object));
    }

    /// <summary>
    /// Verifies that EventContext throws for null signals.
    /// </summary>
    [Test]
    public void EventContext_NullSignals_ThrowsArgumentNullException()
    {
        var mockScheduler = new Mock<IScheduler>();
        var mockBus = new Mock<IMemoryBus>();

        Assert.Throws<ArgumentNullException>(() => new EventContext(mockScheduler.Object, null!, mockBus.Object));
    }

    /// <summary>
    /// Verifies that EventContext throws for null bus.
    /// </summary>
    [Test]
    public void EventContext_NullBus_ThrowsArgumentNullException()
    {
        var mockScheduler = new Mock<IScheduler>();
        var mockSignals = new Mock<ISignalBus>();

        Assert.Throws<ArgumentNullException>(() => new EventContext(mockScheduler.Object, mockSignals.Object, null!));
    }

    /// <summary>
    /// Verifies that CurrentCycle returns the scheduler's current cycle.
    /// </summary>
    [Test]
    public void CurrentCycle_ReturnsSchedulerCurrentCycle()
    {
        var mockScheduler = new Mock<IScheduler>();
        mockScheduler.Setup(s => s.CurrentCycle).Returns(12345ul);
        var mockSignals = new Mock<ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();

        var context = new EventContext(mockScheduler.Object, mockSignals.Object, mockBus.Object);

        Assert.That(context.CurrentCycle, Is.EqualTo(12345ul));
    }

    /// <summary>
    /// Verifies that CurrentCycle reflects scheduler changes.
    /// </summary>
    [Test]
    public void CurrentCycle_ReflectsSchedulerChanges()
    {
        var scheduler = new Scheduler();
        var mockSignals = new Mock<ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();

        var context = new EventContext(scheduler, mockSignals.Object, mockBus.Object);

        Assert.That(context.CurrentCycle, Is.EqualTo(0ul));

        scheduler.RunUntil(500ul);

        Assert.That(context.CurrentCycle, Is.EqualTo(500ul));
    }

    /// <summary>
    /// Verifies EventContext can be used with real Scheduler.
    /// </summary>
    [Test]
    public void EventContext_WorksWithRealScheduler()
    {
        var scheduler = new Scheduler();
        var signals = new SignalBus();
        var mockBus = new Mock<IMemoryBus>();

        var context = new EventContext(scheduler, signals, mockBus.Object);

        Assert.Multiple(() =>
        {
            Assert.That(context.Scheduler, Is.SameAs(scheduler));
            Assert.That(context.Signals, Is.SameAs(signals));
            Assert.That(context.Bus, Is.SameAs(mockBus.Object));
            Assert.That(context.CurrentCycle, Is.EqualTo(0ul));
        });
    }
}