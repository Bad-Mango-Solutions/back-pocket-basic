// <copyright file="SignalBusTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Core.Signaling;

using Core;

/// <summary>
/// Unit tests for the <see cref="SignalBus"/> class.
/// </summary>
[TestFixture]
public class SignalBusTests
{
    /// <summary>
    /// Verifies that a new SignalBus has no signals asserted.
    /// </summary>
    [Test]
    public void SignalBus_NewInstance_NoSignalsAsserted()
    {
        var bus = new SignalBus();

        Assert.Multiple(() =>
        {
            Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.False);
            Assert.That(bus.IsAsserted(SignalLine.NMI), Is.False);
            Assert.That(bus.IsAsserted(SignalLine.RDY), Is.False);
            Assert.That(bus.IsAsserted(SignalLine.DmaReq), Is.False);
        });
    }

    /// <summary>
    /// Verifies that Assert sets IRQ to asserted.
    /// </summary>
    [Test]
    public void SignalBus_Assert_SetsIrqAsserted()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: Cycle.Zero);

        Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.True);
    }

    /// <summary>
    /// Verifies that Deassert deasserts IRQ.
    /// </summary>
    [Test]
    public void SignalBus_Deassert_DeassertsIrq()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: Cycle.Zero);

        bus.Deassert(SignalLine.IRQ, deviceId: 1, cycle: new(10));

        Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.False);
    }

    /// <summary>
    /// Verifies that multiple devices can assert IRQ.
    /// </summary>
    [Test]
    public void SignalBus_MultipleDevices_CanAssertIrq()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: Cycle.Zero);
        bus.Assert(SignalLine.IRQ, deviceId: 2, cycle: Cycle.Zero);

        Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.True);
    }

    /// <summary>
    /// Verifies that IRQ remains asserted until all devices deassert.
    /// </summary>
    [Test]
    public void SignalBus_IrqRemainsAsserted_UntilAllDevicesDeassert()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: Cycle.Zero);
        bus.Assert(SignalLine.IRQ, deviceId: 2, cycle: Cycle.Zero);

        bus.Deassert(SignalLine.IRQ, deviceId: 1, cycle: new(10));

        Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.True, "IRQ should remain asserted while device 2 holds it");

        bus.Deassert(SignalLine.IRQ, deviceId: 2, cycle: new(20));

        Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.False, "IRQ should be clear when all devices release");
    }

    /// <summary>
    /// Verifies that Assert sets NMI to asserted.
    /// </summary>
    [Test]
    public void SignalBus_Assert_SetsNmiAsserted()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.NMI, deviceId: 1, cycle: Cycle.Zero);

        Assert.That(bus.IsAsserted(SignalLine.NMI), Is.True);
    }

    /// <summary>
    /// Verifies that NMI edge is detected on transition from deasserted to asserted.
    /// </summary>
    [Test]
    public void SignalBus_NmiEdge_DetectedOnRisingEdge()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.NMI, deviceId: 1, cycle: Cycle.Zero);

        Assert.That(bus.ConsumeNmiEdge(), Is.True, "NMI edge should be pending after assertion");
    }

    /// <summary>
    /// Verifies that ConsumeNmiEdge clears the edge-detected flag.
    /// </summary>
    [Test]
    public void SignalBus_ConsumeNmiEdge_ClearsEdgeFlag()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.NMI, deviceId: 1, cycle: Cycle.Zero);

        // Consume the edge
        bool firstCall = bus.ConsumeNmiEdge();
        bool secondCall = bus.ConsumeNmiEdge();

        Assert.Multiple(() =>
        {
            Assert.That(firstCall, Is.True, "First call should return true");
            Assert.That(secondCall, Is.False, "Second call should return false (edge consumed)");
        });
    }

    /// <summary>
    /// Verifies that NMI edge remains pending after deassert until consumed.
    /// </summary>
    [Test]
    public void SignalBus_NmiEdge_RemainsPendingAfterDeassert()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.NMI, deviceId: 1, cycle: Cycle.Zero);
        bus.Deassert(SignalLine.NMI, deviceId: 1, cycle: new(10));

        Assert.That(bus.ConsumeNmiEdge(), Is.True, "NMI edge should remain until consumed");
    }

    /// <summary>
    /// Verifies that Assert sets RDY to asserted.
    /// </summary>
    [Test]
    public void SignalBus_Assert_SetsRdyAsserted()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.RDY, deviceId: 1, cycle: Cycle.Zero);

        Assert.That(bus.IsAsserted(SignalLine.RDY), Is.True);
    }

    /// <summary>
    /// Verifies that Assert sets DmaReq to asserted.
    /// </summary>
    [Test]
    public void SignalBus_Assert_SetsDmaRequested()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.DmaReq, deviceId: 1, cycle: Cycle.Zero);

        Assert.That(bus.IsAsserted(SignalLine.DmaReq), Is.True);
    }

    /// <summary>
    /// Verifies that IsAsserted returns true when line is asserted.
    /// </summary>
    [Test]
    public void SignalBus_IsAsserted_ReturnsTrueWhenAsserted()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: Cycle.Zero);

        Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.True);
    }

    /// <summary>
    /// Verifies that IsAsserted returns false when line is not asserted.
    /// </summary>
    [Test]
    public void SignalBus_IsAsserted_ReturnsFalseWhenNotAsserted()
    {
        var bus = new SignalBus();

        Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.False);
    }

    /// <summary>
    /// Verifies that Reset clears all signals.
    /// </summary>
    [Test]
    public void SignalBus_Reset_ClearsAllSignals()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: Cycle.Zero);
        bus.Assert(SignalLine.NMI, deviceId: 2, cycle: Cycle.Zero);
        bus.Assert(SignalLine.RDY, deviceId: 3, cycle: Cycle.Zero);
        bus.Assert(SignalLine.DmaReq, deviceId: 4, cycle: Cycle.Zero);

        bus.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.False);
            Assert.That(bus.IsAsserted(SignalLine.NMI), Is.False);
            Assert.That(bus.IsAsserted(SignalLine.RDY), Is.False);
            Assert.That(bus.IsAsserted(SignalLine.DmaReq), Is.False);
        });
    }

    /// <summary>
    /// Verifies that Reset clears NMI edge-detected flag.
    /// </summary>
    [Test]
    public void SignalBus_Reset_ClearsNmiEdgeFlag()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.NMI, deviceId: 1, cycle: Cycle.Zero);
        bus.Deassert(SignalLine.NMI, deviceId: 1, cycle: new(10));

        bus.Reset();

        Assert.That(bus.ConsumeNmiEdge(), Is.False, "NMI edge should be cleared by Reset");
    }

    /// <summary>
    /// Verifies that Deassert for non-asserting device has no effect.
    /// </summary>
    [Test]
    public void SignalBus_Deassert_ForNonAssertingDevice_NoEffect()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: Cycle.Zero);

        // Device 2 never asserted, so deassert should have no effect
        bus.Deassert(SignalLine.IRQ, deviceId: 2, cycle: new(10));

        Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.True, "IRQ should remain asserted");
    }

    /// <summary>
    /// Verifies that asserting Reset signal works correctly.
    /// </summary>
    [Test]
    public void SignalBus_Assert_ResetSignal()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.Reset, deviceId: 1, cycle: Cycle.Zero);

        Assert.That(bus.IsAsserted(SignalLine.Reset), Is.True);
    }

    /// <summary>
    /// Verifies that same device asserting twice only counts once.
    /// </summary>
    [Test]
    public void SignalBus_SameDeviceAssertingTwice_CountsOnce()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: Cycle.Zero);
        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: new(5));

        Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.True);

        // Single deassert should deassert
        bus.Deassert(SignalLine.IRQ, deviceId: 1, cycle: new(10));

        Assert.That(bus.IsAsserted(SignalLine.IRQ), Is.False);
    }

    /// <summary>
    /// Verifies that SignalChanged event fires on state transition.
    /// </summary>
    [Test]
    public void SignalBus_SignalChanged_FiresOnTransition()
    {
        var bus = new SignalBus();
        var events = new List<(SignalLine Line, bool Asserted, int DeviceId, Core.Cycle Cycle)>();
        bus.SignalChanged += (line, asserted, deviceId, cycle) =>
            events.Add((line, asserted, deviceId, cycle));

        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: new(100));

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(events[0].Line, Is.EqualTo(SignalLine.IRQ));
            Assert.That(events[0].Asserted, Is.True);
            Assert.That(events[0].DeviceId, Is.EqualTo(1));
            Assert.That(events[0].Cycle, Is.EqualTo(new Core.Cycle(100)));
        });
    }

    /// <summary>
    /// Verifies that SignalChanged event does not fire when state doesn't change.
    /// </summary>
    [Test]
    public void SignalBus_SignalChanged_DoesNotFireWhenNoStateChange()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: Cycle.Zero);

        var events = new List<(SignalLine Line, bool Asserted, int DeviceId, Core.Cycle Cycle)>();
        bus.SignalChanged += (line, asserted, deviceId, cycle) =>
            events.Add((line, asserted, deviceId, cycle));

        // Second device asserts - line already asserted, no state change
        bus.Assert(SignalLine.IRQ, deviceId: 2, cycle: new(10));

        Assert.That(events, Is.Empty, "Event should not fire when state doesn't change");
    }

    /// <summary>
    /// Verifies that SignalChanged event fires on deassert.
    /// </summary>
    [Test]
    public void SignalBus_SignalChanged_FiresOnDeassert()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.IRQ, deviceId: 1, cycle: Cycle.Zero);

        var events = new List<(SignalLine Line, bool Asserted, int DeviceId, Core.Cycle Cycle)>();
        bus.SignalChanged += (line, asserted, deviceId, cycle) =>
            events.Add((line, asserted, deviceId, cycle));

        bus.Deassert(SignalLine.IRQ, deviceId: 1, cycle: new(50));

        Assert.That(events, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(events[0].Line, Is.EqualTo(SignalLine.IRQ));
            Assert.That(events[0].Asserted, Is.False);
            Assert.That(events[0].DeviceId, Is.EqualTo(1));
            Assert.That(events[0].Cycle, Is.EqualTo(new Cycle(50)));
        });
    }

    /// <summary>
    /// Verifies that NMI edge is not detected on re-assertion without deassert.
    /// </summary>
    [Test]
    public void SignalBus_NmiEdge_NotDetectedOnReassertWithoutDeassert()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.NMI, deviceId: 1, cycle: Cycle.Zero);

        // Consume the first edge
        bus.ConsumeNmiEdge();

        // Assert again from another device while still asserted
        bus.Assert(SignalLine.NMI, deviceId: 2, cycle: new(10));

        Assert.That(bus.ConsumeNmiEdge(), Is.False, "No new edge should be detected when line was already asserted");
    }

    /// <summary>
    /// Verifies that new NMI edge is detected after full deassert/assert cycle.
    /// </summary>
    [Test]
    public void SignalBus_NmiEdge_DetectedAfterFullCycle()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.NMI, deviceId: 1, cycle: Cycle.Zero);
        bus.ConsumeNmiEdge();
        bus.Deassert(SignalLine.NMI, deviceId: 1, cycle: new(10));

        // Now assert again - should detect new edge
        bus.Assert(SignalLine.NMI, deviceId: 1, cycle: new(20));

        Assert.That(bus.ConsumeNmiEdge(), Is.True, "New edge should be detected after full deassert/assert cycle");
    }
}