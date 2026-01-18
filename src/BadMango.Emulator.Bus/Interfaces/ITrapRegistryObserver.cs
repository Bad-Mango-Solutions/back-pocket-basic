// <copyright file="ITrapRegistryObserver.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Interfaces.Cpu;

/// <summary>
/// Provides observation events for trap registry activity.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables monitoring tools (like TrapMonitor) to observe trap lifecycle
/// and invocation events without modifying the core trap registry logic. Implementations
/// can subscribe to events to track when traps are registered, unregistered, and invoked.
/// </para>
/// <para>
/// <b>Performance Note:</b> Trap execution is on the hot path when traps are enabled.
/// Observers should minimize the work done in event handlers to avoid impacting performance.
/// Consider buffering events for batch processing in the UI thread.
/// </para>
/// </remarks>
public interface ITrapRegistryObserver
{
    /// <summary>
    /// Occurs when a trap is registered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Event parameter is the <see cref="TrapInfo"/> of the newly registered trap.
    /// </para>
    /// </remarks>
    event Action<TrapInfo>? TrapRegistered;

    /// <summary>
    /// Occurs when a trap is unregistered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Event parameters are:
    /// <list type="bullet">
    /// <item><description><see cref="Addr"/>: The address of the unregistered trap.</description></item>
    /// <item><description><see cref="TrapOperation"/>: The operation type of the unregistered trap.</description></item>
    /// <item><description><see cref="MemoryContext"/>: The memory context of the unregistered trap.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    event Action<Addr, TrapOperation, MemoryContext>? TrapUnregistered;

    /// <summary>
    /// Occurs when a trap is invoked (executed).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Event parameters are:
    /// <list type="bullet">
    /// <item><description><see cref="TrapInfo"/>: Information about the invoked trap.</description></item>
    /// <item><description><see cref="TrapResult"/>: The result of the trap invocation.</description></item>
    /// <item><description><see cref="Cycle"/>: The cycle when the trap was invoked.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    event Action<TrapInfo, TrapResult, Cycle>? TrapInvoked;

    /// <summary>
    /// Occurs when a trap's enabled state changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Event parameters are:
    /// <list type="bullet">
    /// <item><description><see cref="Addr"/>: The address of the trap.</description></item>
    /// <item><description><see cref="TrapOperation"/>: The operation type of the trap.</description></item>
    /// <item><description><see cref="MemoryContext"/>: The memory context of the trap.</description></item>
    /// <item><description><see cref="bool"/>: The new enabled state.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    event Action<Addr, TrapOperation, MemoryContext, bool>? TrapEnabledChanged;
}