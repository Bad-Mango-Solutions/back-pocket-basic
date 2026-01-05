// <copyright file="TrapReturnMethod.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Specifies how the CPU should return after handling a trap.
/// </summary>
/// <remarks>
/// <para>
/// The return method depends on how execution arrived at the trapped address:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>JSR/JSL:</b> Use <see cref="Rts"/> - the return address is on the stack.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>JMP/JML or linear flow:</b> Use <see cref="None"/> and set
/// <see cref="TrapResult.ReturnAddress"/> to specify continuation.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>IRQ/NMI/BRK:</b> Use <see cref="Rti"/> - PC and status are on the stack.
/// </description>
/// </item>
/// </list>
/// </remarks>
public enum TrapReturnMethod
{
    /// <summary>
    /// Return via RTS instruction (pull address from stack, add 1).
    /// Use when the trap was reached via JSR.
    /// </summary>
    Rts,

    /// <summary>
    /// Return via RTI instruction (pull status and PC from stack).
    /// Use when the trap was reached via IRQ, NMI, or BRK.
    /// </summary>
    Rti,

    /// <summary>
    /// No automatic return - the trap handler manages the return itself.
    /// Use when the trap was reached via JMP or when the handler
    /// specifies <see cref="TrapResult.ReturnAddress"/> directly.
    /// </summary>
    None,
}