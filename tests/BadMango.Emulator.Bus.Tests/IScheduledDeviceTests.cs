// <copyright file="IScheduledDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Interfaces.Signaling;
using BadMango.Emulator.Core.Signaling;

using Interfaces;

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
        scheduler.SetEventContext(context);

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
        scheduler.SetEventContext(context);

        var device = new TestScheduledDevice();
        device.Initialize(context);

        Assert.That(device.EventFired, Is.False);

        scheduler.Advance(100ul);

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
        scheduler.SetEventContext(context);

        var device1 = new TestScheduledDevice("Device1", 50ul);
        var device2 = new TestScheduledDevice("Device2", 100ul);

        device1.Initialize(context);
        device2.Initialize(context);

        Assert.That(scheduler.PendingEventCount, Is.EqualTo(2));

        scheduler.Advance(75ul);

        Assert.Multiple(() =>
        {
            Assert.That(device1.EventFired, Is.True);
            Assert.That(device2.EventFired, Is.False);
        });

        scheduler.Advance(75ul);

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
        scheduler.SetEventContext(context);

        var device = new InterruptRaisingDevice();
        device.Initialize(context);

        Assert.That(signals.IsAsserted(SignalLine.IRQ), Is.False);

        scheduler.Advance(100ul);

        Assert.That(signals.IsAsserted(SignalLine.IRQ), Is.True);
    }

    /// <summary>
    /// Test device that schedules an event during initialization.
    /// </summary>
    private sealed class TestScheduledDevice : IScheduledDevice
    {
        private readonly string name;
        private readonly ulong scheduledCycle;

        public TestScheduledDevice(string name = "TestDevice", ulong scheduledCycle = 100ul)
        {
            this.name = name;
            this.scheduledCycle = scheduledCycle;
        }

        public string Name => name;

        public bool EventFired { get; private set; }

        public void Initialize(IEventContext context)
        {
            context.Scheduler.ScheduleAt(scheduledCycle, ScheduledEventKind.DeviceTimer, 0, _ => EventFired = true, tag: this);
        }
    }

    /// <summary>
    /// Device that raises an IRQ when its scheduled event fires.
    /// </summary>
    private sealed class InterruptRaisingDevice : IScheduledDevice
    {
        public string Name => "InterruptDevice";

        public void Initialize(IEventContext context)
        {
            _ = context.Scheduler.ScheduleAt(
                100ul,
                ScheduledEventKind.InterruptLineChange,
                0,
                ctx =>
                {
                    ctx.Signals.Assert(SignalLine.IRQ, 1, ctx.Now);
                },
                tag: this);
        }
    }
}