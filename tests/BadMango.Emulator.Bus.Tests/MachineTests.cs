// <copyright file="MachineTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
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
    /// Verifies that the first reset after construction is a power-on reset: the CPU is held
    /// for the hold window and then initialized to its power-on state when the line releases,
    /// instead of running the warm reset sequence.
    /// </summary>
    [Test]
    public void Reset_FirstReset_HoldsThenPerformsPowerOnCpuReset()
    {
        var mockCpu = CreateMockCpu();
        var machine = CreateTestMachine(mockCpu);

        machine.Reset();

        Assert.Multiple(() =>
        {
            mockCpu.Verify(c => c.Step(), Times.Exactly(2));
            mockCpu.Verify(c => c.Reset(), Times.Once);
            Assert.That(machine.Signals.IsAsserted(SignalLine.Reset), Is.False, "RESET must be released after the pulse");
        });
    }

    /// <summary>
    /// Verifies that a soft reset after power-on steps the hold window and the reset release
    /// sequence (three CPU steps) and does not re-initialize the CPU.
    /// </summary>
    [Test]
    public void Reset_SoftResetAfterPowerOn_StepsHoldWindowAndReleaseSequence()
    {
        var mockCpu = CreateMockCpu();
        var machine = CreateTestMachine(mockCpu);
        machine.Reset();
        mockCpu.Invocations.Clear();

        machine.Reset();

        Assert.Multiple(() =>
        {
            mockCpu.Verify(c => c.Step(), Times.Exactly(3));
            mockCpu.Verify(c => c.Reset(), Times.Never);
        });
    }

    /// <summary>
    /// Verifies that a hard reset is a power-on reset again: the CPU is re-initialized.
    /// </summary>
    [Test]
    public void Reset_HardResetAfterPowerOn_ReinitializesCpu()
    {
        var mockCpu = CreateMockCpu();
        var machine = CreateTestMachine(mockCpu);
        machine.Reset();
        machine.Reset();
        mockCpu.Invocations.Clear();

        machine.Reset(hardReset: true);

        Assert.Multiple(() =>
        {
            mockCpu.Verify(c => c.Step(), Times.Exactly(2));
            mockCpu.Verify(c => c.Reset(), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that hard reset performs peripheral power-on reset.
    /// </summary>
    [Test]
    public void Reset_HardReset_ResetsPeripheralDevices()
    {
        var mockCpu = CreateMockCpu();
        var mockPeripheral = new Mock<IPeripheral>();
        mockPeripheral.Setup(p => p.Name).Returns("TestDevice");

        var machine = CreateTestMachine(mockCpu);
        machine.AddScheduledDevice(mockPeripheral.Object);

        machine.Reset(hardReset: true);

        mockPeripheral.Verify(p => p.Reset(), Times.Once);
    }

    /// <summary>
    /// Verifies that Reset reschedules device events after clearing the scheduler.
    /// </summary>
    /// <remarks>
    /// This tests the critical scenario where <see cref="Scheduler.Reset"/> clears
    /// all events, and then device resets must reschedule them (e.g., VBLANK).
    /// </remarks>
    [Test]
    public void Reset_HardReset_DeviceEventsArePresentAfterReset()
    {
        var mockCpu = CreateMockCpu();
        var machine = CreateTestMachine(mockCpu);

        // Create a peripheral that schedules an event during Reset
        bool eventScheduled = false;
        var mockPeripheral = new Mock<IPeripheral>();
        mockPeripheral.Setup(p => p.Name).Returns("TestSchedulingDevice");
        mockPeripheral.Setup(p => p.Reset()).Callback(() =>
        {
            machine.Scheduler.ScheduleAfter(100, ScheduledEventKind.DeviceTimer, 0, _ => { }, "test");
            eventScheduled = true;
        });

        machine.AddScheduledDevice(mockPeripheral.Object);

        machine.Reset(hardReset: true);

        Assert.Multiple(() =>
        {
            Assert.That(eventScheduled, Is.True, "Device Reset should have been called");
            Assert.That(machine.Scheduler.PendingEventCount, Is.GreaterThan(0), "Device should have scheduled events after reset");
        });
    }

    /// <summary>
    /// Verifies that reset clears stale scheduler events and only post-reset events fire.
    /// </summary>
    [Test]
    public void Reset_FlushesPreResetSchedulerEvents()
    {
        var machine = CreateTestMachine();
        bool staleEventFired = false;
        bool resetEventFired = false;

        machine.Scheduler.ScheduleAfter(10, ScheduledEventKind.DeviceTimer, 0, _ => staleEventFired = true, "stale");

        var mockPeripheral = new Mock<IPeripheral>();
        mockPeripheral.Setup(p => p.Name).Returns("ReschedulingDevice");
        mockPeripheral.Setup(p => p.Reset()).Callback(() =>
        {
            machine.Scheduler.ScheduleAfter(5, ScheduledEventKind.DeviceTimer, 0, _ => resetEventFired = true, "fresh");
        });

        machine.AddScheduledDevice(mockPeripheral.Object);
        machine.Reset(hardReset: true);

        bool dispatched = machine.Scheduler.JumpToNextEventAndDispatch();

        Assert.Multiple(() =>
        {
            Assert.That(dispatched, Is.True, "A fresh post-reset event should be pending");
            Assert.That(resetEventFired, Is.True, "Post-reset event should fire");
            Assert.That(staleEventFired, Is.False, "Pre-reset event must be flushed");
        });
    }

    /// <summary>
    /// Verifies that a soft reset reaches slot cards through the slot manager: cards that
    /// implement <see cref="IResetReceiver"/> get the RESET signal (assert and release) and are
    /// not power-on reset, while other cards get their <see cref="IPeripheral.Reset"/> once.
    /// </summary>
    [Test]
    public void Reset_Soft_DeliversResetSignalToSlotCardsThroughSlotManager()
    {
        var (machine, receiverCard, plainCard) = CreateMachineWithSlotCards();
        machine.Reset(); // Power-on reset
        receiverCard.Invocations.Clear();
        plainCard.Invocations.Clear();

        machine.Reset();

        var receiver = receiverCard.As<IResetReceiver>();
        Assert.Multiple(() =>
        {
            receiver.Verify(r => r.OnResetAsserted(It.IsAny<Cycle>()), Times.Once);
            receiver.Verify(r => r.OnResetReleased(It.IsAny<Cycle>()), Times.Once);
            receiverCard.Verify(c => c.Reset(), Times.Never);
            plainCard.Verify(c => c.Reset(), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that a hard reset power-on resets every peripheral and slot card exactly once,
    /// on top of delivering the RESET signal, even when a card is also registered as a
    /// scheduled device.
    /// </summary>
    [Test]
    public void Reset_HardReset_PowerOnResetsDevicesAndSlotCardsExactlyOnce()
    {
        var (machine, receiverCard, plainCard) = CreateMachineWithSlotCards();

        var mockPeripheral = new Mock<IPeripheral>();
        mockPeripheral.Setup(p => p.Name).Returns("OrderDevice");
        machine.AddScheduledDevice(mockPeripheral.Object);
        machine.AddScheduledDevice(plainCard.Object);

        machine.Reset(); // Power-on reset
        machine.Reset();
        mockPeripheral.Invocations.Clear();
        receiverCard.Invocations.Clear();
        plainCard.Invocations.Clear();

        machine.Reset(hardReset: true);

        var receiver = receiverCard.As<IResetReceiver>();
        Assert.Multiple(() =>
        {
            mockPeripheral.Verify(p => p.Reset(), Times.Once);
            receiverCard.Verify(c => c.Reset(), Times.Once);
            receiver.Verify(r => r.OnResetAsserted(It.IsAny<Cycle>()), Times.Once);
            receiver.Verify(r => r.OnResetReleased(It.IsAny<Cycle>()), Times.Once);
            plainCard.Verify(c => c.Reset(), Times.Once);
        });
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
    /// Verifies that Step does not advance the scheduler directly,
    /// since the CPU is responsible for advancing the scheduler internally
    /// during <see cref="ICpu.Step()"/>.
    /// </summary>
    [Test]
    public void Step_DoesNotDoubleAdvanceScheduler()
    {
        var mockCpu = CreateMockCpu();
        var machine = CreateTestMachine(mockCpu);

        // Setup CPU to report 10 cycles consumed.
        // In production, Cpu65C02.Step() would have already advanced the scheduler
        // by 10 cycles internally. The mock doesn't do this, so the scheduler stays at 0.
        mockCpu.Setup(c => c.Step()).Returns(new CpuStepResult(CpuRunState.Running, 10));

        machine.Step();

        // Machine.Step() must NOT advance the scheduler itself — the scheduler should
        // remain at 0 (since the mock CPU doesn't advance it). If Machine.Step() were
        // to call Scheduler.Advance(10), the value would be 10, indicating a double-advance
        // bug with the real Cpu65C02.
        Assert.That(machine.Now.Value, Is.EqualTo(0));
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
    /// Verifies that stepping while running waits for the run loop to exit, so the CPU is
    /// never stepped by two threads at once.
    /// </summary>
    [Test]
    public void Step_WhenRunning_NeverStepsTheCpuConcurrentlyWithTheRunLoop()
    {
        var detector = new ConcurrencyDetector();
        var mockCpu = CreateMockCpu();
        int stopRequested = 0;
        int stepCalls = 0;

        mockCpu.Setup(c => c.IsStopRequested).Returns(() => Volatile.Read(ref stopRequested) != 0);
        mockCpu.Setup(c => c.RequestStop()).Callback(() => Volatile.Write(ref stopRequested, 1));
        mockCpu.Setup(c => c.ClearStopRequest()).Callback(() => Volatile.Write(ref stopRequested, 0));
        mockCpu.Setup(c => c.Step()).Returns(() =>
        {
            Interlocked.Increment(ref stepCalls);
            using (detector.Enter())
            {
                // Hold the CPU long enough that an unsynchronized step would overlap.
                Thread.SpinWait(20_000);
            }

            return new CpuStepResult(CpuRunState.Running, 1);
        });

        var machine = CreateTestMachine(mockCpu);

        for (int i = 0; i < 20; i++)
        {
            var runTask = Task.Run(() => machine.Run());
            Assert.That(SpinWait.SpinUntil(() => machine.State == MachineState.Running && Volatile.Read(ref stepCalls) > 0, 5000), Is.True);

            int stepsBefore = Volatile.Read(ref stepCalls);
            machine.Step();

            Assert.Multiple(() =>
            {
                Assert.That(runTask.Wait(5000), Is.True, "Run loop must have exited when Step returns");
                Assert.That(runTask.IsCompleted, Is.True);
                Assert.That(machine.State, Is.EqualTo(MachineState.Paused));
                Assert.That(Volatile.Read(ref stepCalls), Is.GreaterThan(stepsBefore), "The explicit step must run");
            });

            machine.Stop();
        }

        Assert.That(detector.Overlaps, Is.Zero, "The explicit Step overlapped a run-loop step");
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
    /// Verifies that resets requested from another thread while the machine runs are applied
    /// by the run loop: every reset is delivered (assert and release) on the machine thread,
    /// never concurrently with a CPU step, and the machine keeps running.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Reset_WhileRunning_IsAppliedOnTheRunLoopWithoutRacingTheCpu()
    {
        const int ResetCount = 25;
        var detector = new ConcurrencyDetector();
        Machine machine = null!;
        var mockCpu = CreateMockCpu();
        mockCpu.Setup(c => c.Step()).Returns(() =>
        {
            using (detector.Enter())
            {
                Thread.SpinWait(200);
                machine.Scheduler.Advance(1);
            }

            return new CpuStepResult(CpuRunState.Running, 1);
        });

        machine = CreateTestMachine(mockCpu);
        int testThread = Environment.CurrentManagedThreadId;
        int asserted = 0;
        int released = 0;
        int onTestThread = 0;
        var receiver = new Mock<IResetReceiver>();
        receiver.Setup(r => r.OnResetAsserted(It.IsAny<Cycle>())).Callback(() =>
        {
            using (detector.Enter())
            {
                Interlocked.Increment(ref asserted);
                if (Environment.CurrentManagedThreadId == testThread)
                {
                    Interlocked.Increment(ref onTestThread);
                }
            }
        });
        receiver.Setup(r => r.OnResetReleased(It.IsAny<Cycle>())).Callback(() =>
        {
            using (detector.Enter())
            {
                Interlocked.Increment(ref released);
            }
        });
        machine.AddComponent(receiver.Object);
        machine.Reset(); // Power-on reset while quiescent, on this thread
        asserted = released = onTestThread = 0;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var runTask = machine.RunAsync(cts.Token);
        Assert.That(SpinWait.SpinUntil(() => machine.State == MachineState.Running, 5000), Is.True);

        for (int i = 0; i < ResetCount; i++)
        {
            machine.Reset();
            Assert.That(machine.State, Is.EqualTo(MachineState.Running), "Reset must not change the run state");
            int expected = i + 1;
            Assert.That(SpinWait.SpinUntil(() => Volatile.Read(ref released) >= expected, 5000), Is.True, $"Reset {expected} was not released");
        }

        machine.Stop();
        await runTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(asserted, Is.EqualTo(ResetCount), "Every reset must assert the line once");
            Assert.That(released, Is.EqualTo(ResetCount), "Every reset must release the line once");
            Assert.That(onTestThread, Is.Zero, "RESET receivers must run on the machine thread, not the caller's");
            Assert.That(detector.Overlaps, Is.Zero, "A RESET receiver ran concurrently with a CPU step");
        });
    }

    /// <summary>
    /// Verifies that Stop and Step called from a scheduled callback inside the run loop return
    /// instead of waiting for the loop that is executing them, also after the throttled loop
    /// has resumed on another thread.
    /// </summary>
    /// <param name="useStep">Whether the callback calls Step (otherwise Stop).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [TestCase(false)]
    [TestCase(true)]
    public async Task StopOrStep_FromScheduledCallback_DoesNotDeadlock(bool useStep)
    {
        Machine machine = null!;
        var mockCpu = CreateMockCpu();
        int stopRequested = 0;
        mockCpu.Setup(c => c.IsStopRequested).Returns(() => Volatile.Read(ref stopRequested) != 0);
        mockCpu.Setup(c => c.RequestStop()).Callback(() => Volatile.Write(ref stopRequested, 1));
        mockCpu.Setup(c => c.ClearStopRequest()).Callback(() => Volatile.Write(ref stopRequested, 0));
        mockCpu.Setup(c => c.Step()).Returns(() =>
        {
            machine.Scheduler.Advance(10);
            return new CpuStepResult(CpuRunState.Running, 10);
        });

        machine = CreateTestMachine(mockCpu);

        // Throttle so the loop awaits between slices and may resume on a different thread.
        machine.ClockSpeedHz = 1_000_000;
        var threads = new HashSet<int>();
        bool calledFromLoop = false;
        Action<IEventContext>? tick = null;
        tick = _ =>
        {
            threads.Add(Environment.CurrentManagedThreadId);
            if (machine.Now.Value < 200_000 || calledFromLoop)
            {
                machine.Scheduler.ScheduleAfter(5_000, ScheduledEventKind.DeviceTimer, 0, tick!, "tick");
                return;
            }

            calledFromLoop = true;
            if (useStep)
            {
                machine.Step();
            }
            else
            {
                machine.Stop();
            }
        };
        machine.Scheduler.ScheduleAfter(5_000, ScheduledEventKind.DeviceTimer, 0, tick, "tick");

        var runTask = machine.RunAsync();
        var finished = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(10)));

        Assert.Multiple(() =>
        {
            Assert.That(finished, Is.SameAs(runTask), "Run loop deadlocked on a Stop/Step from its own callback");
            Assert.That(calledFromLoop, Is.True);
            Assert.That(machine.State, Is.EqualTo(useStep ? MachineState.Paused : MachineState.Stopped));
        });

        TestContext.Out.WriteLine($"Callback threads observed: {threads.Count}");
    }

    /// <summary>
    /// Verifies that Pause does not return while the run loop is still inside a CPU step.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Pause_WhileAStepIsInProgress_WaitsForTheRunLoopToExit()
    {
        using var cpu = new GatedCpu();
        var machine = CreateTestMachine(cpu.Mock);
        var runTask = Task.Run(() => machine.RunAsync());
        cpu.BlockNextStep();

        var pauseTask = Task.Run(machine.Pause);
        bool returnedWhileStepping = pauseTask.Wait(200);
        cpu.ReleaseStep();

        Assert.Multiple(() =>
        {
            Assert.That(returnedWhileStepping, Is.False, "Pause returned while the run loop was still executing");
            Assert.That(pauseTask.Wait(5000), Is.True);
            Assert.That(machine.State, Is.EqualTo(MachineState.Paused));
        });

        await runTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that Step does not step the CPU while the run loop is still inside a CPU step.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Step_WhileAStepIsInProgress_WaitsForTheRunLoopToExit()
    {
        using var cpu = new GatedCpu();
        var machine = CreateTestMachine(cpu.Mock);
        var runTask = Task.Run(() => machine.RunAsync());
        cpu.BlockNextStep();
        int stepsBefore = cpu.Steps;

        var stepTask = Task.Run(machine.Step);
        bool returnedWhileStepping = stepTask.Wait(200);
        int stepsWhileBlocked = cpu.Steps;
        cpu.ReleaseStep();

        Assert.Multiple(() =>
        {
            Assert.That(returnedWhileStepping, Is.False, "Step returned while the run loop was still executing");
            Assert.That(stepsWhileBlocked, Is.EqualTo(stepsBefore), "Step entered the CPU while the run loop was inside it");
            Assert.That(stepTask.Wait(5000), Is.True);
            Assert.That(machine.State, Is.EqualTo(MachineState.Paused));
            Assert.That(cpu.Detector.Overlaps, Is.Zero);
        });

        await runTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that Resume issued while the previous run loop is still winding down waits for
    /// it instead of starting a second loop on the same CPU.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Resume_WhileThePreviousLoopIsWindingDown_DoesNotStartASecondLoop()
    {
        using var cpu = new GatedCpu();
        var machine = CreateTestMachine(cpu.Mock);
        var runTask = Task.Run(() => machine.RunAsync());
        cpu.BlockNextStep();

        var pauseTask = Task.Run(machine.Pause);
        Assert.That(SpinWait.SpinUntil(() => machine.State == MachineState.Paused, 5000), Is.True);
        var resumeTask = Task.Run(() => machine.ResumeAsync());
        bool resumeStartedEarly = SpinWait.SpinUntil(() => cpu.Detector.Overlaps > 0, 200);
        cpu.ReleaseStep();

        await pauseTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(SpinWait.SpinUntil(() => machine.State == MachineState.Running, 5000), Is.True, "Resume must run once the old loop has exited");
        machine.Stop();
        await Task.WhenAll(runTask, resumeTask).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(resumeStartedEarly, Is.False, "A second run loop stepped the CPU while the first was inside a step");
            Assert.That(cpu.Detector.Overlaps, Is.Zero);
        });
    }

    /// <summary>
    /// Verifies that Pause waits for the run loop to exit and that rapid Pause/Resume cycles
    /// never leave two run loops executing the same CPU.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PauseResume_NeverRunsTwoLoopsAtOnce()
    {
        var detector = new ConcurrencyDetector();
        Machine machine = null!;
        var mockCpu = CreateMockCpu();
        int stopRequested = 0;
        mockCpu.Setup(c => c.IsStopRequested).Returns(() => Volatile.Read(ref stopRequested) != 0);
        mockCpu.Setup(c => c.RequestStop()).Callback(() => Volatile.Write(ref stopRequested, 1));
        mockCpu.Setup(c => c.ClearStopRequest()).Callback(() => Volatile.Write(ref stopRequested, 0));
        mockCpu.Setup(c => c.Step()).Returns(() =>
        {
            using (detector.Enter())
            {
                Thread.SpinWait(20);
                machine.Scheduler.Advance(1);
            }

            return new CpuStepResult(CpuRunState.Running, 1);
        });

        machine = CreateTestMachine(mockCpu);
        var tasks = new List<Task> { machine.RunAsync() };
        int pausedButAdvanced = 0;

        // Note: RunAsync/ResumeAsync run their first slice synchronously on the caller.
        for (int i = 0; i < 100; i++)
        {
            machine.Pause();
            ulong pausedAt = machine.Now.Value;
            Thread.SpinWait(5_000);
            if (machine.Now.Value != pausedAt)
            {
                pausedButAdvanced++;
            }

            tasks.Add(machine.ResumeAsync());

            if (i % 3 == 0)
            {
                // A second resume or run while running must not start another loop.
                tasks.Add(machine.ResumeAsync());
                tasks.Add(machine.RunAsync());
            }
        }

        machine.Stop();
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(detector.Overlaps, Is.Zero, "Two run loops stepped the CPU at the same time");
            Assert.That(pausedButAdvanced, Is.Zero, "The CPU kept running after Pause returned");
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));
        });
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
    /// Verifies that the <see cref="Machine.BootAsync(System.Threading.CancellationToken)"/> method
    /// performs reset stepping followed by run logic.
    /// </summary>
    /// <remarks>
    /// This test ensures that the machine's boot sequence properly initializes the CPU by resetting it
    /// and then transitions to the appropriate state after running.
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation of the test.</returns>
    [Test]
    public async Task BootAsync_PerformsResetThenRun()
    {
        var mockCpu = CreateMockCpu();
        mockCpu.SetupSequence(c => c.Step())
            .Returns(new CpuStepResult(CpuRunState.Running, 1))
            .Returns(new CpuStepResult(CpuRunState.Running, 1))
            .Returns(new CpuStepResult(CpuRunState.Running, 1))
            .Returns(new CpuStepResult(CpuRunState.Stopped, 1));

        var machine = CreateTestMachine(mockCpu);

        // Act
        await machine.BootAsync();

        // Assert
        Assert.Multiple(() =>
        {
            mockCpu.Verify(c => c.Step(), Times.Exactly(4));
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped), "Should be stopped after boot completes");
        });
    }

    /// <summary>
    /// Tests that the <see cref="Machine.RunAsync(System.Threading.CancellationToken)"/> method
    /// can be cancelled successfully.
    /// </summary>
    /// <remarks>
    /// This test ensures that when a cancellation token is triggered during the execution of
    /// <see cref="Machine.RunAsync(System.Threading.CancellationToken)"/>, the machine transitions
    /// from the <see cref="MachineState.Running"/> state to the <see cref="MachineState.Paused"/> state.
    /// </remarks>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown if the cancellation token is triggered while the method is running.
    /// </exception>
    /// <returns>A task that represents the asynchronous operation of the test.</returns>
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

    /// <summary>
    /// Verifies that an exception from <c>Cpu.Step</c> (or a scheduled callback it
    /// dispatches) leaves the machine <see cref="MachineState.Stopped"/> rather than
    /// stuck in <see cref="MachineState.Running"/>, and that the exception is rethrown
    /// so callers such as BootCommand can still report it.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of the test.</returns>
    [Test]
    public async Task RunAsync_WhenStepThrows_TransitionsToStoppedAndRethrows()
    {
        var mockCpu = CreateMockCpu();
        mockCpu.Setup(c => c.Step()).Throws(new InvalidOperationException("scheduler fault"));

        var machine = CreateTestMachine(mockCpu);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => machine.RunAsync());

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo("scheduler fault"));
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped));
        });
    }

    // ─── CPU Throttling Tests ───────────────────────────────────────────

    /// <summary>
    /// Verifies that ClockSpeedHz defaults to zero (unthrottled).
    /// </summary>
    [Test]
    public void ClockSpeedHz_DefaultsToZero()
    {
        var machine = CreateTestMachine();
        Assert.That(machine.ClockSpeedHz, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that ClockSpeedHz can be set and retrieved.
    /// </summary>
    [Test]
    public void ClockSpeedHz_CanBeSetAndRetrieved()
    {
        var machine = CreateTestMachine();
        machine.ClockSpeedHz = 1_020_484;
        Assert.That(machine.ClockSpeedHz, Is.EqualTo(1_020_484));
    }

    /// <summary>
    /// Verifies that ClockSpeedHz can be changed at runtime.
    /// </summary>
    [Test]
    public void ClockSpeedHz_CanBeChangedAtRuntime()
    {
        var machine = CreateTestMachine();
        machine.ClockSpeedHz = 1_020_484;
        Assert.That(machine.ClockSpeedHz, Is.EqualTo(1_020_484));

        machine.ClockSpeedHz = 2_800_000; // IIgs fast mode
        Assert.That(machine.ClockSpeedHz, Is.EqualTo(2_800_000));

        machine.ClockSpeedHz = 0; // Unthrottled
        Assert.That(machine.ClockSpeedHz, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that throttling slows execution when clock speed is set.
    /// </summary>
    /// <remarks>
    /// This test configures a very low clock speed so that 10,000 cycles
    /// (the yield interval) should take significant wall-clock time. It then
    /// verifies that execution completes in a time range consistent with
    /// throttling rather than running flat-out.
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation of the test.</returns>
    [Test]
    public async Task RunAsync_WithThrottling_SlowsExecution()
    {
        var mockCpu = CreateMockCpu();
        int stepCount = 0;

        // Each step returns 100 cycles, so 100 steps = 10,000 cycles = 1 yield
        mockCpu.Setup(c => c.Step()).Returns(() =>
        {
            stepCount++;
            return new CpuStepResult(CpuRunState.Running, 100);
        });

        var machine = CreateTestMachine(mockCpu);

        // Set clock speed to 100,000 Hz — 10,000 cycles should take 100ms
        machine.ClockSpeedHz = 100_000;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await machine.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        stopwatch.Stop();

        // At 100,000 Hz with 100 cycles/step, we get ~1000 steps/second.
        // In 500ms, we expect ~500 steps. We use a generous upper bound of 2000
        // to account for timer resolution, scheduling overhead, and CI variability.
        // Without throttling, this same test would execute hundreds of thousands of steps.
        Assert.That(stepCount, Is.LessThan(2000), "Throttling should limit step count");
    }

    /// <summary>
    /// Verifies that unthrottled execution runs at maximum speed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of the test.</returns>
    [Test]
    public async Task RunAsync_WithoutThrottling_RunsAtFullSpeed()
    {
        var mockCpu = CreateMockCpu();
        int stepCount = 0;

        // Each step returns 100 cycles
        mockCpu.Setup(c => c.Step()).Returns(() =>
        {
            stepCount++;
            return new CpuStepResult(CpuRunState.Running, 100);
        });

        var machine = CreateTestMachine(mockCpu);

        // No throttling (default ClockSpeedHz = 0)
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try
        {
            await machine.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Without throttling, we should execute a very large number of steps
        Assert.That(stepCount, Is.GreaterThan(1000), "Without throttling, step count should be high");
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

    private static (Machine Machine, Mock<ISlotCard> ReceiverCard, Mock<ISlotCard> PlainCard) CreateMachineWithSlotCards()
    {
        var machine = CreateTestMachine();
        var slotManager = new SlotManager(new IOPageDispatcher());
        machine.AddComponent<ISlotManager>(slotManager);

        var receiverCard = new Mock<ISlotCard>();
        receiverCard.As<IResetReceiver>();
        receiverCard.Setup(c => c.Name).Returns("ReceiverCard");
        var plainCard = new Mock<ISlotCard>();
        plainCard.Setup(c => c.Name).Returns("PlainCard");

        slotManager.Install(4, receiverCard.Object);
        slotManager.Install(6, plainCard.Object);
        return (machine, receiverCard, plainCard);
    }

    private static Mock<ICpu> CreateMockCpu()
    {
        var mockCpu = new Mock<ICpu>();
        mockCpu.Setup(c => c.Halted).Returns(false);
        mockCpu.Setup(c => c.IsStopRequested).Returns(false);
        mockCpu.Setup(c => c.Step()).Returns(new CpuStepResult(CpuRunState.Running, 1));
        return mockCpu;
    }

    /// <summary>
    /// A mock CPU whose next step can be held inside <see cref="ICpu.Step"/> until released,
    /// to observe what other threads do while the run loop is mid-instruction.
    /// </summary>
    private sealed class GatedCpu : IDisposable
    {
        private readonly ManualResetEventSlim entered = new(false);
        private readonly ManualResetEventSlim release = new(true);
        private int stopRequested;
        private int blockNext;
        private int steps;

        /// <summary>
        /// Initializes a new instance of the <see cref="GatedCpu"/> class.
        /// </summary>
        public GatedCpu()
        {
            Mock = CreateMockCpu();
            Mock.Setup(c => c.IsStopRequested).Returns(() => Volatile.Read(ref stopRequested) != 0);
            Mock.Setup(c => c.RequestStop()).Callback(() => Volatile.Write(ref stopRequested, 1));
            Mock.Setup(c => c.ClearStopRequest()).Callback(() => Volatile.Write(ref stopRequested, 0));
            Mock.Setup(c => c.Step()).Returns(() =>
            {
                using (Detector.Enter())
                {
                    Interlocked.Increment(ref steps);
                    if (Interlocked.Exchange(ref blockNext, 0) != 0)
                    {
                        entered.Set();
                        release.Wait(TimeSpan.FromSeconds(10));
                    }
                }

                return new CpuStepResult(CpuRunState.Running, 1);
            });
        }

        /// <summary>Gets the mock.</summary>
        public Mock<ICpu> Mock { get; }

        /// <summary>Gets the detector guarding every step.</summary>
        public ConcurrencyDetector Detector { get; } = new();

        /// <summary>Gets the number of steps entered so far.</summary>
        public int Steps => Volatile.Read(ref steps);

        /// <summary>
        /// Holds the next step inside the CPU and waits until a step is held.
        /// </summary>
        public void BlockNextStep()
        {
            release.Reset();
            entered.Reset();
            Volatile.Write(ref blockNext, 1);
            Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True, "The run loop never stepped the CPU");
        }

        /// <summary>
        /// Lets the held step finish.
        /// </summary>
        public void ReleaseStep() => release.Set();

        /// <inheritdoc />
        public void Dispose()
        {
            release.Set();
            entered.Dispose();
            release.Dispose();
        }
    }

    /// <summary>
    /// Counts how often two different threads are inside guarded sections at the same time.
    /// Re-entry on the same thread (for example a RESET release dispatched inside a CPU step)
    /// is not an overlap.
    /// </summary>
    private sealed class ConcurrencyDetector
    {
        private readonly Dictionary<int, int> depthByThread = [];
        private int overlaps;

        /// <summary>
        /// Gets the number of times a section was entered while another thread was inside one.
        /// </summary>
        public int Overlaps
        {
            get
            {
                lock (depthByThread)
                {
                    return overlaps;
                }
            }
        }

        /// <summary>
        /// Enters a guarded section.
        /// </summary>
        /// <returns>A scope that leaves the section when disposed.</returns>
        public Scope Enter()
        {
            int thread = Environment.CurrentManagedThreadId;
            lock (depthByThread)
            {
                depthByThread.TryGetValue(thread, out int depth);
                if (depth == 0 && depthByThread.Count > 0)
                {
                    overlaps++;
                }

                depthByThread[thread] = depth + 1;
            }

            return new Scope(this, thread);
        }

        private void Leave(int thread)
        {
            lock (depthByThread)
            {
                int depth = depthByThread[thread] - 1;
                if (depth == 0)
                {
                    depthByThread.Remove(thread);
                }
                else
                {
                    depthByThread[thread] = depth;
                }
            }
        }

        /// <summary>
        /// Leaves the guarded section on dispose.
        /// </summary>
        public readonly struct Scope : IDisposable
        {
            private readonly ConcurrencyDetector owner;
            private readonly int thread;

            /// <summary>
            /// Initializes a new instance of the <see cref="Scope"/> struct.
            /// </summary>
            /// <param name="owner">The detector that owns the section.</param>
            /// <param name="thread">The managed thread that entered the section.</param>
            public Scope(ConcurrencyDetector owner, int thread)
            {
                this.owner = owner;
                this.thread = thread;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                owner.Leave(thread);
            }
        }
    }
}