// <copyright file="TrapOperation.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Specifies the type of memory operation that triggers a trap.
/// </summary>
/// <remarks>
/// <para>
/// Traps can be triggered by different types of memory operations:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Read"/> - Triggered when the address is read (LDA, LDX, etc.).</description></item>
/// <item><description><see cref="Write"/> - Triggered when the address is written (STA, STX, etc.).</description></item>
/// <item><description><see cref="Call"/> - Triggered when execution reaches the address (JSR, JMP, or linear flow).</description></item>
/// </list>
/// <para>
/// <b>Call Trap Mechanics:</b>
/// </para>
/// <para>
/// Call traps are invoked during instruction fetch when the Program Counter reaches
/// the trapped address. The behavior depends on how execution arrived at the address:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>JSR/JSL (Jump to Subroutine):</b> The return address is already on the stack.
/// The trap handler can use <see cref="TrapResult.ReturnAddress"/> set to <see langword="null"/>
/// to indicate the CPU should simulate RTS (pull return address from stack and continue).
/// Alternatively, the handler can set a specific address to redirect execution.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>JMP/JML (Jump):</b> No return address is pushed. The handler typically performs
/// the operation and then either continues at the next instruction or redirects to
/// a specific address. There is no automatic RTS behavior.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Linear Flow:</b> Execution arrived via sequential instruction execution (e.g.,
/// falling through from a previous instruction). Treated the same as JMP.
/// </description>
/// </item>
/// </list>
/// <para>
/// To distinguish between JSR and JMP, the trap handler can examine the CPU state:
/// check if the previous instruction was JSR by inspecting the stack or maintaining
/// context. Most trap handlers don't need this distinction as they simply perform
/// the ROM routine's function and return.
/// </para>
/// </remarks>
public enum TrapOperation
{
    /// <summary>
    /// Trap triggered on memory read operations.
    /// </summary>
    /// <remarks>
    /// Used for trapping soft switch reads, I/O port reads, or implementing
    /// read-triggered side effects without requiring a full memory device.
    /// </remarks>
    Read,

    /// <summary>
    /// Trap triggered on memory write operations.
    /// </summary>
    /// <remarks>
    /// Used for trapping soft switch writes, I/O port writes, or implementing
    /// write-triggered side effects without requiring a full memory device.
    /// </remarks>
    Write,

    /// <summary>
    /// Trap triggered when execution reaches the address.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call traps intercept execution at ROM entry points, allowing native
    /// implementations of ROM routines for performance optimization.
    /// </para>
    /// <para>
    /// The trap is triggered during instruction fetch, before the instruction
    /// at the trapped address executes. This allows the handler to completely
    /// replace the ROM routine's behavior.
    /// </para>
    /// </remarks>
    Call,
}