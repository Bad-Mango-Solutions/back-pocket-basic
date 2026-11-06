// <copyright file="CompositeDebugStepListenerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using Core.Debugger;
using Core.Interfaces.Debugging;

/// <summary>
/// Unit tests for the <see cref="CompositeDebugStepListener"/> class.
/// </summary>
[TestFixture]
public class CompositeDebugStepListenerTests
{
    /// <summary>
    /// Verifies that an empty composite dispatches without error.
    /// </summary>
    [Test]
    public void EmptyComposite_DispatchIsNoOp()
    {
        var composite = new CompositeDebugStepListener();
        var args = new DebugStepEventArgs { PC = 0x1234 };

        Assert.Multiple(() =>
        {
            Assert.That(composite.Count, Is.Zero);
            Assert.DoesNotThrow(() => composite.OnBeforeStep(in args));
            Assert.DoesNotThrow(() => composite.OnAfterStep(in args));
        });
    }

    /// <summary>
    /// Verifies that both callbacks fan out to every listener in registration order.
    /// </summary>
    [Test]
    public void Dispatch_InvokesListenersInRegistrationOrder()
    {
        var log = new List<string>();
        var composite = new CompositeDebugStepListener();
        composite.Add(new RecordingListener("a", log));
        composite.Add(new RecordingListener("b", log));
        var args = new DebugStepEventArgs { PC = 0x0300 };

        composite.OnBeforeStep(in args);
        composite.OnAfterStep(in args);

        Assert.That(log, Is.EqualTo(new[] { "a:before:0300", "b:before:0300", "a:after:0300", "b:after:0300" }));
    }

    /// <summary>
    /// Verifies that adding the same listener twice registers it once.
    /// </summary>
    [Test]
    public void Add_Duplicate_IsIgnored()
    {
        var log = new List<string>();
        var composite = new CompositeDebugStepListener();
        var listener = new RecordingListener("a", log);

        composite.Add(listener);
        composite.Add(listener);
        var args = default(DebugStepEventArgs);
        composite.OnAfterStep(in args);

        Assert.Multiple(() =>
        {
            Assert.That(composite.Count, Is.EqualTo(1));
            Assert.That(log, Has.Count.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that a removed listener no longer receives callbacks and that
    /// removing an unknown listener returns false.
    /// </summary>
    [Test]
    public void Remove_StopsDispatchToListener()
    {
        var log = new List<string>();
        var composite = new CompositeDebugStepListener();
        var a = new RecordingListener("a", log);
        var b = new RecordingListener("b", log);
        composite.Add(a);
        composite.Add(b);

        bool removed = composite.Remove(a);
        bool removedAgain = composite.Remove(a);
        var args = default(DebugStepEventArgs);
        composite.OnAfterStep(in args);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(removedAgain, Is.False);
            Assert.That(composite.Count, Is.EqualTo(1));
            Assert.That(log, Is.EqualTo(new[] { "b:after:0000" }));
        });
    }

    /// <summary>
    /// Verifies that a listener added during dispatch is not invoked for the
    /// in-flight dispatch but is invoked on the next one (copy-on-write snapshot).
    /// </summary>
    [Test]
    public void Add_DuringDispatch_TakesEffectOnNextDispatch()
    {
        var log = new List<string>();
        var composite = new CompositeDebugStepListener();
        var late = new RecordingListener("late", log);
        composite.Add(new CallbackListener(() => composite.Add(late)));
        var args = default(DebugStepEventArgs);

        composite.OnAfterStep(in args);
        Assert.That(log, Is.Empty, "listener added mid-dispatch must not run in that dispatch");

        composite.OnAfterStep(in args);
        Assert.That(log, Is.EqualTo(new[] { "late:after:0000" }));
    }

    /// <summary>
    /// Verifies that a listener removed during dispatch still completes the
    /// in-flight dispatch without error and is skipped afterwards.
    /// </summary>
    [Test]
    public void Remove_DuringDispatch_TakesEffectOnNextDispatch()
    {
        var log = new List<string>();
        var composite = new CompositeDebugStepListener();
        var victim = new RecordingListener("victim", log);
        composite.Add(new CallbackListener(() => composite.Remove(victim)));
        composite.Add(victim);
        var args = default(DebugStepEventArgs);

        composite.OnAfterStep(in args);
        composite.OnAfterStep(in args);

        Assert.That(log, Is.EqualTo(new[] { "victim:after:0000" }));
    }

    /// <summary>
    /// Verifies that steady-state dispatch does not allocate.
    /// </summary>
    [Test]
    public void Dispatch_DoesNotAllocate()
    {
        const int WarmupIterations = 20_000;
        const int MeasureIterations = 10_000;
        const int SampleCount = 5;
        const int MaxTransientBytes = 1024;

        var composite = new CompositeDebugStepListener();
        composite.Add(new CallbackListener(() => { }));
        composite.Add(new CallbackListener(() => { }));
        var args = default(DebugStepEventArgs);

        // Warm up long enough for tiered JIT to settle.
        for (int i = 0; i < WarmupIterations; i++)
        {
            composite.OnBeforeStep(in args);
            composite.OnAfterStep(in args);
        }

        long bestAllocated = long.MaxValue;
        for (int sample = 0; sample < SampleCount; sample++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < MeasureIterations; i++)
            {
                composite.OnBeforeStep(in args);
                composite.OnAfterStep(in args);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated < bestAllocated)
            {
                bestAllocated = allocated;
            }
        }

        Assert.That(bestAllocated, Is.LessThanOrEqualTo(MaxTransientBytes));
    }

    /// <summary>
    /// Verifies that concurrent add/remove while another thread dispatches does
    /// not throw and leaves the composite in a consistent state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async System.Threading.Tasks.Task ConcurrentMutationAndDispatch_IsSafe()
    {
        var composite = new CompositeDebugStepListener();
        var listeners = Enumerable.Range(0, 8).Select(_ => new CallbackListener(() => { })).ToArray();
        using var cts = new CancellationTokenSource();

        var dispatcher = Task.Run(() =>
        {
            var args = default(DebugStepEventArgs);
            while (!cts.IsCancellationRequested)
            {
                composite.OnBeforeStep(in args);
                composite.OnAfterStep(in args);
            }
        });

        for (int round = 0; round < 2000; round++)
        {
            var l = listeners[round % listeners.Length];
            composite.Add(l);
            if (round % 3 == 0)
            {
                composite.Remove(l);
            }
        }

        cts.Cancel();
        await Assert.DoesNotThrowAsync(async () => await dispatcher);

        foreach (var l in listeners)
        {
            composite.Remove(l);
        }

        Assert.That(composite.Count, Is.Zero);
    }

    /// <summary>
    /// Verifies that inactive activatable listeners are registered but not dispatched,
    /// and become dispatched when they turn active.
    /// </summary>
    [Test]
    public void ActivatableListener_DispatchedOnlyWhileActive()
    {
        var composite = new CompositeDebugStepListener();
        var toggle = new ToggleListener();
        composite.Add(toggle);
        var args = default(DebugStepEventArgs);

        composite.OnAfterStep(in args);
        Assert.Multiple(() =>
        {
            Assert.That(composite.Count, Is.EqualTo(1));
            Assert.That(composite.ActiveCount, Is.Zero);
            Assert.That(composite.HasActiveListeners, Is.False);
            Assert.That(toggle.AfterCount, Is.Zero);
        });

        toggle.SetActive(true);
        composite.OnAfterStep(in args);
        Assert.Multiple(() =>
        {
            Assert.That(composite.HasActiveListeners, Is.True);
            Assert.That(toggle.AfterCount, Is.EqualTo(1));
        });

        toggle.SetActive(false);
        composite.OnAfterStep(in args);
        Assert.That(toggle.AfterCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that <see cref="CompositeDebugStepListener.ActiveListenersChanged"/>
    /// is raised only when <see cref="CompositeDebugStepListener.HasActiveListeners"/> flips.
    /// </summary>
    [Test]
    public void ActiveListenersChanged_RaisedOnlyOnTransitions()
    {
        var composite = new CompositeDebugStepListener();
        var a = new ToggleListener();
        var b = new ToggleListener();
        var states = new List<bool>();
        composite.ActiveListenersChanged += (_, _) => states.Add(composite.HasActiveListeners);

        composite.Add(a);          // inactive add: no transition
        composite.Add(b);          // inactive add: no transition
        a.SetActive(true);         // false -> true
        b.SetActive(true);         // still true: no event
        a.SetActive(false);        // still true (b): no event
        b.SetActive(false);        // true -> false
        composite.Add(new RecordingListener("plain", [])); // plain listener: false -> true

        Assert.That(states, Is.EqualTo(new[] { true, false, true }));
    }

    /// <summary>
    /// Verifies that a removed activatable listener is unsubscribed and no longer
    /// affects the composite.
    /// </summary>
    [Test]
    public void Remove_ActivatableListener_Unsubscribes()
    {
        var composite = new CompositeDebugStepListener();
        var toggle = new ToggleListener();
        int raised = 0;
        composite.Add(toggle);
        composite.Remove(toggle);
        composite.ActiveListenersChanged += (_, _) => raised++;

        toggle.SetActive(true);

        Assert.Multiple(() =>
        {
            Assert.That(raised, Is.Zero);
            Assert.That(composite.HasActiveListeners, Is.False);
            Assert.That(toggle.SubscriberCount, Is.Zero);
        });
    }

    /// <summary>
    /// Verifies that the real tracing listener and watchpoint manager report activity.
    /// </summary>
    [Test]
    public void TracingAndWatchpoints_ReportActivityToComposite()
    {
        var composite = new CompositeDebugStepListener();
        var tracing = new TracingDebugListener();
        var watch = new WatchpointManager();
        composite.Add(tracing);
        composite.Add(watch);
        Assert.That(composite.HasActiveListeners, Is.False);

        tracing.IsEnabled = true;
        Assert.That(composite.ActiveCount, Is.EqualTo(1));

        watch.Add(0x10, WatchAccess.Read, stopOnHit: false);
        Assert.That(composite.ActiveCount, Is.EqualTo(2));

        tracing.IsEnabled = false;
        watch.Remove(0x10);
        Assert.That(composite.HasActiveListeners, Is.False);
    }

    /// <summary>
    /// Listener that records each callback to a shared log.
    /// </summary>
    private sealed class RecordingListener(string name, List<string> log) : IDebugStepListener
    {
        /// <inheritdoc/>
        public void OnBeforeStep(in DebugStepEventArgs eventData) => log.Add($"{name}:before:{eventData.PC:X4}");

        /// <inheritdoc/>
        public void OnAfterStep(in DebugStepEventArgs eventData) => log.Add($"{name}:after:{eventData.PC:X4}");
    }

    /// <summary>
    /// Listener that runs a callback from <see cref="OnAfterStep"/>.
    /// </summary>
    private sealed class CallbackListener(Action onAfter) : IDebugStepListener
    {
        /// <inheritdoc/>
        public void OnBeforeStep(in DebugStepEventArgs eventData)
        {
        }

        /// <inheritdoc/>
        public void OnAfterStep(in DebugStepEventArgs eventData) => onAfter();
    }

    /// <summary>
    /// Activatable listener whose activity is controlled by the test.
    /// </summary>
    private sealed class ToggleListener : IActivatableDebugStepListener
    {
        private EventHandler? isActiveChanged;

        /// <inheritdoc/>
        public event EventHandler? IsActiveChanged
        {
            add
            {
                isActiveChanged += value;
                SubscriberCount++;
            }

            remove
            {
                isActiveChanged -= value;
                SubscriberCount--;
            }
        }

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets the number of after-step callbacks received.
        /// </summary>
        public int AfterCount { get; private set; }

        /// <summary>
        /// Gets the number of current event subscribers.
        /// </summary>
        public int SubscriberCount { get; private set; }

        /// <summary>
        /// Sets activity and raises the change notification.
        /// </summary>
        /// <param name="active">The new activity state.</param>
        public void SetActive(bool active)
        {
            IsActive = active;
            isActiveChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public void OnBeforeStep(in DebugStepEventArgs eventData)
        {
        }

        /// <inheritdoc/>
        public void OnAfterStep(in DebugStepEventArgs eventData) => AfterCount++;
    }
}