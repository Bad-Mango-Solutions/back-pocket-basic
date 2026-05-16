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
/// </remarks>
public sealed class CompositeDebugStepListener : IDebugStepListener
{
    private readonly Lock syncLock = new();
    private readonly List<IDebugStepListener> listeners = [];

    /// <summary>
    /// Gets the number of registered listeners.
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
            return listeners.Remove(listener);
        }
    }

    /// <inheritdoc/>
    public void OnBeforeStep(in DebugStepEventArgs eventData)
    {
        IDebugStepListener[] snapshot;
        lock (syncLock)
        {
            if (listeners.Count == 0)
            {
                return;
            }

            snapshot = [.. listeners];
        }

        foreach (var l in snapshot)
        {
            l.OnBeforeStep(in eventData);
        }
    }

    /// <inheritdoc/>
    public void OnAfterStep(in DebugStepEventArgs eventData)
    {
        IDebugStepListener[] snapshot;
        lock (syncLock)
        {
            if (listeners.Count == 0)
            {
                return;
            }

            snapshot = [.. listeners];
        }

        foreach (var l in snapshot)
        {
            l.OnAfterStep(in eventData);
        }
    }
}