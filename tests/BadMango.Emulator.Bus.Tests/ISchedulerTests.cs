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
        mockScheduler.Setup(s => s.Now).Returns(1000ul);

        Assert.That((ulong)mockScheduler.Object.Now, Is.EqualTo(1000ul));
    }

    /// <summary>
    /// Verifies that ScheduleAt can be called.
    /// </summary>
    [Test]
    public void IScheduler_ScheduleAt_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();
        Action<IEventContext> callback = _ => { };
        var expectedHandle = new EventHandle(1);
        mockScheduler.Setup(s => s.ScheduleAt(500ul, ScheduledEventKind.DeviceTimer, 0, callback, null))
            .Returns(expectedHandle);

        var handle = mockScheduler.Object.ScheduleAt(500ul, ScheduledEventKind.DeviceTimer, 0, callback, null);

        Assert.That(handle, Is.EqualTo(expectedHandle));
        mockScheduler.Verify(s => s.ScheduleAt(500ul, ScheduledEventKind.DeviceTimer, 0, callback, null), Times.Once);
    }

    /// <summary>
    /// Verifies that ScheduleAfter can be called.
    /// </summary>
    [Test]
    public void IScheduler_ScheduleAfter_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();
        Action<IEventContext> callback = _ => { };
        var expectedHandle = new EventHandle(2);
        mockScheduler.Setup(s => s.ScheduleAfter(100ul, ScheduledEventKind.AudioTick, 1, callback, "tag"))
            .Returns(expectedHandle);

        var handle = mockScheduler.Object.ScheduleAfter(100ul, ScheduledEventKind.AudioTick, 1, callback, "tag");

        Assert.That(handle, Is.EqualTo(expectedHandle));
        mockScheduler.Verify(s => s.ScheduleAfter(100ul, ScheduledEventKind.AudioTick, 1, callback, "tag"), Times.Once);
    }

    /// <summary>
    /// Verifies that DispatchDue can be called.
    /// </summary>
    [Test]
    public void IScheduler_DispatchDue_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();

        mockScheduler.Object.DispatchDue();

        mockScheduler.Verify(s => s.DispatchDue(), Times.Once);
    }

    /// <summary>
    /// Verifies that Cancel can be called and returns a value.
    /// </summary>
    [Test]
    public void IScheduler_Cancel_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();
        var handle = new EventHandle(42);
        mockScheduler.Setup(s => s.Cancel(handle)).Returns(true);

        var result = mockScheduler.Object.Cancel(handle);

        Assert.That(result, Is.True);
        mockScheduler.Verify(s => s.Cancel(handle), Times.Once);
    }

    /// <summary>
    /// Verifies that Advance can be called.
    /// </summary>
    [Test]
    public void IScheduler_Advance_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();

        mockScheduler.Object.Advance(100ul);

        mockScheduler.Verify(s => s.Advance(100ul), Times.Once);
    }

    /// <summary>
    /// Verifies that PeekNextDue can be called.
    /// </summary>
    [Test]
    public void IScheduler_PeekNextDue_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();
        mockScheduler.Setup(s => s.PeekNextDue()).Returns((Core.Cycle?)100ul);

        var result = mockScheduler.Object.PeekNextDue();

        Assert.That(result, Is.EqualTo((Core.Cycle?)100ul));
        mockScheduler.Verify(s => s.PeekNextDue(), Times.Once);
    }

    /// <summary>
    /// Verifies that PeekNextDue can return null.
    /// </summary>
    [Test]
    public void IScheduler_PeekNextDue_CanReturnNull()
    {
        var mockScheduler = new Mock<IScheduler>();
        mockScheduler.Setup(s => s.PeekNextDue()).Returns((Core.Cycle?)null);

        var result = mockScheduler.Object.PeekNextDue();

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies that JumpToNextEventAndDispatch can be called.
    /// </summary>
    [Test]
    public void IScheduler_JumpToNextEventAndDispatch_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();
        mockScheduler.Setup(s => s.JumpToNextEventAndDispatch()).Returns(true);

        var result = mockScheduler.Object.JumpToNextEventAndDispatch();

        Assert.That(result, Is.True);
        mockScheduler.Verify(s => s.JumpToNextEventAndDispatch(), Times.Once);
    }

    /// <summary>
    /// Verifies that Reset can be called.
    /// </summary>
    [Test]
    public void IScheduler_Reset_CanBeCalled()
    {
        var mockScheduler = new Mock<IScheduler>();

        mockScheduler.Object.Reset();

        mockScheduler.Verify(s => s.Reset(), Times.Once);
    }

    /// <summary>
    /// Verifies that PendingEventCount can be read.
    /// </summary>
    [Test]
    public void IScheduler_PendingEventCount_CanBeRead()
    {
        var mockScheduler = new Mock<IScheduler>();
        mockScheduler.Setup(s => s.PendingEventCount).Returns(5);

        var count = mockScheduler.Object.PendingEventCount;

        Assert.That(count, Is.EqualTo(5));
    }
}