// <copyright file="EventContextTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Core.Interfaces.Signaling;
using BadMango.Emulator.Core.Signaling;

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
    /// Verifies that Now returns the scheduler's current cycle.
    /// </summary>
    [Test]
    public void CurrentCycle_ReturnsSchedulerCurrentCycle()
    {
        var mockScheduler = new Mock<IScheduler>();
        mockScheduler.Setup(s => s.Now).Returns(12345ul);
        var mockSignals = new Mock<ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();

        var context = new EventContext(mockScheduler.Object, mockSignals.Object, mockBus.Object);

        Assert.That((ulong)context.Now, Is.EqualTo(12345ul));
    }

    /// <summary>
    /// Verifies that Now reflects scheduler changes.
    /// </summary>
    [Test]
    public void CurrentCycle_ReflectsSchedulerChanges()
    {
        var scheduler = new Scheduler();
        var mockSignals = new Mock<ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();

        var context = new EventContext(scheduler, mockSignals.Object, mockBus.Object);

        Assert.That((ulong)context.Now, Is.EqualTo(0ul));

        scheduler.Advance(500ul);

        Assert.That((ulong)context.Now, Is.EqualTo(500ul));
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
            Assert.That((ulong)context.Now, Is.EqualTo(0ul));
        });
    }

    /// <summary>
    /// Verifies GetComponent returns component when added.
    /// </summary>
    [Test]
    public void GetComponent_WhenAdded_ReturnsComponent()
    {
        var context = CreateTestContext();
        var component = new TestComponent { Name = "Test" };

        context.AddComponent(component);
        var result = context.GetComponent<TestComponent>();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Test"));
        });
    }

    /// <summary>
    /// Verifies GetComponent returns null when not found.
    /// </summary>
    [Test]
    public void GetComponent_WhenNotFound_ReturnsNull()
    {
        var context = CreateTestContext();

        var result = context.GetComponent<TestComponent>();

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies GetComponents returns all matching components.
    /// </summary>
    [Test]
    public void GetComponents_ReturnsAllMatching()
    {
        var context = CreateTestContext();
        context.AddComponent(new TestComponent { Name = "First" });
        context.AddComponent(new TestComponent { Name = "Second" });

        var result = context.GetComponents<TestComponent>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("First"));
            Assert.That(result[1].Name, Is.EqualTo("Second"));
        });
    }

    /// <summary>
    /// Verifies GetComponents returns empty when none found.
    /// </summary>
    [Test]
    public void GetComponents_WhenNoneFound_ReturnsEmpty()
    {
        var context = CreateTestContext();

        var result = context.GetComponents<TestComponent>();

        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Verifies HasComponent returns true when component exists.
    /// </summary>
    [Test]
    public void HasComponent_WhenExists_ReturnsTrue()
    {
        var context = CreateTestContext();
        context.AddComponent(new TestComponent());

        var result = context.HasComponent<TestComponent>();

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Verifies HasComponent returns false when component doesn't exist.
    /// </summary>
    [Test]
    public void HasComponent_WhenNotExists_ReturnsFalse()
    {
        var context = CreateTestContext();

        var result = context.HasComponent<TestComponent>();

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies AddComponent throws for null component.
    /// </summary>
    [Test]
    public void AddComponent_NullComponent_ThrowsArgumentNullException()
    {
        var context = CreateTestContext();

        Assert.Throws<ArgumentNullException>(() => context.AddComponent<object>(null!));
    }

    private static EventContext CreateTestContext()
    {
        var mockScheduler = new Mock<IScheduler>();
        var mockSignals = new Mock<ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();
        return new EventContext(mockScheduler.Object, mockSignals.Object, mockBus.Object);
    }

    private sealed class TestComponent
    {
        public string Name { get; set; } = string.Empty;
    }
}