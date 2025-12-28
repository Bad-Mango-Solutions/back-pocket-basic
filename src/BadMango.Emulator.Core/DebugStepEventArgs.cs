// <copyright file="DebugStepEventArgs.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.InteropServices;

using Cpu;

/// <summary>
/// Provides data for debug step events. Designed for minimal overhead in hot loop.
/// </summary>
/// <remarks>
/// <para>
/// This structure is passed by reference to avoid allocations during instruction
/// execution. It contains the raw data needed by the debug console to format
/// disassembly output, but does not perform any formatting itself.
/// </para>
/// <para>
/// The debug console uses the opcode and operand bytes along with an opcode
/// table to determine instruction mnemonics and addressing modes for formatting.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DebugStepEventArgs
{
    /// <summary>
    /// Gets or sets the program counter value before the instruction was fetched.
    /// </summary>
    public Addr PC;

    /// <summary>
    /// Gets or sets the opcode byte that was executed.
    /// </summary>
    public byte Opcode;

    /// <summary>
    /// Gets or sets the instruction mnemonic that was executed.
    /// </summary>
    public CpuInstructions Instruction;

    /// <summary>
    /// Gets or sets the addressing mode used by the instruction.
    /// </summary>
    public CpuAddressingModes AddressingMode;

    /// <summary>
    /// Gets or sets the number of operand bytes (0, 1, or 2).
    /// </summary>
    public byte OperandSize;

    /// <summary>
    /// Gets or sets the operand bytes (up to 4 bytes for future 65816/65832 support).
    /// </summary>
    public OperandBuffer Operands;

    /// <summary>
    /// Gets or sets the effective address computed by the addressing mode (if applicable).
    /// </summary>
    public Addr EffectiveAddress;

    /// <summary>
    /// Gets or sets a snapshot of the registers after instruction execution.
    /// </summary>
    public Registers Registers;

    /// <summary>
    /// Gets or sets the total cycle count after instruction execution.
    /// </summary>
    public ulong Cycles;

    /// <summary>
    /// Gets or sets the number of cycles this instruction took to execute.
    /// </summary>
    public byte InstructionCycles;

    /// <summary>
    /// Gets or sets a value indicating whether the CPU is now halted.
    /// </summary>
    public bool Halted;

    /// <summary>
    /// Gets or sets the halt reason if the CPU is halted.
    /// </summary>
    public HaltState HaltReason;
}