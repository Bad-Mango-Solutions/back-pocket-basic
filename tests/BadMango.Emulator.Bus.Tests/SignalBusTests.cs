// <copyright file="SignalBusTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

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
            Assert.That(bus.IsIrqAsserted, Is.False);
            Assert.That(bus.IsNmiAsserted, Is.False);
            Assert.That(bus.IsWaiting, Is.False);
            Assert.That(bus.IsDmaRequested, Is.False);
        });
    }

    /// <summary>
    /// Verifies that Assert sets IsIrqAsserted to true.
    /// </summary>
    [Test]
    public void SignalBus_Assert_SetsIrqAsserted()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.Irq, deviceId: 1, cycle: 0);

        Assert.That(bus.IsIrqAsserted, Is.True);
    }

    /// <summary>
    /// Verifies that Clear deasserts IRQ.
    /// </summary>
    [Test]
    public void SignalBus_Clear_DeassertsIrq()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.Irq, deviceId: 1, cycle: 0);

        bus.Clear(SignalLine.Irq, deviceId: 1, cycle: 10);

        Assert.That(bus.IsIrqAsserted, Is.False);
    }

    /// <summary>
    /// Verifies that multiple devices can assert IRQ.
    /// </summary>
    [Test]
    public void SignalBus_MultipleDevices_CanAssertIrq()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.Irq, deviceId: 1, cycle: 0);
        bus.Assert(SignalLine.Irq, deviceId: 2, cycle: 0);

        Assert.That(bus.IsIrqAsserted, Is.True);
    }

    /// <summary>
    /// Verifies that IRQ remains asserted until all devices clear.
    /// </summary>
    [Test]
    public void SignalBus_IrqRemainsAsserted_UntilAllDevicesClear()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.Irq, deviceId: 1, cycle: 0);
        bus.Assert(SignalLine.Irq, deviceId: 2, cycle: 0);

        bus.Clear(SignalLine.Irq, deviceId: 1, cycle: 10);

        Assert.That(bus.IsIrqAsserted, Is.True, "IRQ should remain asserted while device 2 holds it");

        bus.Clear(SignalLine.Irq, deviceId: 2, cycle: 20);

        Assert.That(bus.IsIrqAsserted, Is.False, "IRQ should be clear when all devices release");
    }

    /// <summary>
    /// Verifies that Assert sets IsNmiAsserted to true.
    /// </summary>
    [Test]
    public void SignalBus_Assert_SetsNmiAsserted()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.Nmi, deviceId: 1, cycle: 0);

        Assert.That(bus.IsNmiAsserted, Is.True);
    }

    /// <summary>
    /// Verifies that NMI edge is detected on transition from clear to asserted.
    /// </summary>
    [Test]
    public void SignalBus_NmiEdge_DetectedOnRisingEdge()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.Nmi, deviceId: 1, cycle: 0);

        Assert.That(bus.IsNmiAsserted, Is.True, "NMI should be asserted after edge");

        bus.Clear(SignalLine.Nmi, deviceId: 1, cycle: 10);

        // NMI edge should still be detected until acknowledged
        Assert.That(bus.IsNmiAsserted, Is.True, "NMI edge should remain until acknowledged");
    }

    /// <summary>
    /// Verifies that AcknowledgeNmi clears the edge-detected flag.
    /// </summary>
    [Test]
    public void SignalBus_AcknowledgeNmi_ClearsEdgeFlag()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.Nmi, deviceId: 1, cycle: 0);
        bus.Clear(SignalLine.Nmi, deviceId: 1, cycle: 10);

        bus.AcknowledgeNmi(cycle: 20);

        Assert.That(bus.IsNmiAsserted, Is.False, "NMI should be clear after acknowledgment");
    }

    /// <summary>
    /// Verifies that Assert sets IsWaiting to true for RDY.
    /// </summary>
    [Test]
    public void SignalBus_Assert_SetsWaiting()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.Rdy, deviceId: 1, cycle: 0);

        Assert.That(bus.IsWaiting, Is.True);
    }

    /// <summary>
    /// Verifies that Assert sets IsDmaRequested to true for DmaReq.
    /// </summary>
    [Test]
    public void SignalBus_Assert_SetsDmaRequested()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.DmaReq, deviceId: 1, cycle: 0);

        Assert.That(bus.IsDmaRequested, Is.True);
    }

    /// <summary>
    /// Verifies that Sample returns Asserted when line is asserted.
    /// </summary>
    [Test]
    public void SignalBus_Sample_ReturnsAssertedWhenAsserted()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.Irq, deviceId: 1, cycle: 0);

        var state = bus.Sample(SignalLine.Irq);

        Assert.That(state, Is.EqualTo(SignalState.Asserted));
    }

    /// <summary>
    /// Verifies that Sample returns Clear when line is not asserted.
    /// </summary>
    [Test]
    public void SignalBus_Sample_ReturnsClearWhenNotAsserted()
    {
        var bus = new SignalBus();

        var state = bus.Sample(SignalLine.Irq);

        Assert.That(state, Is.EqualTo(SignalState.Clear));
    }

    /// <summary>
    /// Verifies that Sample for NMI includes edge-detected state.
    /// </summary>
    [Test]
    public void SignalBus_SampleNmi_IncludesEdgeState()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.Nmi, deviceId: 1, cycle: 0);
        bus.Clear(SignalLine.Nmi, deviceId: 1, cycle: 10);

        // Edge should still be detected
        var state = bus.Sample(SignalLine.Nmi);

        Assert.That(state, Is.EqualTo(SignalState.Asserted), "Sample should return Asserted while edge is pending");
    }

    /// <summary>
    /// Verifies that Reset clears all signals.
    /// </summary>
    [Test]
    public void SignalBus_Reset_ClearsAllSignals()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.Irq, deviceId: 1, cycle: 0);
        bus.Assert(SignalLine.Nmi, deviceId: 2, cycle: 0);
        bus.Assert(SignalLine.Rdy, deviceId: 3, cycle: 0);
        bus.Assert(SignalLine.DmaReq, deviceId: 4, cycle: 0);

        bus.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(bus.IsIrqAsserted, Is.False);
            Assert.That(bus.IsNmiAsserted, Is.False);
            Assert.That(bus.IsWaiting, Is.False);
            Assert.That(bus.IsDmaRequested, Is.False);
        });
    }

    /// <summary>
    /// Verifies that Reset clears NMI edge-detected flag.
    /// </summary>
    [Test]
    public void SignalBus_Reset_ClearsNmiEdgeFlag()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.Nmi, deviceId: 1, cycle: 0);
        bus.Clear(SignalLine.Nmi, deviceId: 1, cycle: 10);

        bus.Reset();

        Assert.That(bus.IsNmiAsserted, Is.False, "NMI edge should be cleared by Reset");
    }

    /// <summary>
    /// Verifies that Clear for non-asserting device has no effect.
    /// </summary>
    [Test]
    public void SignalBus_Clear_ForNonAssertingDevice_NoEffect()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.Irq, deviceId: 1, cycle: 0);

        // Device 2 never asserted, so clear should have no effect
        bus.Clear(SignalLine.Irq, deviceId: 2, cycle: 10);

        Assert.That(bus.IsIrqAsserted, Is.True, "IRQ should remain asserted");
    }

    /// <summary>
    /// Verifies that asserting Reset signal works correctly.
    /// </summary>
    [Test]
    public void SignalBus_Assert_ResetSignal()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.Reset, deviceId: 1, cycle: 0);

        Assert.That(bus.Sample(SignalLine.Reset), Is.EqualTo(SignalState.Asserted));
    }

    /// <summary>
    /// Verifies that asserting BusEnable signal works correctly.
    /// </summary>
    [Test]
    public void SignalBus_Assert_BusEnableSignal()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.BusEnable, deviceId: 1, cycle: 0);

        Assert.That(bus.Sample(SignalLine.BusEnable), Is.EqualTo(SignalState.Asserted));
    }

    /// <summary>
    /// Verifies that same device asserting twice only counts once.
    /// </summary>
    [Test]
    public void SignalBus_SameDeviceAssertingTwice_CountsOnce()
    {
        var bus = new SignalBus();

        bus.Assert(SignalLine.Irq, deviceId: 1, cycle: 0);
        bus.Assert(SignalLine.Irq, deviceId: 1, cycle: 5);

        Assert.That(bus.IsIrqAsserted, Is.True);

        // Single clear should deassert
        bus.Clear(SignalLine.Irq, deviceId: 1, cycle: 10);

        Assert.That(bus.IsIrqAsserted, Is.False);
    }

    /// <summary>
    /// Verifies that TotalFetchCycles starts at zero.
    /// </summary>
    [Test]
    public void SignalBus_NewInstance_TotalFetchCyclesIsZero()
    {
        var bus = new SignalBus();

        Assert.That(bus.TotalFetchCycles, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies that TotalExecuteCycles starts at zero.
    /// </summary>
    [Test]
    public void SignalBus_NewInstance_TotalExecuteCyclesIsZero()
    {
        var bus = new SignalBus();

        Assert.That(bus.TotalExecuteCycles, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies that TotalCpuCycles starts at zero.
    /// </summary>
    [Test]
    public void SignalBus_NewInstance_TotalCpuCyclesIsZero()
    {
        var bus = new SignalBus();

        Assert.That(bus.TotalCpuCycles, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies that SignalInstructionFetched accumulates cycles.
    /// </summary>
    [Test]
    public void SignalBus_SignalInstructionFetched_AccumulatesCycles()
    {
        var bus = new SignalBus();

        bus.SignalInstructionFetched(3);
        Assert.That(bus.TotalFetchCycles, Is.EqualTo(3ul));

        bus.SignalInstructionFetched(2);
        Assert.That(bus.TotalFetchCycles, Is.EqualTo(5ul));
    }

    /// <summary>
    /// Verifies that SignalInstructionExecuted accumulates cycles.
    /// </summary>
    [Test]
    public void SignalBus_SignalInstructionExecuted_AccumulatesCycles()
    {
        var bus = new SignalBus();

        bus.SignalInstructionExecuted(4);
        Assert.That(bus.TotalExecuteCycles, Is.EqualTo(4ul));

        bus.SignalInstructionExecuted(2);
        Assert.That(bus.TotalExecuteCycles, Is.EqualTo(6ul));
    }

    /// <summary>
    /// Verifies that TotalCpuCycles is the sum of fetch and execute cycles.
    /// </summary>
    [Test]
    public void SignalBus_TotalCpuCycles_IsSumOfFetchAndExecute()
    {
        var bus = new SignalBus();

        bus.SignalInstructionFetched(3);
        bus.SignalInstructionExecuted(4);

        Assert.Multiple(() =>
        {
            Assert.That(bus.TotalFetchCycles, Is.EqualTo(3ul));
            Assert.That(bus.TotalExecuteCycles, Is.EqualTo(4ul));
            Assert.That(bus.TotalCpuCycles, Is.EqualTo(7ul));
        });
    }

    /// <summary>
    /// Verifies that Reset clears CPU cycle counters.
    /// </summary>
    [Test]
    public void SignalBus_Reset_ClearsCycleCounters()
    {
        var bus = new SignalBus();
        bus.SignalInstructionFetched(10);
        bus.SignalInstructionExecuted(20);

        bus.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(bus.TotalFetchCycles, Is.EqualTo(0ul));
            Assert.That(bus.TotalExecuteCycles, Is.EqualTo(0ul));
            Assert.That(bus.TotalCpuCycles, Is.EqualTo(0ul));
        });
    }

    /// <summary>
    /// Verifies that ResetCycleCounters clears counters without affecting signals.
    /// </summary>
    [Test]
    public void SignalBus_ResetCycleCounters_ClearsCountersOnly()
    {
        var bus = new SignalBus();
        bus.Assert(SignalLine.Irq, deviceId: 1, cycle: 0);
        bus.SignalInstructionFetched(10);
        bus.SignalInstructionExecuted(20);

        bus.ResetCycleCounters();

        Assert.Multiple(() =>
        {
            Assert.That(bus.TotalFetchCycles, Is.EqualTo(0ul));
            Assert.That(bus.TotalExecuteCycles, Is.EqualTo(0ul));
            Assert.That(bus.TotalCpuCycles, Is.EqualTo(0ul));
            Assert.That(bus.IsIrqAsserted, Is.True, "IRQ should still be asserted");
        });
    }

    /// <summary>
    /// Verifies cycle counting works with many signals.
    /// </summary>
    [Test]
    public void SignalBus_CycleCounters_WorkWithManyCycles()
    {
        var bus = new SignalBus();

        // Simulate 1000 instructions worth of cycles
        for (int i = 0; i < 1000; i++)
        {
            bus.SignalInstructionFetched(2);
            bus.SignalInstructionExecuted(4);
        }

        Assert.Multiple(() =>
        {
            Assert.That(bus.TotalFetchCycles, Is.EqualTo(2000ul));
            Assert.That(bus.TotalExecuteCycles, Is.EqualTo(4000ul));
            Assert.That(bus.TotalCpuCycles, Is.EqualTo(6000ul));
        });
    }
}