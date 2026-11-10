// <copyright file="IMachineDispatcher.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Marshals work onto the thread that owns a machine's CPU, scheduler, signal bus and devices.
/// </summary>
/// <remarks>
/// <para>
/// The scheduler, signal bus, bus mappings, devices and CPU are single-threaded. While the
/// machine is running they belong to its run loop; while it is stopped or paused they belong
/// to whichever thread drives it (it is quiescent). Host code on other threads, such as the
/// REPL or a UI keyboard handler, must not touch them directly while the machine is running.
/// It posts the work here instead.
/// </para>
/// <para>
/// Devices receive the machine as their <see cref="IEventContext"/>, so they can obtain this
/// dispatcher with <c>context as IMachineDispatcher</c> and fall back to running inline when it
/// is not available (for example in unit tests with a bare event context).
/// </para>
/// </remarks>
public interface IMachineDispatcher
{
    /// <summary>
    /// Gets a value indicating whether the calling thread is currently executing on behalf of
    /// the machine (inside its run loop, a step, a scheduled callback or a posted action).
    /// </summary>
    bool IsOnMachineThread { get; }

    /// <summary>
    /// Runs <paramref name="action"/> on the machine thread.
    /// </summary>
    /// <param name="action">The work to run. It may freely use the scheduler, signals and devices.</param>
    /// <remarks>
    /// <para>
    /// If the caller is already on the machine thread, or the machine has no active run loop,
    /// the action runs inline before this method returns (in the quiescent case no run loop can
    /// start until it finishes). Otherwise it is queued and runs on the run-loop thread at the
    /// next instruction boundary; this method returns without waiting for it.
    /// </para>
    /// <para>
    /// Actions run in the order they were posted.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    void Post(Action action);
}