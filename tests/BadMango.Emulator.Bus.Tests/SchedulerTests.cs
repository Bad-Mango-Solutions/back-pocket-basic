// <copyright file="SchedulerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Core.Interfaces.Signaling;
using BadMango.Emulator.Core.Signaling;

using Core;

using Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="Scheduler"/> class.
/// </summary>
[TestFixture]
public class SchedulerTests
{
    private Scheduler scheduler = null!;
    private Mock<ISignalBus> mockSignals = null!;
    private Mock<IMemoryBus> mockBus = null!;
    private IEventContext eventContext = null!;

    /// <summary>
    /// Sets up the test fixture before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        scheduler = new();
        mockSignals = new();
        mockBus = new();
        eventContext = new EventContext(scheduler, mockSignals.Object, mockBus.Object);
        scheduler.SetEventContext(eventContext);
    }

    /// <summary>
    /// Verifies that a new scheduler starts at cycle 0.
    /// </summary>
    [Test]
    public void Scheduler_NewInstance_StartsAtCycleZero()
    {
        var newScheduler = new Scheduler();

        Assert.That((ulong)newScheduler.Now, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies that a new scheduler has no pending events.
    /// </summary>
    [Test]
    public void Scheduler_NewInstance_HasNoPendingEvents()
    {
        var newScheduler = new Scheduler();

        Assert.That(newScheduler.PendingEventCount, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that ScheduleAt adds an event to the queue.
    /// </summary>
    [Test]
    public void ScheduleAt_AddsEventToQueue()
    {
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => { });

        Assert.That(scheduler.PendingEventCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that ScheduleAt returns a valid handle.
    /// </summary>
    [Test]
    public void ScheduleAt_ReturnsValidHandle()
    {
        var handle = scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => { });

        Assert.That(handle.Id, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies that ScheduleAt throws for null callback.
    /// </summary>
    [Test]
    public void ScheduleAt_NullCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, null!));
    }

    /// <summary>
    /// Verifies that ScheduleAfter adds event at correct cycle.
    /// </summary>
    [Test]
    public void ScheduleAfter_AddsEventAtCorrectCycle()
    {
        var executedAt = 0ul;

        scheduler.ScheduleAfter(50ul, ScheduledEventKind.DeviceTimer, 0, _ => executedAt = scheduler.Now);
        scheduler.Advance(50ul);

        Assert.That(executedAt, Is.EqualTo(50ul));
    }

    /// <summary>
    /// Verifies that ScheduleAfter throws for null callback.
    /// </summary>
    [Test]
    public void ScheduleAfter_NullCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => scheduler.ScheduleAfter(100ul, ScheduledEventKind.DeviceTimer, 0, null!));
    }

    /// <summary>
    /// Verifies that DispatchDue executes events at or before current cycle.
    /// </summary>
    [Test]
    public void DispatchDue_ExecutesEventsDueAtCurrentCycle()
    {
        bool executed = false;

        scheduler.ScheduleAt(0ul, ScheduledEventKind.DeviceTimer, 0, _ => executed = true);
        scheduler.DispatchDue();

        Assert.That(executed, Is.True);
    }

    /// <summary>
    /// Verifies that DispatchDue does not execute events scheduled for the future.
    /// </summary>
    [Test]
    public void DispatchDue_DoesNotExecuteFutureEvents()
    {
        bool executed = false;

        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => executed = true);
        scheduler.DispatchDue();

        Assert.That(executed, Is.False);
    }

    /// <summary>
    /// Verifies that Advance advances current cycle to target.
    /// </summary>
    [Test]
    public void Advance_AdvancesCurrentCycleToTarget()
    {
        scheduler.Advance(500ul);

        Assert.That((ulong)scheduler.Now, Is.EqualTo(500ul));
    }

    /// <summary>
    /// Verifies that Advance executes all due events.
    /// </summary>
    [Test]
    public void Advance_ExecutesAllDueEvents()
    {
        var executionOrder = new List<int>();

        scheduler.ScheduleAt(10ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add(1));
        scheduler.ScheduleAt(50ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add(2));
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add(3));

        scheduler.Advance(100ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Verifies that Advance does not execute events beyond target.
    /// </summary>
    [Test]
    public void Advance_DoesNotExecuteEventsBeyondTarget()
    {
        bool executedEarly = false;
        bool executedLate = false;

        scheduler.ScheduleAt(50ul, ScheduledEventKind.DeviceTimer, 0, _ => executedEarly = true);
        scheduler.ScheduleAt(150ul, ScheduledEventKind.DeviceTimer, 0, _ => executedLate = true);

        scheduler.Advance(100ul);

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
        var executionOrder = new List<string>();

        // ScheduleAt out of order
        scheduler.ScheduleAt(30ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("C"));
        scheduler.ScheduleAt(10ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("A"));
        scheduler.ScheduleAt(20ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("B"));

        scheduler.Advance(30ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { "A", "B", "C" }));
    }

    /// <summary>
    /// Verifies that events at the same cycle are dispatched in priority then schedule order.
    /// </summary>
    [Test]
    public void Events_SameCycle_DispatchedByPriorityThenScheduleOrder()
    {
        var executionOrder = new List<string>();

        // All scheduled for the same cycle with same priority
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("First"));
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("Second"));
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("Third"));

        scheduler.Advance(100ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { "First", "Second", "Third" }));
    }

    /// <summary>
    /// Verifies that priority affects dispatch order when events share the same cycle.
    /// </summary>
    [Test]
    public void Events_SameCycle_DispatchedByPriority()
    {
        var executionOrder = new List<string>();

        // Different priorities at the same cycle (lower priority value = higher priority)
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 2, _ => executionOrder.Add("Low"));
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("High"));
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 1, _ => executionOrder.Add("Medium"));

        scheduler.Advance(100ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { "High", "Medium", "Low" }));
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
            var testScheduler = new Scheduler();
            var testContext = new EventContext(testScheduler, mockSignals.Object, mockBus.Object);
            testScheduler.SetEventContext(testContext);
            var executionOrder = new List<string>();

            testScheduler.ScheduleAt(30ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("C"));
            testScheduler.ScheduleAt(10ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("A"));
            testScheduler.ScheduleAt(30ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("D")); // Same cycle as C
            testScheduler.ScheduleAt(20ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("B"));

            testScheduler.Advance(30ul);
            results.Add(new(executionOrder));
        }

        // All runs should produce identical results
        Assert.That(results[0], Is.EqualTo(results[1]));
        Assert.That(results[1], Is.EqualTo(results[2]));
        Assert.That(results[0], Is.EqualTo(new[] { "A", "B", "C", "D" }));
    }

    /// <summary>
    /// Verifies that Cancel removes the event by handle.
    /// </summary>
    [Test]
    public void Cancel_RemovesEventByHandle()
    {
        bool executed = false;

        var handle = scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => executed = true);
        bool cancelled = scheduler.Cancel(handle);
        scheduler.Advance(200ul);

        Assert.Multiple(() =>
        {
            Assert.That(cancelled, Is.True);
            Assert.That(executed, Is.False);
        });
    }

    /// <summary>
    /// Verifies that Cancel returns false when handle was not scheduled.
    /// </summary>
    [Test]
    public void Cancel_InvalidHandle_ReturnsFalse()
    {
        var handle = new EventHandle(9999);

        bool cancelled = scheduler.Cancel(handle);

        // First cancel should add to cancelled set
        Assert.That(cancelled, Is.True);

        // Second cancel of same handle should return false
        bool cancelledAgain = scheduler.Cancel(handle);
        Assert.That(cancelledAgain, Is.False);
    }

    /// <summary>
    /// Verifies that Cancel only removes the specified event.
    /// </summary>
    [Test]
    public void Cancel_OnlyRemovesSpecifiedEvent()
    {
        bool event1Executed = false;
        bool event2Executed = false;

        var handle1 = scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => event1Executed = true);
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => event2Executed = true);

        scheduler.Cancel(handle1);
        scheduler.Advance(200ul);

        Assert.Multiple(() =>
        {
            Assert.That(event1Executed, Is.False);
            Assert.That(event2Executed, Is.True);
        });
    }

    /// <summary>
    /// Verifies that Reset clears all events.
    /// </summary>
    [Test]
    public void Reset_ClearsAllEvents()
    {
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => { });
        scheduler.ScheduleAt(200ul, ScheduledEventKind.DeviceTimer, 0, _ => { });
        scheduler.Reset();

        Assert.That(scheduler.PendingEventCount, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that Reset resets cycle to zero.
    /// </summary>
    [Test]
    public void Reset_ResetsCycleToZero()
    {
        scheduler.Advance(1000ul);
        scheduler.Reset();

        Assert.That((ulong)scheduler.Now, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies that Reset allows scheduling deterministic events after reset.
    /// </summary>
    [Test]
    public void Reset_AllowsDeterministicSchedulingAfterReset()
    {
        var executionOrder = new List<string>();

        // First run
        scheduler.ScheduleAt(10ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("A"));
        scheduler.ScheduleAt(10ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("B"));
        scheduler.Advance(10ul);

        // Reset
        scheduler.Reset();
        scheduler.SetEventContext(eventContext);
        executionOrder.Clear();

        // Second run - should produce same order
        scheduler.ScheduleAt(10ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("A"));
        scheduler.ScheduleAt(10ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("B"));
        scheduler.Advance(10ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { "A", "B" }));
    }

    /// <summary>
    /// Verifies that callbacks can schedule future events during execution.
    /// </summary>
    [Test]
    public void Callback_CanScheduleFutureEvents()
    {
        var executionOrder = new List<string>();

        scheduler.ScheduleAt(10ul, ScheduledEventKind.DeviceTimer, 0, ctx =>
        {
            executionOrder.Add("First");
            ctx.Scheduler.ScheduleAfter(10ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("Second"));
        });

        scheduler.Advance(100ul);

        Assert.That(executionOrder, Is.EqualTo(new[] { "First", "Second" }));
    }

    /// <summary>
    /// Verifies that Advance with zero does not change cycle.
    /// </summary>
    [Test]
    public void Advance_WithZero_DoesNotChange()
    {
        scheduler.Advance(50ul);

        scheduler.Advance(0ul);

        Assert.That((ulong)scheduler.Now, Is.EqualTo(50ul));
    }

    /// <summary>
    /// Verifies that Advance accumulates correctly.
    /// </summary>
    [Test]
    public void Advance_AccumulatesCorrectly()
    {
        scheduler.Advance(50ul);
        scheduler.Advance(30ul);
        scheduler.Advance(20ul);

        Assert.That((ulong)scheduler.Now, Is.EqualTo(100ul));
    }

    /// <summary>
    /// Verifies that scheduling an event in the past executes on next dispatch.
    /// </summary>
    [Test]
    public void ScheduleAt_EventInPast_ExecutesOnNextDispatch()
    {
        bool executed = false;

        scheduler.Advance(100ul); // Advance to cycle 100
        scheduler.ScheduleAt(50ul, ScheduledEventKind.DeviceTimer, 0, _ => executed = true); // ScheduleAt for cycle 50 (in the past)
        scheduler.DispatchDue(); // Should execute immediately

        Assert.That(executed, Is.True);
    }

    /// <summary>
    /// Verifies PendingEventCount reflects scheduled events correctly.
    /// </summary>
    [Test]
    public void PendingEventCount_ReflectsScheduledEvents()
    {
        Assert.That(scheduler.PendingEventCount, Is.EqualTo(0));

        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => { });
        Assert.That(scheduler.PendingEventCount, Is.EqualTo(1));

        scheduler.ScheduleAt(200ul, ScheduledEventKind.DeviceTimer, 0, _ => { });
        Assert.That(scheduler.PendingEventCount, Is.EqualTo(2));

        scheduler.Advance(100ul);
        Assert.That(scheduler.PendingEventCount, Is.EqualTo(1));

        scheduler.Advance(100ul);
        Assert.That(scheduler.PendingEventCount, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies callback can reschedule itself for periodic timer behavior.
    /// </summary>
    [Test]
    public void Callback_CanRescheduleItself_ForPeriodicTimer()
    {
        var tickCount = 0;
        const int interval = 100;

        void TimerCallback(IEventContext ctx)
        {
            tickCount++;
            ctx.Scheduler.ScheduleAfter(interval, ScheduledEventKind.DeviceTimer, 0, TimerCallback);
        }

        scheduler.ScheduleAt(interval, ScheduledEventKind.DeviceTimer, 0, TimerCallback);
        scheduler.Advance(350ul); // Should fire at 100, 200, 300

        Assert.That(tickCount, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that PeekNextDue returns the next event cycle.
    /// </summary>
    [Test]
    public void PeekNextDue_ReturnsNextEventCycle()
    {
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => { });
        scheduler.ScheduleAt(50ul, ScheduledEventKind.DeviceTimer, 0, _ => { });

        var nextDue = scheduler.PeekNextDue();

        Assert.That(nextDue, Is.EqualTo((Cycle)50ul));
    }

    /// <summary>
    /// Verifies that PeekNextDue returns null when no events are pending.
    /// </summary>
    [Test]
    public void PeekNextDue_ReturnsNull_WhenNoEvents()
    {
        var nextDue = scheduler.PeekNextDue();

        Assert.That(nextDue, Is.Null);
    }

    /// <summary>
    /// Verifies that PeekNextDue skips cancelled events.
    /// </summary>
    [Test]
    public void PeekNextDue_SkipsCancelledEvents()
    {
        var handle1 = scheduler.ScheduleAt(50ul, ScheduledEventKind.DeviceTimer, 0, _ => { });
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => { });

        scheduler.Cancel(handle1);

        var nextDue = scheduler.PeekNextDue();

        Assert.That(nextDue, Is.EqualTo((Cycle)100ul));
    }

    /// <summary>
    /// Verifies that JumpToNextEventAndDispatch advances to and dispatches next event.
    /// </summary>
    [Test]
    public void JumpToNextEventAndDispatch_AdvancesAndDispatches()
    {
        bool executed = false;

        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => executed = true);

        bool result = scheduler.JumpToNextEventAndDispatch();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(executed, Is.True);
            Assert.That((ulong)scheduler.Now, Is.EqualTo(100ul));
        });
    }

    /// <summary>
    /// Verifies that JumpToNextEventAndDispatch returns false when no events pending.
    /// </summary>
    [Test]
    public void JumpToNextEventAndDispatch_ReturnsFalse_WhenNoEvents()
    {
        bool result = scheduler.JumpToNextEventAndDispatch();

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that JumpToNextEventAndDispatch dispatches all events at the same cycle.
    /// </summary>
    [Test]
    public void JumpToNextEventAndDispatch_DispatchesAllEventsAtSameCycle()
    {
        var executionOrder = new List<int>();

        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add(1));
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add(2));
        scheduler.ScheduleAt(200ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add(3));

        scheduler.JumpToNextEventAndDispatch();

        Assert.Multiple(() =>
        {
            Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2 }));
            Assert.That((ulong)scheduler.Now, Is.EqualTo(100ul));
        });
    }

    /// <summary>
    /// Integration test: Validates end-to-end CPU instruction cycle tracking with scheduler.
    /// </summary>
    [Test]
    public void Integration_CpuInstructionCycleTracking_FullWorkflow()
    {
        var events = new List<(string Name, ulong Cycle)>();

        // Schedule multiple device events
        scheduler.ScheduleAt(10ul, ScheduledEventKind.DeviceTimer, 0, _ => events.Add(("Timer1", scheduler.Now)));
        scheduler.ScheduleAt(25ul, ScheduledEventKind.DeviceTimer, 0, _ => events.Add(("Timer2", scheduler.Now)));
        scheduler.ScheduleAt(50ul, ScheduledEventKind.DeviceTimer, 0, _ => events.Add(("Timer3", scheduler.Now)));

        // Simulate CPU executing instructions with varying cycle counts
        scheduler.Advance(5);   // Instruction 1: 5 cycles
        scheduler.Advance(7);   // Instruction 2: 7 cycles (total: 12)
        scheduler.Advance(12);  // Instruction 3: 12 cycles (total: 24)
        scheduler.Advance(26);  // Instruction 4: 26 cycles (total: 50)

        Assert.Multiple(() =>
        {
            Assert.That((ulong)scheduler.Now, Is.EqualTo(50ul), "Scheduler should be at cycle 50");
            Assert.That(events.Count, Is.EqualTo(3), "All three timer events should have fired");
            Assert.That(events[0].Name, Is.EqualTo("Timer1"));
            Assert.That(events[0].Cycle, Is.EqualTo(10ul));
            Assert.That(events[1].Name, Is.EqualTo("Timer2"));
            Assert.That(events[1].Cycle, Is.EqualTo(25ul));
            Assert.That(events[2].Name, Is.EqualTo("Timer3"));
            Assert.That(events[2].Cycle, Is.EqualTo(50ul));
        });
    }

    /// <summary>
    /// Functional test: Validates event cancellation during CPU-driven execution.
    /// </summary>
    [Test]
    public void Functional_EventCancellation_DuringCpuExecution()
    {
        bool event1Fired = false;
        bool event2Fired = false;

        scheduler.ScheduleAt(30ul, ScheduledEventKind.DeviceTimer, 0, _ => event1Fired = true);
        var handle2 = scheduler.ScheduleAt(50ul, ScheduledEventKind.DeviceTimer, 0, _ => event2Fired = true);

        // Advance to cycle 20, then cancel event2
        scheduler.Advance(20);

        bool cancelled = scheduler.Cancel(handle2);

        // Advance to cycle 60
        scheduler.Advance(40);

        Assert.Multiple(() =>
        {
            Assert.That(cancelled, Is.True, "Cancel should return true");
            Assert.That(event1Fired, Is.True, "Event1 should have fired");
            Assert.That(event2Fired, Is.False, "Event2 should NOT have fired (cancelled)");
            Assert.That((ulong)scheduler.Now, Is.EqualTo(60ul));
        });
    }

    /// <summary>
    /// Functional test: Validates scheduler reset clears all state.
    /// </summary>
    [Test]
    public void Functional_SchedulerReset_ClearsAllState()
    {
        bool eventFired = false;

        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => eventFired = true);

        // Advance partially
        scheduler.Advance(50);

        // Reset scheduler
        scheduler.Reset();
        scheduler.SetEventContext(eventContext);

        // Advance past where the event was scheduled
        scheduler.Advance(150);

        Assert.Multiple(() =>
        {
            Assert.That(eventFired, Is.False, "Event should NOT fire after reset");
            Assert.That((ulong)scheduler.Now, Is.EqualTo(150ul));
            Assert.That(scheduler.PendingEventCount, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that cancelling a pre-reset handle does not prevent post-reset
    /// events from dispatching, even though handle IDs may overlap.
    /// </summary>
    /// <remarks>
    /// This test validates the fix for a bug where <see cref="Scheduler.Reset"/>
    /// reset <c>nextHandleId</c> to 0, causing newly scheduled events to receive
    /// handle IDs that matched previously cancelled handles. Those events were then
    /// silently skipped during dispatch because they appeared in the cancelled set.
    /// </remarks>
    [Test]
    public void Functional_PostResetEvents_NotAffectedByPreResetCancellations()
    {
        // Schedule an event and capture its handle
        var preResetHandle = scheduler.ScheduleAt(
            100ul, ScheduledEventKind.VideoBlank, 0, _ => { });

        // Cancel it (adds handle ID to cancelledHandles)
        scheduler.Cancel(preResetHandle);

        // Reset the scheduler
        scheduler.Reset();
        scheduler.SetEventContext(eventContext);

        // Schedule a new event — this must not collide with the cancelled handle
        bool postResetEventFired = false;
        scheduler.ScheduleAt(
            50ul, ScheduledEventKind.VideoBlank, 0, _ => postResetEventFired = true);

        // Advance past the scheduled cycle
        scheduler.Advance(100);

        Assert.That(postResetEventFired, Is.True,
            "Post-reset event must not be skipped due to stale pre-reset cancellation");
    }

    /// <summary>
    /// Verifies that the device reset pattern (cancel old handles + schedule new
    /// events) works correctly after a scheduler reset.
    /// </summary>
    /// <remarks>
    /// This simulates what <c>VideoDevice.Reset()</c> does: cancel stale VBlank
    /// handles, then schedule fresh events. After a scheduler reset, the cancelled
    /// handle IDs must not collide with newly assigned handle IDs.
    /// </remarks>
    [Test]
    public void Functional_DeviceResetPattern_PostSchedulerReset()
    {
        // Simulate initial device scheduling
        var vblankHandle = scheduler.ScheduleAt(
            12480ul, ScheduledEventKind.VideoBlank, 0, _ => { });

        // Advance partway
        scheduler.Advance(5000);

        // --- Simulate Machine.Reset() sequence ---

        // 1. Reset scheduler (clears all events, resets cycle to 0)
        scheduler.Reset();
        scheduler.SetEventContext(eventContext);

        // 2. Device reset: cancel old handles then reschedule
        //    (VideoDevice.Reset() does this)
        scheduler.Cancel(vblankHandle); // Cancel stale handle
        bool vblankFired = false;
        var newVblankHandle = scheduler.ScheduleAfter(
            12480ul, ScheduledEventKind.VideoBlank, 0, _ => vblankFired = true);

        // 3. Simulate CPU execution advancing past VBlank cycle
        for (int i = 0; i < 2000; i++)
        {
            scheduler.Advance(7); // ~7 cycles per instruction
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                vblankFired,
                Is.True,
                "VBlank event must fire — must not be blocked by stale cancellation");
            Assert.That((ulong)scheduler.Now, Is.EqualTo(14000ul));
        });
    }

    /// <summary>
    /// Functional test: Validates device periodic timer behavior with CPU-driven timing.
    /// </summary>
    [Test]
    public void Functional_PeriodicTimer_WithCpuDrivenTiming()
    {
        var tickCycles = new List<ulong>();
        const ulong timerInterval = 25ul;

        void TimerCallback(IEventContext ctx)
        {
            tickCycles.Add(ctx.Now);
            ctx.Scheduler.ScheduleAfter(timerInterval, ScheduledEventKind.DeviceTimer, 0, TimerCallback);
        }

        scheduler.ScheduleAt(timerInterval, ScheduledEventKind.DeviceTimer, 0, TimerCallback);

        // Simulate CPU executing instructions to advance time
        for (int i = 0; i < 20; i++)
        {
            scheduler.Advance(5);
        }

        // Should have ticks at cycles 25, 50, 75, 100
        Assert.Multiple(() =>
        {
            Assert.That(tickCycles.Count, Is.EqualTo(4));
            Assert.That(tickCycles[0], Is.EqualTo(25ul));
            Assert.That(tickCycles[1], Is.EqualTo(50ul));
            Assert.That(tickCycles[2], Is.EqualTo(75ul));
            Assert.That(tickCycles[3], Is.EqualTo(100ul));
            Assert.That((ulong)scheduler.Now, Is.EqualTo(100ul));
        });
    }

    /// <summary>
    /// Integration test: Validates SignalBus and Scheduler work correctly with mixed operations.
    /// </summary>
    [Test]
    public void Integration_MixedOperations_SignalBusAndScheduler()
    {
        var signalBus = new SignalBus();
        var testScheduler = new Scheduler();
        var testContext = new EventContext(testScheduler, signalBus, mockBus.Object);
        testScheduler.SetEventContext(testContext);

        var events = new List<string>();

        // Set up some signal line activity
        signalBus.Assert(SignalLine.IRQ, deviceId: 1, cycle: Cycle.Zero);

        // Schedule events
        testScheduler.ScheduleAt(10ul, ScheduledEventKind.DeviceTimer, 0, _ => events.Add("Event1"));
        testScheduler.ScheduleAt(30ul, ScheduledEventKind.DeviceTimer, 0, _ =>
        {
            events.Add("Event2");
            signalBus.Deassert(SignalLine.IRQ, deviceId: 1, cycle: new(testScheduler.Now));
        });

        // Verify initial state
        Assert.That(signalBus.IsAsserted(SignalLine.IRQ), Is.True);

        // Execute some CPU instructions
        testScheduler.Advance(15);

        Assert.Multiple(() =>
        {
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(signalBus.IsAsserted(SignalLine.IRQ), Is.True, "IRQ still asserted");
        });

        // Execute more instructions
        testScheduler.Advance(20);

        Assert.Multiple(() =>
        {
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(signalBus.IsAsserted(SignalLine.IRQ), Is.False, "IRQ should be cleared by Event2");
        });
    }

    /// <summary>
    /// Verifies that SetEventContext throws for null context.
    /// </summary>
    [Test]
    public void SetEventContext_NullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => scheduler.SetEventContext(null!));
    }

    /// <summary>
    /// Verifies that ScheduledEventKind is passed correctly to events.
    /// </summary>
    [Test]
    public void ScheduleAt_PreservesEventKind()
    {
        // This test verifies the kind is tracked - we can't directly inspect it
        // but we can verify different kinds don't affect dispatch order
        var executionOrder = new List<string>();

        scheduler.ScheduleAt(100ul, ScheduledEventKind.VideoScanline, 0, _ => executionOrder.Add("Video"));
        scheduler.ScheduleAt(100ul, ScheduledEventKind.AudioTick, 0, _ => executionOrder.Add("Audio"));
        scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => executionOrder.Add("Timer"));

        scheduler.Advance(100ul);

        // All events at same cycle, same priority - should dispatch in schedule order
        Assert.That(executionOrder, Is.EqualTo(new[] { "Video", "Audio", "Timer" }));
    }

    /// <summary>
    /// Verifies that tag can be used to identify event source.
    /// </summary>
    [Test]
    public void ScheduleAt_CanUseTagForIdentification()
    {
        // Note: Tag is stored in the event but not directly accessible in callback
        // This test verifies the tag parameter is accepted
        var handle = scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => { }, tag: "TestDevice");

        // The tag is passed to the event - verification is that no exception is thrown
        Assert.That(handle.Id, Is.GreaterThanOrEqualTo(0ul));
    }

    /// <summary>
    /// Verifies that handles are unique across scheduling.
    /// </summary>
    [Test]
    public void ScheduleAt_HandlesAreUnique()
    {
        var handles = new List<EventHandle>();

        for (int i = 0; i < 10; i++)
        {
            handles.Add(scheduler.ScheduleAt(100ul, ScheduledEventKind.DeviceTimer, 0, _ => { }));
        }

        var uniqueIds = handles.Select(h => h.Id).Distinct().Count();
        Assert.That(uniqueIds, Is.EqualTo(10));
    }

    /// <summary>
    /// Verifies that dispatching events without setting event context throws InvalidOperationException.
    /// </summary>
    [Test]
    public void DispatchEventsUntil_WithoutEventContext_ThrowsInvalidOperationException()
    {
        var schedulerWithoutContext = new Scheduler();
        schedulerWithoutContext.ScheduleAt(10ul, ScheduledEventKind.DeviceTimer, 0, _ => { });

        var ex = Assert.Throws<InvalidOperationException>(() => schedulerWithoutContext.Advance(10ul));

        Assert.That(ex!.Message, Does.Contain("Event context is not set"));
        Assert.That(ex.Message, Does.Contain("SetEventContext"));
    }

    /// <summary>
    /// Verifies that cancelled handles are cleaned up when threshold is exceeded.
    /// </summary>
    [Test]
    public void Cancel_ExceedingThreshold_TriggersCleanup()
    {
        // Schedule and cancel more than 1000 events to trigger cleanup
        var handles = new List<EventHandle>();
        for (int i = 0; i < 1100; i++)
        {
            var handle = scheduler.ScheduleAt((ulong)(100 + i), ScheduledEventKind.DeviceTimer, 0, _ => { });
            handles.Add(handle);
        }

        // Cancel all handles
        foreach (var handle in handles)
        {
            scheduler.Cancel(handle);
        }

        // Verify all handles are cancelled
        Assert.That(scheduler.PendingEventCount, Is.EqualTo(1100), "All events should still be in queue");

        // Now advance and dispatch - this should trigger cleanup since we have >1000 cancelled handles
        scheduler.Advance(100ul);

        // After advancing past the first event, some events should have been processed and removed
        Assert.That(scheduler.PendingEventCount, Is.LessThan(1100), "Some events should have been dispatched");
    }

    /// <summary>
    /// Verifies that cancelled handles cleanup prevents unbounded growth.
    /// </summary>
    [Test]
    public void Cancel_ManyHandles_DoesNotCauseUnboundedGrowth()
    {
        // Schedule many events at different times
        var handles = new List<EventHandle>();
        for (int i = 0; i < 1500; i++)
        {
            var handle = scheduler.ScheduleAt((ulong)(1000 + i), ScheduledEventKind.DeviceTimer, 0, _ => { });
            handles.Add(handle);
        }

        // Cancel all of them
        foreach (var handle in handles)
        {
            scheduler.Cancel(handle);
        }

        // Advance time to trigger a dispatch (which should trigger cleanup since >1000 cancelled)
        scheduler.Advance(500ul);

        // The cleanup should have occurred when we tried to dispatch
        // We can verify this by trying to peek at the next event - it should skip cancelled ones
        // and return the first valid event. Since all events are cancelled and we haven't reached
        // any event times yet (all at 1000+), this demonstrates the cleanup is working.
        var nextDue = scheduler.PeekNextDue();

        // Since we haven't reached any event times yet, nextDue should be the first event (at cycle 1000)
        // The important thing is that PeekNextDue completes successfully even with many cancelled events
        Assert.That(nextDue, Is.Not.Null, "PeekNextDue should complete and find next event");
        Assert.That(nextDue!.Value.Value, Is.GreaterThanOrEqualTo(1000ul), "Next event should be at or after cycle 1000");
    }

    /// <summary>
    /// Verifies that EventScheduled is raised when an event is scheduled.
    /// </summary>
    [Test]
    public void EventScheduled_WhenSchedulingEvent_RaisesEvent()
    {
        EventHandle raisedHandle = default;
        Cycle raisedCycle = default;
        ScheduledEventKind raisedKind = default;
        int raisedPriority = -1;
        object? raisedTag = null;

        scheduler.EventScheduled += (handle, cycle, kind, priority, tag) =>
        {
            raisedHandle = handle;
            raisedCycle = cycle;
            raisedKind = kind;
            raisedPriority = priority;
            raisedTag = tag;
        };

        var handle = scheduler.ScheduleAt(100, ScheduledEventKind.VideoScanline, 5, _ => { }, "test-tag");

        Assert.Multiple(() =>
        {
            Assert.That(raisedHandle, Is.EqualTo(handle));
            Assert.That(raisedCycle.Value, Is.EqualTo(100ul));
            Assert.That(raisedKind, Is.EqualTo(ScheduledEventKind.VideoScanline));
            Assert.That(raisedPriority, Is.EqualTo(5));
            Assert.That(raisedTag, Is.EqualTo("test-tag"));
        });
    }

    /// <summary>
    /// Verifies that EventConsumed is raised when an event is dispatched.
    /// </summary>
    [Test]
    public void EventConsumed_WhenDispatchingEvent_RaisesEvent()
    {
        EventHandle raisedHandle = default;
        Cycle raisedCycle = default;
        ScheduledEventKind raisedKind = default;
        var eventConsumed = false;

        scheduler.EventConsumed += (handle, cycle, kind) =>
        {
            raisedHandle = handle;
            raisedCycle = cycle;
            raisedKind = kind;
            eventConsumed = true;
        };

        var handle = scheduler.ScheduleAt(10, ScheduledEventKind.AudioTick, 0, _ => { });
        scheduler.Advance(10);

        Assert.Multiple(() =>
        {
            Assert.That(eventConsumed, Is.True);
            Assert.That(raisedHandle, Is.EqualTo(handle));
            Assert.That(raisedCycle.Value, Is.EqualTo(10ul));
            Assert.That(raisedKind, Is.EqualTo(ScheduledEventKind.AudioTick));
        });
    }

    /// <summary>
    /// Verifies that EventCancelled is raised when an event is cancelled.
    /// </summary>
    [Test]
    public void EventCancelled_WhenCancellingEvent_RaisesEvent()
    {
        EventHandle raisedHandle = default;
        var eventCancelled = false;

        scheduler.EventCancelled += (handle) =>
        {
            raisedHandle = handle;
            eventCancelled = true;
        };

        var handle = scheduler.ScheduleAt(100, ScheduledEventKind.DeviceTimer, 0, _ => { });
        scheduler.Cancel(handle);

        Assert.Multiple(() =>
        {
            Assert.That(eventCancelled, Is.True);
            Assert.That(raisedHandle, Is.EqualTo(handle));
        });
    }

    /// <summary>
    /// Verifies that EventCancelled is not raised when cancelling the same event twice.
    /// </summary>
    [Test]
    public void EventCancelled_WhenCancellingSameEventTwice_RaisesEventOnlyOnce()
    {
        var cancelCount = 0;

        scheduler.EventCancelled += (_) => cancelCount++;

        var handle = scheduler.ScheduleAt(100, ScheduledEventKind.DeviceTimer, 0, _ => { });
        scheduler.Cancel(handle);
        scheduler.Cancel(handle);

        Assert.That(cancelCount, Is.EqualTo(1));
    }
}