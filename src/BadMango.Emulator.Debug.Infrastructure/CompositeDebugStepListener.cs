// <copyright file="CompositeDebugStepListener.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using Core.Debugger;
using Core.Interfaces.Debugging;

/// <summary>
/// An <see cref="IDebugStepListener"/> that fans out step notifications to
/// multiple registered listeners.
/// </summary>
/// <remarks>
/// <para>
/// The CPU only exposes a single debugger attachment slot via
/// <see cref="Core.Interfaces.Cpu.ICpu.AttachDebugger(IDebugStepListener)"/>. This
/// composite allows multiple debug-side observers (for example, the trace listener
/// and a watchpoint manager) to share that slot without interfering with one another.
/// </para>
/// <para>
/// Listeners are invoked in registration order. Exceptions raised by one listener
/// are not caught — they will propagate to the CPU step loop, which is the desired
/// behavior for debugger plumbing where a misbehaving listener is itself a bug.
/// </para>
/// <para>
/// The step callbacks run once per emulated instruction, so the listener set is
/// stored copy-on-write: <see cref="Add"/> and <see cref="Remove"/> rebuild an
/// immutable snapshot array under a lock, and <see cref="OnBeforeStep"/> /
/// <see cref="OnAfterStep"/> read that snapshot without locking or allocating.
/// A listener added or removed while a step is being dispatched takes effect on
/// the next dispatch.
/// </para>
/// <para>
/// Listeners implementing <see cref="IActivatableDebugStepListener"/> are only
/// included in the dispatch snapshot while they report
/// <see cref="IActivatableDebugStepListener.IsActive"/>; other listeners are always
/// dispatched. <see cref="HasActiveListeners"/> and <see cref="ActiveListenersChanged"/>
/// let the owner attach the composite to the CPU only while something needs it.
/// </para>
/// </remarks>
public sealed class CompositeDebugStepListener : IDebugStepListener
{
    private readonly Lock syncLock = new();
    private readonly List<IDebugStepListener> listeners = [];

    /// <summary>
    /// Cached delegate so subscribe/unsubscribe use the same instance.
    /// </summary>
    private readonly EventHandler activityHandler;

    /// <summary>
    /// Immutable snapshot of the currently active listeners, read by the
    /// per-instruction callbacks. Replaced (never mutated) whenever the listener
    /// set or any listener's activity changes.
    /// </summary>
    private volatile IDebugStepListener[] snapshot = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeDebugStepListener"/> class.
    /// </summary>
    public CompositeDebugStepListener()
    {
        activityHandler = OnListenerActivityChanged;
    }

    /// <summary>
    /// Raised when <see cref="HasActiveListeners"/> changes value.
    /// </summary>
    /// <remarks>
    /// Raised on the thread that caused the change (an add/remove, or a listener
    /// raising <see cref="IActivatableDebugStepListener.IsActiveChanged"/>), outside
    /// the composite's internal lock. Handlers should re-read
    /// <see cref="HasActiveListeners"/>.
    /// </remarks>
    public event EventHandler? ActiveListenersChanged;

    /// <summary>
    /// Gets the number of registered listeners, active or not.
    /// </summary>
    public int Count
    {
        get
        {
            lock (syncLock)
            {
                return listeners.Count;
            }
        }
    }

    /// <summary>
    /// Gets the number of registered listeners that are currently active and
    /// therefore receive step callbacks.
    /// </summary>
    public int ActiveCount => snapshot.Length;

    /// <summary>
    /// Gets a value indicating whether at least one registered listener is active.
    /// </summary>
    public bool HasActiveListeners => snapshot.Length > 0;

    /// <summary>
    /// Adds a listener to the composite.
    /// </summary>
    /// <param name="listener">The listener to add.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="listener"/> is <see langword="null"/>.
    /// </exception>
    public void Add(IDebugStepListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        bool changed;
        lock (syncLock)
        {
            if (listeners.Contains(listener))
            {
                return;
            }

            listeners.Add(listener);
            if (listener is IActivatableDebugStepListener activatable)
            {
                activatable.IsActiveChanged += activityHandler;
            }

            changed = RebuildSnapshot();
        }

        RaiseIfChanged(changed);
    }

    /// <summary>
    /// Removes a listener from the composite.
    /// </summary>
    /// <param name="listener">The listener to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the listener was present and removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Remove(IDebugStepListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        bool changed;
        lock (syncLock)
        {
            if (!listeners.Remove(listener))
            {
                return false;
            }

            if (listener is IActivatableDebugStepListener activatable)
            {
                activatable.IsActiveChanged -= activityHandler;
            }

            changed = RebuildSnapshot();
        }

        RaiseIfChanged(changed);
        return true;
    }

    /// <inheritdoc/>
    public void OnBeforeStep(in DebugStepEventArgs eventData)
    {
        // Lock-free, allocation-free: read the current immutable snapshot once.
        var current = snapshot;
        for (int i = 0; i < current.Length; i++)
        {
            current[i].OnBeforeStep(in eventData);
        }
    }

    /// <inheritdoc/>
    public void OnAfterStep(in DebugStepEventArgs eventData)
    {
        // Lock-free, allocation-free: read the current immutable snapshot once.
        var current = snapshot;
        for (int i = 0; i < current.Length; i++)
        {
            current[i].OnAfterStep(in eventData);
        }
    }

    private static bool IsListenerActive(IDebugStepListener listener)
        => listener is not IActivatableDebugStepListener activatable || activatable.IsActive;

    private void OnListenerActivityChanged(object? sender, EventArgs e)
    {
        bool changed;
        lock (syncLock)
        {
            changed = RebuildSnapshot();
        }

        RaiseIfChanged(changed);
    }

    /// <summary>
    /// Rebuilds the active-listener snapshot. Must be called under <see cref="syncLock"/>.
    /// </summary>
    /// <returns><see langword="true"/> if <see cref="HasActiveListeners"/> changed.</returns>
    private bool RebuildSnapshot()
    {
        bool hadActive = snapshot.Length > 0;
        snapshot = [.. listeners.Where(IsListenerActive)];
        return hadActive != (snapshot.Length > 0);
    }

    private void RaiseIfChanged(bool changed)
    {
        if (changed)
        {
            ActiveListenersChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}