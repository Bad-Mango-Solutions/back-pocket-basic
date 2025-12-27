// <copyright file="IScheduledDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Moq;

/// <summary>
/// Unit tests for the <see cref="IScheduledDevice"/> interface.
/// </summary>
[TestFixture]
public class IScheduledDeviceTests
{
    /// <summary>
    /// Verifies that IScheduledDevice can be mocked.
    /// </summary>
    [Test]
    public void IScheduledDevice_CanBeMocked()
    {
        var mockDevice = new Mock<IScheduledDevice>();
        mockDevice.Setup(d => d.Name).Returns("TestDevice");

        Assert.That(mockDevice.Object.Name, Is.EqualTo("TestDevice"));
    }

    /// <summary>
    /// Verifies that Initialize can be called with an event context.
    /// </summary>
    [Test]
    public void IScheduledDevice_Initialize_CanBeCalled()
    {
        var mockDevice = new Mock<IScheduledDevice>();
        var mockContext = new Mock<IEventContext>();

        mockDevice.Object.Initialize(mockContext.Object);

        mockDevice.Verify(d => d.Initialize(mockContext.Object), Times.Once);
    }

    /// <summary>
    /// Verifies that a device can schedule events during initialization.
    /// </summary>
    [Test]
    public void Device_CanScheduleEventsDuringInitialization()
    {
        var scheduler = new Scheduler();
        var mockSignals = new Mock<ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();
        var context = new EventContext(scheduler, mockSignals.Object, mockBus.Object);

        var device = new TestScheduledDevice();
        device.Initialize(context);

        Assert.That(scheduler.PendingEventCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that device events are executed at the correct cycle.
    /// </summary>
    [Test]
    public void Device_ScheduledEvents_ExecuteAtCorrectCycle()
    {
        var scheduler = new Scheduler();
        var mockSignals = new Mock<ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();
        var context = new EventContext(scheduler, mockSignals.Object, mockBus.Object);

        var device = new TestScheduledDevice();
        device.Initialize(context);

        Assert.That(device.EventFired, Is.False);

        scheduler.RunUntil(100ul);

        Assert.That(device.EventFired, Is.True);
    }

    /// <summary>
    /// Verifies that multiple devices can schedule events.
    /// </summary>
    [Test]
    public void MultipleDevices_CanScheduleEvents()
    {
        var scheduler = new Scheduler();
        var mockSignals = new Mock<ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();
        var context = new EventContext(scheduler, mockSignals.Object, mockBus.Object);

        var device1 = new TestScheduledDevice("Device1", 50ul);
        var device2 = new TestScheduledDevice("Device2", 100ul);

        device1.Initialize(context);
        device2.Initialize(context);

        Assert.That(scheduler.PendingEventCount, Is.EqualTo(2));

        scheduler.RunUntil(75ul);

        Assert.Multiple(() =>
        {
            Assert.That(device1.EventFired, Is.True);
            Assert.That(device2.EventFired, Is.False);
        });

        scheduler.RunUntil(150ul);

        Assert.Multiple(() =>
        {
            Assert.That(device1.EventFired, Is.True);
            Assert.That(device2.EventFired, Is.True);
        });
    }

    /// <summary>
    /// Verifies that a device can use signals during scheduled callback.
    /// </summary>
    [Test]
    public void Device_CanUseSignals_DuringScheduledCallback()
    {
        var scheduler = new Scheduler();
        var signals = new SignalBus();
        var mockBus = new Mock<IMemoryBus>();
        var context = new EventContext(scheduler, signals, mockBus.Object);

        var device = new InterruptRaisingDevice();
        device.Initialize(context);

        Assert.That(signals.IsIrqAsserted, Is.False);

        scheduler.RunUntil(100ul);

        Assert.That(signals.IsIrqAsserted, Is.True);
    }

    /// <summary>
    /// Test device that schedules an event during initialization.
    /// </summary>
    private sealed class TestScheduledDevice : IScheduledDevice, ISchedulable
    {
        private readonly string name;
        private readonly ulong scheduledCycle;
        private IEventContext? context;

        public TestScheduledDevice(string name = "TestDevice", ulong scheduledCycle = 100ul)
        {
            this.name = name;
            this.scheduledCycle = scheduledCycle;
        }

        public string Name => name;

        public bool EventFired { get; private set; }

        public void Initialize(IEventContext context)
        {
            this.context = context;
            context.Scheduler.Schedule(this, scheduledCycle);
        }

        public ulong Execute(ulong currentCycle, IScheduler scheduler)
        {
            EventFired = true;
            return 0;
        }
    }

    /// <summary>
    /// Device that raises an IRQ when its scheduled event fires.
    /// </summary>
    private sealed class InterruptRaisingDevice : IScheduledDevice, ISchedulable
    {
        private IEventContext? context;

        public string Name => "InterruptDevice";

        public void Initialize(IEventContext context)
        {
            this.context = context;
            context.Scheduler.Schedule(this, 100ul);
        }

        public ulong Execute(ulong currentCycle, IScheduler scheduler)
        {
            context?.Signals.Assert(SignalLine.Irq, 1, currentCycle);
            return 0;
        }
    }
}