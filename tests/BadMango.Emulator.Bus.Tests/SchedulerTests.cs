// <copyright file="SchedulerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Moq;

/// <summary>
/// Unit tests for the <see cref="Scheduler"/> class.
/// </summary>
[TestFixture]
public class SchedulerTests
{
    /// <summary>
    /// Verifies that a new scheduler starts at cycle 0.
    /// </summary>
    [Test]
    public void Scheduler_NewInstance_StartsAtCycleZero()
    {
        var scheduler = new Scheduler();

        Assert.That(scheduler.CurrentCycle, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies that a new scheduler has no pending events.
    /// </summary>
    [Test]
    public void Scheduler_NewInstance_HasNoPendingEvents()
    {
        var scheduler = new Scheduler();

        Assert.That(scheduler.PendingEventCount, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that Schedule adds an event to the queue.
    /// </summary>
    [Test]
    public void Schedule_AddsEventToQueue()
    {
        var scheduler = new Scheduler();
        var mockActor = new Mock<ISchedulable>();

        scheduler.Schedule(mockActor.Object, 100ul);

        Assert.That(scheduler.PendingEventCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that Schedule throws for null actor.
    /// </summary>
    [Test]
    public void Schedule_NullActor_ThrowsArgumentNullException()
    {
        var scheduler = new Scheduler();

        Assert.Throws<ArgumentNullException>(() => scheduler.Schedule(null!, 100ul));
    }

    /// <summary>
    /// Verifies that ScheduleAfter adds event at correct cycle.
    /// </summary>
    [Test]
    public void ScheduleAfter_AddsEventAtCorrectCycle()
    {
        var scheduler = new Scheduler();
        var executedAt = 0ul;
        var actor = new TestActor(() => executedAt = scheduler.CurrentCycle);

        scheduler.ScheduleAfter(actor, 50ul);
        scheduler.RunUntil(50ul);

        Assert.That(executedAt, Is.EqualTo(50ul));
    }

    /// <summary>
    /// Verifies that ScheduleAfter throws for null actor.
    /// </summary>
    [Test]
    public void ScheduleAfter_NullActor_ThrowsArgumentNullException()
    {
        var scheduler = new Scheduler();

        Assert.Throws<ArgumentNullException>(() => scheduler.ScheduleAfter(null!, 100ul));
    }

    /// <summary>
    /// Verifies that Drain executes events at or before current cycle.
    /// </summary>
    [Test]
    public void Drain_ExecutesEventsDueAtCurrentCycle()
    {
        var scheduler = new Scheduler();
        bool executed = false;
        var actor = new TestActor(() => executed = true);

        scheduler.Schedule(actor, 0ul); // Due at cycle 0
        scheduler.Drain();

        Assert.That(executed, Is.True);
    }

    /// <summary>
    /// Verifies that Drain does not execute events scheduled for the future.
    /// </summary>
    [Test]
    public void Drain_DoesNotExecuteFutureEvents()
    {
        var scheduler = new Scheduler();
        bool executed = false;
        var actor = new TestActor(() => executed = true);

        scheduler.Schedule(actor, 100ul); // Due at cycle 100
        scheduler.Drain();

        Assert.That(executed, Is.False);
    }

    /// <summary>
    /// Verifies that RunUntil advances current cycle to target.
    /// </summary>
    [Test]
    public void RunUntil_AdvancesCurrentCycleToTarget()
    {
        var scheduler = new Scheduler();

        scheduler.RunUntil(500ul);

        Assert.That(scheduler.CurrentCycle, Is.EqualTo(500ul));
    }

    /// <summary>
    /// Verifies that RunUntil executes all due events.
    /// </summary>
    [Test]
    public void RunUntil_ExecutesAllDueEvents()
    {
        var scheduler = new Scheduler();
        var executionOrder = new List<int>();
        var actor1 = new TestActor(() => executionOrder.Add(1));
        var actor2 = new TestActor(() => executionOrder.Add(2));
        var actor3 = new TestActor(() => executionOrder.Add(3));

        scheduler.Schedule(actor1, 10ul);
        scheduler.Schedule(actor2, 50ul);
        scheduler.Schedule(actor3, 100ul);

        scheduler.RunUntil(100ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Verifies that RunUntil does not execute events beyond target.
    /// </summary>
    [Test]
    public void RunUntil_DoesNotExecuteEventsBeyondTarget()
    {
        var scheduler = new Scheduler();
        bool executedEarly = false;
        bool executedLate = false;
        var earlyActor = new TestActor(() => executedEarly = true);
        var lateActor = new TestActor(() => executedLate = true);

        scheduler.Schedule(earlyActor, 50ul);
        scheduler.Schedule(lateActor, 150ul);

        scheduler.RunUntil(100ul);

        Assert.Multiple(() =>
        {
            Assert.That(executedEarly, Is.True);
            Assert.That(executedLate, Is.False);
        });
    }

    /// <summary>
    /// Verifies that events are dispatched in deterministic order by cycle.
    /// </summary>
    [Test]
    public void Events_AreDispatchedInOrderByCycle()
    {
        var scheduler = new Scheduler();
        var executionOrder = new List<string>();

        // Schedule out of order
        scheduler.Schedule(new TestActor(() => executionOrder.Add("C")), 30ul);
        scheduler.Schedule(new TestActor(() => executionOrder.Add("A")), 10ul);
        scheduler.Schedule(new TestActor(() => executionOrder.Add("B")), 20ul);

        scheduler.RunUntil(30ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { "A", "B", "C" }));
    }

    /// <summary>
    /// Verifies that events at the same cycle are dispatched in schedule order (FIFO).
    /// </summary>
    [Test]
    public void Events_SameCycle_DispatchedInScheduleOrder()
    {
        var scheduler = new Scheduler();
        var executionOrder = new List<string>();

        // All scheduled for the same cycle
        scheduler.Schedule(new TestActor(() => executionOrder.Add("First")), 100ul);
        scheduler.Schedule(new TestActor(() => executionOrder.Add("Second")), 100ul);
        scheduler.Schedule(new TestActor(() => executionOrder.Add("Third")), 100ul);

        scheduler.RunUntil(100ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { "First", "Second", "Third" }));
    }

    /// <summary>
    /// Verifies deterministic dispatch: same inputs produce same output.
    /// </summary>
    [Test]
    public void Events_DeterministicDispatch_SameInputsSameOutput()
    {
        var results = new List<List<string>>();

        // Run the same sequence multiple times
        for (int run = 0; run < 3; run++)
        {
            var scheduler = new Scheduler();
            var executionOrder = new List<string>();

            scheduler.Schedule(new TestActor(() => executionOrder.Add("C")), 30ul);
            scheduler.Schedule(new TestActor(() => executionOrder.Add("A")), 10ul);
            scheduler.Schedule(new TestActor(() => executionOrder.Add("D")), 30ul); // Same cycle as C
            scheduler.Schedule(new TestActor(() => executionOrder.Add("B")), 20ul);

            scheduler.RunUntil(30ul);
            results.Add(new List<string>(executionOrder));
        }

        // All runs should produce identical results
        Assert.That(results[0], Is.EqualTo(results[1]));
        Assert.That(results[1], Is.EqualTo(results[2]));
        Assert.That(results[0], Is.EqualTo(new[] { "A", "B", "C", "D" }));
    }

    /// <summary>
    /// Verifies that Cancel removes all events for an actor.
    /// </summary>
    [Test]
    public void Cancel_RemovesAllEventsForActor()
    {
        var scheduler = new Scheduler();
        bool executed = false;
        var actor = new TestActor(() => executed = true);

        scheduler.Schedule(actor, 100ul);
        bool cancelled = scheduler.Cancel(actor);
        scheduler.RunUntil(200ul);

        Assert.Multiple(() =>
        {
            Assert.That(cancelled, Is.True);
            Assert.That(executed, Is.False);
            Assert.That(scheduler.PendingEventCount, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that Cancel returns false when actor has no events.
    /// </summary>
    [Test]
    public void Cancel_NoEvents_ReturnsFalse()
    {
        var scheduler = new Scheduler();
        var mockActor = new Mock<ISchedulable>();

        bool cancelled = scheduler.Cancel(mockActor.Object);

        Assert.That(cancelled, Is.False);
    }

    /// <summary>
    /// Verifies that Cancel throws for null actor.
    /// </summary>
    [Test]
    public void Cancel_NullActor_ThrowsArgumentNullException()
    {
        var scheduler = new Scheduler();

        Assert.Throws<ArgumentNullException>(() => scheduler.Cancel(null!));
    }

    /// <summary>
    /// Verifies that Cancel only removes events for the specified actor.
    /// </summary>
    [Test]
    public void Cancel_OnlyRemovesSpecifiedActorEvents()
    {
        var scheduler = new Scheduler();
        bool actor1Executed = false;
        bool actor2Executed = false;
        var actor1 = new TestActor(() => actor1Executed = true);
        var actor2 = new TestActor(() => actor2Executed = true);

        scheduler.Schedule(actor1, 100ul);
        scheduler.Schedule(actor2, 100ul);
        scheduler.Cancel(actor1);
        scheduler.RunUntil(200ul);

        Assert.Multiple(() =>
        {
            Assert.That(actor1Executed, Is.False);
            Assert.That(actor2Executed, Is.True);
        });
    }

    /// <summary>
    /// Verifies that Reset clears all events.
    /// </summary>
    [Test]
    public void Reset_ClearsAllEvents()
    {
        var scheduler = new Scheduler();
        var mockActor = new Mock<ISchedulable>();

        scheduler.Schedule(mockActor.Object, 100ul);
        scheduler.Schedule(mockActor.Object, 200ul);
        scheduler.Reset();

        Assert.That(scheduler.PendingEventCount, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that Reset resets cycle to zero.
    /// </summary>
    [Test]
    public void Reset_ResetsCycleToZero()
    {
        var scheduler = new Scheduler();

        scheduler.RunUntil(1000ul);
        scheduler.Reset();

        Assert.That(scheduler.CurrentCycle, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies that Reset allows scheduling deterministic events after reset.
    /// </summary>
    [Test]
    public void Reset_AllowsDeterministicSchedulingAfterReset()
    {
        var scheduler = new Scheduler();
        var executionOrder = new List<string>();

        // First run
        scheduler.Schedule(new TestActor(() => executionOrder.Add("A")), 10ul);
        scheduler.Schedule(new TestActor(() => executionOrder.Add("B")), 10ul);
        scheduler.RunUntil(10ul);

        // Reset
        scheduler.Reset();
        executionOrder.Clear();

        // Second run - should produce same order
        scheduler.Schedule(new TestActor(() => executionOrder.Add("A")), 10ul);
        scheduler.Schedule(new TestActor(() => executionOrder.Add("B")), 10ul);
        scheduler.RunUntil(10ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { "A", "B" }));
    }

    /// <summary>
    /// Verifies that actors can schedule future events during execution.
    /// </summary>
    [Test]
    public void Actor_CanScheduleFutureEvents()
    {
        var scheduler = new Scheduler();
        var executionOrder = new List<string>();
        TestActor? firstActor = null;
        firstActor = new TestActor(() =>
        {
            executionOrder.Add("First");
            scheduler.ScheduleAfter(new TestActor(() => executionOrder.Add("Second")), 10ul);
        });

        scheduler.Schedule(firstActor, 10ul);
        scheduler.RunUntil(100ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { "First", "Second" }));
    }

    /// <summary>
    /// Verifies that actor-returned cycles advance the current cycle.
    /// </summary>
    [Test]
    public void Actor_ConsumedCycles_AdvanceCurrentCycle()
    {
        var scheduler = new Scheduler();
        var actor = new TestActor(() => { }, consumedCycles: 10ul);

        scheduler.Schedule(actor, 0ul);
        scheduler.Drain();

        Assert.That(scheduler.CurrentCycle, Is.EqualTo(10ul));
    }

    /// <summary>
    /// Verifies that zero consumed cycles don't advance the cycle.
    /// </summary>
    [Test]
    public void Actor_ZeroConsumedCycles_DoNotAdvanceCycle()
    {
        var scheduler = new Scheduler();
        var actor = new TestActor(() => { }, consumedCycles: 0ul);

        scheduler.Schedule(actor, 0ul);
        scheduler.Drain();

        Assert.That(scheduler.CurrentCycle, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies scheduler advances to event's due cycle even if no cycles consumed.
    /// </summary>
    [Test]
    public void RunUntil_AdvancesToEventCycle_BeforeExecution()
    {
        var scheduler = new Scheduler();
        ulong cycleWhenExecuted = 0;
        var actor = new TestActor(() => cycleWhenExecuted = scheduler.CurrentCycle, consumedCycles: 0ul);

        scheduler.Schedule(actor, 50ul);
        scheduler.RunUntil(100ul);

        Assert.That(cycleWhenExecuted, Is.EqualTo(50ul));
    }

    /// <summary>
    /// Verifies that scheduling an event in the past clamps to current cycle behavior.
    /// </summary>
    [Test]
    public void Schedule_EventInPast_ExecutesOnNextDrain()
    {
        var scheduler = new Scheduler();
        bool executed = false;
        var actor = new TestActor(() => executed = true);

        scheduler.RunUntil(100ul); // Advance to cycle 100
        scheduler.Schedule(actor, 50ul); // Schedule for cycle 50 (in the past)
        scheduler.Drain(); // Should execute immediately

        Assert.That(executed, Is.True);
    }

    /// <summary>
    /// Verifies PendingEventCount reflects scheduled events correctly.
    /// </summary>
    [Test]
    public void PendingEventCount_ReflectsScheduledEvents()
    {
        var scheduler = new Scheduler();
        var mockActor = new Mock<ISchedulable>();
        mockActor.Setup(a => a.Execute(It.IsAny<ulong>(), It.IsAny<IScheduler>())).Returns(0ul);

        Assert.That(scheduler.PendingEventCount, Is.EqualTo(0));

        scheduler.Schedule(mockActor.Object, 100ul);
        Assert.That(scheduler.PendingEventCount, Is.EqualTo(1));

        scheduler.Schedule(mockActor.Object, 200ul);
        Assert.That(scheduler.PendingEventCount, Is.EqualTo(2));

        scheduler.RunUntil(100ul);
        Assert.That(scheduler.PendingEventCount, Is.EqualTo(1));

        scheduler.RunUntil(200ul);
        Assert.That(scheduler.PendingEventCount, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies device can reschedule itself for periodic timer behavior.
    /// </summary>
    [Test]
    public void Device_CanRescheduleItself_ForPeriodicTimer()
    {
        var scheduler = new Scheduler();
        var tickCount = 0;
        const int interval = 100;
        PeriodicActor? periodicActor = null;

        periodicActor = new PeriodicActor(
            () => tickCount++,
            () => scheduler.ScheduleAfter(periodicActor!, interval));

        scheduler.Schedule(periodicActor, interval);
        scheduler.RunUntil(350ul); // Should fire at 100, 200, 300

        Assert.That(tickCount, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that AdvanceCycles advances the current cycle.
    /// </summary>
    [Test]
    public void AdvanceCycles_AdvancesCurrentCycle()
    {
        var scheduler = new Scheduler();

        scheduler.AdvanceCycles(100ul);

        Assert.That(scheduler.CurrentCycle, Is.EqualTo(100ul));
    }

    /// <summary>
    /// Verifies that AdvanceCycles with zero does not change cycle.
    /// </summary>
    [Test]
    public void AdvanceCycles_WithZero_DoesNotChange()
    {
        var scheduler = new Scheduler();
        scheduler.RunUntil(50ul);

        scheduler.AdvanceCycles(0ul);

        Assert.That(scheduler.CurrentCycle, Is.EqualTo(50ul));
    }

    /// <summary>
    /// Verifies that AdvanceCycles executes due events.
    /// </summary>
    [Test]
    public void AdvanceCycles_ExecutesDueEvents()
    {
        var scheduler = new Scheduler();
        var executionOrder = new List<int>();
        var actor1 = new TestActor(() => executionOrder.Add(1));
        var actor2 = new TestActor(() => executionOrder.Add(2));

        scheduler.Schedule(actor1, 50ul);
        scheduler.Schedule(actor2, 75ul);

        scheduler.AdvanceCycles(100ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2 }));
    }

    /// <summary>
    /// Verifies that AdvanceCycles does not execute future events.
    /// </summary>
    [Test]
    public void AdvanceCycles_DoesNotExecuteFutureEvents()
    {
        var scheduler = new Scheduler();
        bool executed = false;
        var actor = new TestActor(() => executed = true);

        scheduler.Schedule(actor, 200ul);
        scheduler.AdvanceCycles(100ul);

        Assert.Multiple(() =>
        {
            Assert.That(executed, Is.False);
            Assert.That(scheduler.CurrentCycle, Is.EqualTo(100ul));
        });
    }

    /// <summary>
    /// Verifies that AdvanceCycles accumulates correctly.
    /// </summary>
    [Test]
    public void AdvanceCycles_AccumulatesCorrectly()
    {
        var scheduler = new Scheduler();

        scheduler.AdvanceCycles(50ul);
        scheduler.AdvanceCycles(30ul);
        scheduler.AdvanceCycles(20ul);

        Assert.That(scheduler.CurrentCycle, Is.EqualTo(100ul));
    }

    /// <summary>
    /// Verifies that AdvanceCycles works with events consuming cycles.
    /// </summary>
    [Test]
    public void AdvanceCycles_WithEventConsumingCycles()
    {
        var scheduler = new Scheduler();
        var actor = new TestActor(() => { }, consumedCycles: 10ul);

        scheduler.Schedule(actor, 50ul);
        scheduler.AdvanceCycles(100ul);

        // The event fires at 50 and consumes 10 cycles (current cycle becomes 60)
        // Then we advance to the target cycle 100
        Assert.That(scheduler.CurrentCycle, Is.EqualTo(100ul));
    }

    /// <summary>
    /// Verifies integration between SignalBus and Scheduler for CPU-driven timing.
    /// </summary>
    [Test]
    public void Integration_SignalBusAndScheduler_CpuDrivenTiming()
    {
        var signalBus = new SignalBus();
        var scheduler = new Scheduler();
        bool deviceTriggered = false;

        // Schedule a device event at cycle 50
        scheduler.Schedule(new TestActor(() => deviceTriggered = true), 50ul);

        // Simulate CPU executing 10 instructions of 6 cycles each
        for (int i = 0; i < 10; i++)
        {
            signalBus.SignalInstructionFetched(2);
            signalBus.SignalInstructionExecuted(4);
            scheduler.AdvanceCycles(6);
        }

        Assert.Multiple(() =>
        {
            Assert.That(signalBus.TotalCpuCycles, Is.EqualTo(60ul));
            Assert.That(scheduler.CurrentCycle, Is.EqualTo(60ul));
            Assert.That(deviceTriggered, Is.True, "Device event should have triggered at cycle 50");
        });
    }

    /// <summary>
    /// Verifies that AdvanceCycles maintains deterministic event ordering.
    /// </summary>
    [Test]
    public void AdvanceCycles_MaintainsDeterministicOrdering()
    {
        var results = new List<List<int>>();

        for (int run = 0; run < 3; run++)
        {
            var scheduler = new Scheduler();
            var executionOrder = new List<int>();

            scheduler.Schedule(new TestActor(() => executionOrder.Add(1)), 25ul);
            scheduler.Schedule(new TestActor(() => executionOrder.Add(2)), 25ul);
            scheduler.Schedule(new TestActor(() => executionOrder.Add(3)), 50ul);

            // Advance in increments
            scheduler.AdvanceCycles(30ul);
            scheduler.AdvanceCycles(30ul);

            results.Add(new List<int>(executionOrder));
        }

        // All runs should produce identical results
        Assert.That(results[0], Is.EqualTo(results[1]));
        Assert.That(results[1], Is.EqualTo(results[2]));
        Assert.That(results[0], Is.EqualTo(new[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Test actor that executes a callback and returns a specified number of consumed cycles.
    /// </summary>
    private sealed class TestActor : ISchedulable
    {
        private readonly Action callback;
        private readonly ulong consumedCycles;

        public TestActor(Action callback, ulong consumedCycles = 0)
        {
            this.callback = callback;
            this.consumedCycles = consumedCycles;
        }

        public ulong Execute(ulong currentCycle, IScheduler scheduler)
        {
            callback();
            return consumedCycles;
        }
    }

    /// <summary>
    /// Actor that executes a callback and reschedules itself.
    /// </summary>
    private sealed class PeriodicActor : ISchedulable
    {
        private readonly Action tickCallback;
        private readonly Action rescheduleCallback;

        public PeriodicActor(Action tickCallback, Action rescheduleCallback)
        {
            this.tickCallback = tickCallback;
            this.rescheduleCallback = rescheduleCallback;
        }

        public ulong Execute(ulong currentCycle, IScheduler scheduler)
        {
            tickCallback();
            rescheduleCallback();
            return 0;
        }
    }
}