// <copyright file="TraceRecord.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using Core;
using Core.Cpu;

/// <summary>
/// Represents a single trace record capturing the state after an instruction executes.
/// </summary>
public struct TraceRecord
{
    /// <summary>Gets or sets the program counter before the instruction executed.</summary>
    public uint PC;

    /// <summary>Gets or sets the opcode byte.</summary>
    public byte Opcode;

    /// <summary>Gets or sets the instruction mnemonic.</summary>
    public CpuInstructions Instruction;

    /// <summary>Gets or sets the addressing mode.</summary>
    public CpuAddressingModes AddressingMode;

    /// <summary>Gets or sets the operand bytes.</summary>
    public OperandBuffer Operands;

    /// <summary>Gets or sets the number of operand bytes.</summary>
    public byte OperandSize;

    /// <summary>Gets or sets the effective address (for memory operations).</summary>
    public uint EffectiveAddress;

    /// <summary>Gets or sets the accumulator value after execution.</summary>
    public byte A;

    /// <summary>Gets or sets the X register value after execution.</summary>
    public byte X;

    /// <summary>Gets or sets the Y register value after execution.</summary>
    public byte Y;

    /// <summary>Gets or sets the stack pointer value after execution.</summary>
    public byte SP;

    /// <summary>Gets or sets the processor status flags after execution.</summary>
    public ProcessorStatusFlags P;

    /// <summary>Gets or sets the total cycle count after execution.</summary>
    public ulong Cycles;

    /// <summary>Gets or sets the number of cycles this instruction took.</summary>
    public byte InstructionCycles;

    /// <summary>Gets or sets a value indicating whether the CPU is halted.</summary>
    public bool Halted;

    /// <summary>Gets or sets the halt reason if the CPU is halted.</summary>
    public HaltState HaltReason;
}