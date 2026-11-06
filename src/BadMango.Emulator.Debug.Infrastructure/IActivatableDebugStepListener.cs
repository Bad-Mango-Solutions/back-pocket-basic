// <copyright file="IActivatableDebugStepListener.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using Core.Interfaces.Debugging;

/// <summary>
/// An <see cref="IDebugStepListener"/> that can be registered but idle, and
/// reports when it starts or stops needing per-instruction step callbacks.
/// </summary>
/// <remarks>
/// <para>
/// Debug step callbacks are expensive: while any listener is attached to the CPU,
/// every instruction builds an instruction trace and step event arguments. Listeners
/// such as the tracing listener (when tracing is disabled) or the watchpoint manager
/// (when no watchpoints exist) do not consume that data, so
/// <see cref="CompositeDebugStepListener"/> skips inactive listeners, and
/// <see cref="Commands.DebugContext"/> only attaches the composite to the CPU while
/// at least one of its listeners is active.
/// </para>
/// <para>
/// Listeners that do not implement this interface are treated as always active.
/// </para>
/// </remarks>
public interface IActivatableDebugStepListener : IDebugStepListener
{
    /// <summary>
    /// Raised after <see cref="IsActive"/> may have changed.
    /// </summary>
    /// <remarks>
    /// May be raised on any thread. Handlers should re-read <see cref="IsActive"/>
    /// rather than assume a direction. Spurious notifications are allowed.
    /// </remarks>
    event EventHandler? IsActiveChanged;

    /// <summary>
    /// Gets a value indicating whether this listener currently needs step callbacks.
    /// </summary>
    bool IsActive { get; }
}