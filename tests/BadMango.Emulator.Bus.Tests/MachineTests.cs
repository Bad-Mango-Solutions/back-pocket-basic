// <copyright file="MachineTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Core.Signaling;

using Moq;

/// <summary>
/// Unit tests for <see cref="Machine"/>.
/// </summary>
[TestFixture]
public class MachineTests
{
    /// <summary>
    /// Verifies that initial state is Stopped.
    /// </summary>
    [Test]
    public void InitialState_IsStopped()
    {
        var machine = CreateTestMachine();

        Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));
    }

    /// <summary>
    /// Verifies that Reset transitions to Stopped state.
    /// </summary>
    [Test]
    public void Reset_TransitionsToStopped()
    {
        var machine = CreateTestMachine();
        MachineState? reportedState = null;
        machine.StateChanged += s => reportedState = s;

        machine.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));
        });
    }

    /// <summary>
    /// Verifies that Reset calls CPU reset.
    /// </summary>
    [Test]
    public void Reset_CallsCpuReset()
    {
        var mockCpu = CreateMockCpu();
        var machine = CreateTestMachine(mockCpu);

        machine.Reset();

        mockCpu.Verify(c => c.Reset(), Times.Once);
    }

    /// <summary>
    /// Verifies that Stop transitions from Running to Stopped.
    /// </summary>
    [Test]
    public void Stop_FromRunning_TransitionsToStopped()
    {
        var mockCpu = CreateMockCpu();

        // Make Step return Stopped after first call to exit Run loop
        mockCpu.SetupSequence(c => c.Step())
            .Returns(new CpuStepResult(CpuRunState.Stopped, 1));

        var machine = CreateTestMachine(mockCpu);
        machine.Run(); // Will exit immediately because of Stopped result

        MachineState? reportedState = null;
        machine.StateChanged += s => reportedState = s;

        // Now test Stop
        machine.Stop();

        Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));
    }

    /// <summary>
    /// Verifies that Stop when already stopped has no effect.
    /// </summary>
    [Test]
    public void Stop_WhenAlreadyStopped_HasNoEffect()
    {
        var machine = CreateTestMachine();
        var stateChangeCount = 0;
        machine.StateChanged += _ => stateChangeCount++;

        machine.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));
            Assert.That(stateChangeCount, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that Step returns CPU step result.
    /// </summary>
    [Test]
    public void Step_ReturnsCpuStepResult()
    {
        var mockCpu = CreateMockCpu();
        mockCpu.Setup(c => c.Step()).Returns(new CpuStepResult(CpuRunState.Running, 5));

        var machine = CreateTestMachine(mockCpu);

        var result = machine.Step();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(CpuRunState.Running));
            Assert.That(result.CyclesConsumed.Value, Is.EqualTo(5));
        });
    }

    /// <summary>
    /// Verifies that Step advances the scheduler.
    /// </summary>
    [Test]
    public void Step_AdvancesScheduler()
    {
        var mockCpu = CreateMockCpu();
        mockCpu.Setup(c => c.Step()).Returns(new CpuStepResult(CpuRunState.Running, 10));

        var machine = CreateTestMachine(mockCpu);

        machine.Step();

        Assert.That(machine.Now.Value, Is.EqualTo(10));
    }

    /// <summary>
    /// Verifies that Step transitions to Paused.
    /// </summary>
    [Test]
    public void Step_TransitionsToPaused()
    {
        var mockCpu = CreateMockCpu();
        var machine = CreateTestMachine(mockCpu);

        machine.Step();

        Assert.That(machine.State, Is.EqualTo(MachineState.Paused));
    }

    /// <summary>
    /// Verifies that StateChanged event is raised on state transition.
    /// </summary>
    [Test]
    public void StateChanged_RaisedOnTransition()
    {
        var mockCpu = CreateMockCpu();
        var machine = CreateTestMachine(mockCpu);

        var states = new List<MachineState>();
        machine.StateChanged += s => states.Add(s);

        machine.Step();

        Assert.That(states, Contains.Item(MachineState.Paused));
    }

    /// <summary>
    /// Verifies that GetComponent returns null when not found.
    /// </summary>
    [Test]
    public void GetComponent_WhenNotFound_ReturnsNull()
    {
        var machine = CreateTestMachine();

        var result = machine.GetComponent<string>();

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies that GetComponents returns empty when none found.
    /// </summary>
    [Test]
    public void GetComponents_WhenNoneFound_ReturnsEmpty()
    {
        var machine = CreateTestMachine();

        var result = machine.GetComponents<string>();

        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Verifies that HasComponent returns false when not found.
    /// </summary>
    [Test]
    public void HasComponent_WhenNotFound_ReturnsFalse()
    {
        var machine = CreateTestMachine();

        var result = machine.HasComponent<string>();

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies machine provides access to core services.
    /// </summary>
    [Test]
    public void Machine_ProvidesAccessToCoreServices()
    {
        var machine = CreateTestMachine();

        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu, Is.Not.Null);
            Assert.That(machine.Bus, Is.Not.Null);
            Assert.That(machine.Scheduler, Is.Not.Null);
            Assert.That(machine.Signals, Is.Not.Null);
            Assert.That(machine.Devices, Is.Not.Null);
        });
    }

    /// <summary>
    /// Verifies Now reflects scheduler time.
    /// </summary>
    [Test]
    public void Now_ReflectsSchedulerTime()
    {
        var machine = CreateTestMachine();

        Assert.That(machine.Now.Value, Is.EqualTo(0));

        machine.Scheduler.Advance(100);

        Assert.That(machine.Now.Value, Is.EqualTo(100));
    }

    /// <summary>
    /// Verifies that Run stops when CPU returns Halted state.
    /// </summary>
    [Test]
    public void Run_WhenCpuHalts_TransitionsToStopped()
    {
        var mockCpu = CreateMockCpu();

        // Make Step return Running for a few cycles, then Halted
        mockCpu.SetupSequence(c => c.Step())
            .Returns(new CpuStepResult(CpuRunState.Running, 1))
            .Returns(new CpuStepResult(CpuRunState.Running, 1))
            .Returns(new CpuStepResult(CpuRunState.Halted, 1));

        var machine = CreateTestMachine(mockCpu);

        var states = new List<MachineState>();
        machine.StateChanged += s => states.Add(s);

        machine.Run();

        Assert.Multiple(() =>
        {
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));
            Assert.That(states, Contains.Item(MachineState.Running));
            Assert.That(states, Contains.Item(MachineState.Stopped));
        });

        // Verify Step was called 3 times
        mockCpu.Verify(c => c.Step(), Times.Exactly(3));
    }

    private static Machine CreateTestMachine(Mock<ICpu>? mockCpu = null)
    {
        mockCpu ??= CreateMockCpu();
        var bus = new MainBus(16);
        var scheduler = new Scheduler();
        var signals = new SignalBus();
        var devices = new DeviceRegistry();

        var machine = new Machine(mockCpu.Object, bus, scheduler, signals, devices);
        scheduler.SetEventContext(machine);

        return machine;
    }

    private static Mock<ICpu> CreateMockCpu()
    {
        var mockCpu = new Mock<ICpu>();
        mockCpu.Setup(c => c.Halted).Returns(false);
        mockCpu.Setup(c => c.IsStopRequested).Returns(false);
        mockCpu.Setup(c => c.Step()).Returns(new CpuStepResult(CpuRunState.Running, 1));
        return mockCpu;
    }
}