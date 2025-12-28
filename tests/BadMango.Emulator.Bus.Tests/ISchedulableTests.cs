// <copyright file="ISchedulableTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="ISchedulable"/> interface.
/// </summary>
[TestFixture]
public class ISchedulableTests
{
    /// <summary>
    /// Verifies that ISchedulable can be mocked and executed.
    /// </summary>
    [Test]
    public void ISchedulable_CanBeMockedAndExecuted()
    {
        var mockActor = new Mock<ISchedulable>();
        var mockScheduler = new Mock<IScheduler>();

        mockActor.Setup(a => a.Execute(100ul, mockScheduler.Object)).Returns(5ul);

        var cycles = mockActor.Object.Execute(100ul, mockScheduler.Object);

        Assert.That(cycles, Is.EqualTo(5ul));
        mockActor.Verify(a => a.Execute(100ul, mockScheduler.Object), Times.Once);
    }

    /// <summary>
    /// Verifies that ISchedulable can return zero cycles.
    /// </summary>
    [Test]
    public void ISchedulable_CanReturnZeroCycles()
    {
        var mockActor = new Mock<ISchedulable>();
        var mockScheduler = new Mock<IScheduler>();

        mockActor.Setup(a => a.Execute(It.IsAny<ulong>(), It.IsAny<IScheduler>())).Returns(0ul);

        var cycles = mockActor.Object.Execute(50ul, mockScheduler.Object);

        Assert.That(cycles, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies that ISchedulable can schedule future events.
    /// </summary>
    [Test]
    public void ISchedulable_CanScheduleFutureEvents()
    {
        var mockScheduler = new Mock<IScheduler>();
        var actor = new ReschedulingActor();

        actor.Execute(100ul, mockScheduler.Object);

        mockScheduler.Verify(s => s.ScheduleAfter(actor, 10ul), Times.Once);
    }

    private sealed class ReschedulingActor : ISchedulable
    {
        public ulong Execute(ulong currentCycle, IScheduler scheduler)
        {
            scheduler.ScheduleAfter(this, 10ul);
            return 5ul;
        }
    }
}