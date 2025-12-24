// <copyright file="IDebugStepListener.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

/// <summary>
/// Interface for receiving debug step notifications from the CPU.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface receive callbacks when the CPU executes
/// instructions while a debugger is attached. The callback provides minimal
/// data for performance since it's called from a hot loop.
/// </para>
/// <para>
/// The listener receives terse data (PC, opcode, operand bytes, register snapshots)
/// that can be formatted by the debug console into human-readable output like:
/// <code>
/// $1000: A9 42    LDA #$42       ; A=$42
/// $1002: 8D 00 02 STA $0200      ; [$0200]=$42
/// </code>
/// </para>
/// </remarks>
public interface IDebugStepListener
{
    /// <summary>
    /// Called before an instruction is executed.
    /// </summary>
    /// <param name="eventData">The step event data containing PC, opcode, and operand information.</param>
    void OnBeforeStep(in DebugStepEventArgs eventData);

    /// <summary>
    /// Called after an instruction is executed.
    /// </summary>
    /// <param name="eventData">The step event data containing updated register state.</param>
    void OnAfterStep(in DebugStepEventArgs eventData);
}