// <copyright file="MachineState.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents the lifecycle state of a machine instance.
/// </summary>
/// <remarks>
/// <para>
/// Machine state tracks the overall execution status of the emulated system,
/// distinct from CPU-level states like halted or waiting for interrupt.
/// </para>
/// <para>
/// State transitions:
/// <list type="bullet">
/// <item><description><see cref="Stopped"/> → <see cref="Running"/> (via Run)</description></item>
/// <item><description><see cref="Running"/> → <see cref="Stopped"/> (via Stop)</description></item>
/// <item><description><see cref="Stopped"/> → <see cref="Paused"/> (via Step when already stopped)</description></item>
/// <item><description><see cref="Running"/> → <see cref="Paused"/> (via breakpoint or step-mode)</description></item>
/// <item><description><see cref="Paused"/> → <see cref="Running"/> (via Run)</description></item>
/// <item><description><see cref="Paused"/> → <see cref="Stopped"/> (via Stop)</description></item>
/// <item><description>Any state → <see cref="Stopped"/> (via Reset)</description></item>
/// </list>
/// </para>
/// </remarks>
public enum MachineState
{
    /// <summary>
    /// Machine is stopped and not executing instructions.
    /// </summary>
    /// <remarks>
    /// This is the initial state after machine creation and the state
    /// after a reset or explicit stop.
    /// </remarks>
    Stopped,

    /// <summary>
    /// Machine is actively executing instructions in continuous mode.
    /// </summary>
    /// <remarks>
    /// The CPU will execute instructions until stopped, a breakpoint is hit,
    /// or an error occurs.
    /// </remarks>
    Running,

    /// <summary>
    /// Machine execution is paused, typically at a breakpoint or after single-stepping.
    /// </summary>
    /// <remarks>
    /// The machine can be resumed via Run or advanced one instruction via Step.
    /// </remarks>
    Paused,
}