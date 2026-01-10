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

    /// <summary>
    /// Verifies that Run handles WaitingForInterrupt state by using scheduler.
    /// </summary>
    [Test]
    public void Run_WhenCpuWaitingForInterrupt_UsesSchedulerToAdvanceTime()
    {
        var mockCpu = CreateMockCpu();
        var scheduler = new Scheduler();
        var bus = new MainBus(16);
        var signals = new SignalBus();
        var devices = new DeviceRegistry();

        var machine = new Machine(mockCpu.Object, bus, scheduler, signals, devices);
        scheduler.SetEventContext(machine);

        bool eventFired = false;

        // Schedule an event for cycle 100
        scheduler.ScheduleAt((BadMango.Emulator.Core.Cycle)100UL, ScheduledEventKind.DeviceTimer, 0, _ =>
        {
            eventFired = true;

            // After event fires, make CPU stop waiting
            mockCpu.Setup(c => c.Step()).Returns(new CpuStepResult(CpuRunState.Stopped, 1));
        });

        // Make CPU return WaitingForInterrupt first, then Stopped after event fires
        mockCpu.SetupSequence(c => c.Step())
            .Returns(new CpuStepResult(CpuRunState.WaitingForInterrupt, 0))
            .Returns(new CpuStepResult(CpuRunState.Stopped, 1));

        // Act
        machine.Run();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(eventFired, Is.True, "Scheduled event should have fired");
            Assert.That(scheduler.Now.Value, Is.GreaterThanOrEqualTo(100), "Scheduler should have advanced to event time");
        });
    }

    /// <summary>
    /// Verifies that Pause transitions state to Paused when Running.
    /// </summary>
    [Test]
    public void Pause_WhenRunning_SetsStateToPaused()
    {
        var mockCpu = CreateMockCpu();
        var machine = CreateTestMachine(mockCpu);

        // Setup the mock to simulate being in a run loop that checks IsStopRequested
        bool stopRequested = false;
        mockCpu.Setup(c => c.IsStopRequested).Returns(() => stopRequested);
        mockCpu.Setup(c => c.RequestStop()).Callback(() => stopRequested = true);
        mockCpu.Setup(c => c.Step()).Returns(() =>
            stopRequested
                ? new CpuStepResult(CpuRunState.Stopped, 1)
                : new CpuStepResult(CpuRunState.Running, 1));

        // Start running synchronously - this will block until stopped
        var runTask = Task.Run(() => machine.Run());

        // Give it time to start
        Thread.Sleep(50);

        // Verify it's running
        Assert.That(machine.State, Is.EqualTo(MachineState.Running));

        // Pause
        machine.Pause();

        // Wait for task to complete
        runTask.Wait(1000);

        // Should have been paused
        Assert.That(machine.State, Is.EqualTo(MachineState.Paused));
    }

    /// <summary>
    /// Verifies that Pause when not running has no effect.
    /// </summary>
    [Test]
    public void Pause_WhenNotRunning_HasNoEffect()
    {
        var machine = CreateTestMachine();

        // Initially stopped
        Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));

        // Pause should have no effect
        machine.Pause();

        Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));
    }

    /// <summary>
    /// Verifies that Halt transitions to Stopped and sets HaltReason.
    /// </summary>
    [Test]
    public void Halt_TransitionsToStoppedAndSetsHaltReason()
    {
        var mockCpu = CreateMockCpu();
        HaltState capturedHaltReason = HaltState.None;
        mockCpu.SetupSet(c => c.HaltReason = It.IsAny<HaltState>())
            .Callback<HaltState>(hr => capturedHaltReason = hr);

        var machine = CreateTestMachine(mockCpu);

        // Act
        machine.Halt();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));
            Assert.That(capturedHaltReason, Is.EqualTo(HaltState.Stp));
        });
    }

    /// <summary>
    /// Verifies that BootAsync calls Reset then RunAsync.
    /// </summary>
    [Test]
    public async Task BootAsync_CallsResetThenRun()
    {
        var mockCpu = CreateMockCpu();
        bool resetCalled = false;
        mockCpu.Setup(c => c.Reset()).Callback(() => resetCalled = true);
        mockCpu.Setup(c => c.Step()).Returns(new CpuStepResult(CpuRunState.Stopped, 1));

        var machine = CreateTestMachine(mockCpu);

        // Act
        await machine.BootAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(resetCalled, Is.True, "Reset should be called");
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped), "Should be stopped after boot completes");
        });
    }

    /// <summary>
    /// Verifies that RunAsync can be cancelled.
    /// </summary>
    [Test]
    public async Task RunAsync_CanBeCancelled()
    {
        var mockCpu = CreateMockCpu();
        mockCpu.Setup(c => c.Step()).Returns(new CpuStepResult(CpuRunState.Running, 1));

        var machine = CreateTestMachine(mockCpu);

        using var cts = new CancellationTokenSource();

        // Start running
        var runTask = machine.RunAsync(cts.Token);

        // Wait briefly to ensure it's running
        await Task.Delay(10);
        Assert.That(machine.State, Is.EqualTo(MachineState.Running));

        // Cancel
        cts.Cancel();

        // Wait for task to complete
        await runTask;

        // Should be paused after cancellation
        Assert.That(machine.State, Is.EqualTo(MachineState.Paused));
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