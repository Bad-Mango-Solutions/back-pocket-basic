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
/// </remarks>
public sealed class CompositeDebugStepListener : IDebugStepListener
{
    private readonly Lock syncLock = new();
    private readonly List<IDebugStepListener> listeners = [];

    /// <summary>
    /// Immutable snapshot of <see cref="listeners"/> read by the per-instruction
    /// callbacks. Replaced (never mutated) whenever the listener set changes.
    /// </summary>
    private volatile IDebugStepListener[] snapshot = [];

    /// <summary>
    /// Gets the number of registered listeners.
    /// </summary>
    public int Count => snapshot.Length;

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
        lock (syncLock)
        {
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
                snapshot = [.. listeners];
            }
        }
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
        lock (syncLock)
        {
            if (!listeners.Remove(listener))
            {
                return false;
            }

            snapshot = [.. listeners];
            return true;
        }
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
}