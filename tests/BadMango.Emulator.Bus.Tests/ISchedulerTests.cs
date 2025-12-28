// <copyright file="ISchedulerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="IScheduler"/> interface.
/// </summary>
[TestFixture]
public class ISchedulerTests
{
    /// <summary>
    /// Verifies that IScheduler can be mocked.
    /// </summary>
    [Test]
    public void IScheduler_CanBeMocked()
    {
        var mockScheduler = new Mock<IScheduler>();
        mockScheduler.Setup(s => s.CurrentCycle).Returns(1000ul);

        Assert.That(mockScheduler.Object.CurrentCycle, Is.EqualTo(1000ul));
    }

    /// <summary>
    /// Verifies that Schedule can be called.
    /// </summary>
    [Test]
    public void IScheduler_Schedule_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();
        var mockActor = new Mock<ISchedulable>();

        mockScheduler.Object.Schedule(mockActor.Object, 500ul);

        mockScheduler.Verify(s => s.Schedule(mockActor.Object, 500ul), Times.Once);
    }

    /// <summary>
    /// Verifies that ScheduleAfter can be called.
    /// </summary>
    [Test]
    public void IScheduler_ScheduleAfter_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();
        var mockActor = new Mock<ISchedulable>();

        mockScheduler.Object.ScheduleAfter(mockActor.Object, 100ul);

        mockScheduler.Verify(s => s.ScheduleAfter(mockActor.Object, 100ul), Times.Once);
    }

    /// <summary>
    /// Verifies that Drain can be called.
    /// </summary>
    [Test]
    public void IScheduler_Drain_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();

        mockScheduler.Object.Drain();

        mockScheduler.Verify(s => s.Drain(), Times.Once);
    }

    /// <summary>
    /// Verifies that RunUntil can be called.
    /// </summary>
    [Test]
    public void IScheduler_RunUntil_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();

        mockScheduler.Object.RunUntil(5000ul);

        mockScheduler.Verify(s => s.RunUntil(5000ul), Times.Once);
    }

    /// <summary>
    /// Verifies that Cancel can be called and returns a value.
    /// </summary>
    [Test]
    public void IScheduler_Cancel_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();
        var mockActor = new Mock<ISchedulable>();
        mockScheduler.Setup(s => s.Cancel(mockActor.Object)).Returns(true);

        var result = mockScheduler.Object.Cancel(mockActor.Object);

        Assert.That(result, Is.True);
        mockScheduler.Verify(s => s.Cancel(mockActor.Object), Times.Once);
    }

    /// <summary>
    /// Verifies that AdvanceCycles can be called.
    /// </summary>
    [Test]
    public void IScheduler_AdvanceCycles_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();

        mockScheduler.Object.AdvanceCycles(100ul);

        mockScheduler.Verify(s => s.AdvanceCycles(100ul), Times.Once);
    }
}