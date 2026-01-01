// <copyright file="InstructionTrace.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

using System.Runtime.InteropServices;

/// <summary>
/// Represents a trace of a CPU instruction execution, capturing details such as the opcode,
/// addressing mode, operands, and execution cycles.
/// </summary>
/// <remarks>
/// This record is used to log and analyze the behavior of the CPU during emulation,
/// providing insights into the executed instructions and their associated data.
/// Use the <c>with</c> keyword to create modified copies of this immutable struct.
/// </remarks>
/// <param name="StartPC">The starting program counter (PC) value for the instruction.</param>
/// <param name="OpCode">The opcode of the instruction.</param>
/// <param name="Instruction">The instruction being executed.</param>
/// <param name="AddressingMode">The addressing mode used by the instruction.</param>
/// <param name="OperandSize">The size of the operands used by the instruction.</param>
/// <param name="Operands">The operands used by the instruction.</param>
/// <param name="EffectiveAddress">The effective address calculated for the instruction, if any.</param>
/// <param name="StartCycle">The cycle in which the instruction started execution.</param>
/// <param name="InstructionCycles">The number of cycles taken to execute the instruction.</param>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct InstructionTrace(
    Addr StartPC,
    OpcodeBuffer OpCode,
    CpuInstructions Instruction,
    CpuAddressingModes AddressingMode,
    byte OperandSize,
    OperandBuffer Operands,
    Addr EffectiveAddress,
    Cycle StartCycle,
    Cycle InstructionCycles);