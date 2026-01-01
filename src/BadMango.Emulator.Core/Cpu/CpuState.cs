// <copyright file="CpuState.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Represents the complete state of a 65C02 CPU.
/// </summary>
/// <remarks>
/// This structure captures all CPU registers and execution state for
/// save states, debugging, and state inspection purposes.
/// Uses explicit layout for optimal memory packing.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CpuState
{
    /// <summary>
    /// Gets or sets the CPU registers.
    /// </summary>
    public Registers Registers;

    /// <summary>
    /// Gets or sets the total number of cycles executed.
    /// </summary>
    public ulong Cycles;

    /// <summary>Indicates whether a debugger is currently attached to the CPU.</summary>
    public bool IsDebuggerAttached;

    /// <summary>
    /// The instruction trace containing debug-related state information for the current instruction.
    /// </summary>
    private InstructionTrace trace;

    /// <summary>Initializes a new instance of the <see cref="CpuState"/> struct.</summary>
    public CpuState()
    {
        Registers = default;
        Cycles = 0;
        trace = default;
        ClearDebugStateInformation();
    }

    /// <summary>
    /// Gets the program counter at the start of the instruction.
    /// </summary>
    public Addr StartPC
    {
        readonly get => trace.StartPC;
        set => trace = trace with { StartPC = value };
    }

    /// <summary>
    /// Gets the opcode of the currently executing instruction.
    /// </summary>
    public byte Opcode
    {
        readonly get => trace.OpCode.OpcodeByte;
        set
        {
            var opcode = trace.OpCode;
            opcode[0] = value;
            trace = trace with { OpCode = opcode };
        }
    }

    /// <summary>
    /// Gets the sub-opcode or extension byte for the current instruction, if applicable.
    /// </summary>
    public byte SubOpcode
    {
        readonly get => trace.OpCode.SubOpcodeByte;
        set
        {
            var opcode = trace.OpCode;
            opcode[1] = value;
            trace = trace with { OpCode = opcode };
        }
    }

    /// <summary>
    /// Gets the instruction mnemonic for the current instruction.
    /// </summary>
    public CpuInstructions Instruction
    {
        readonly get => trace.Instruction;
        set => trace = trace with { Instruction = value };
    }

    /// <summary>
    /// Gets the addressing mode used by the current instruction.
    /// </summary>
    public CpuAddressingModes AddressingMode
    {
        readonly get => trace.AddressingMode;
        set => trace = trace with { AddressingMode = value };
    }

    /// <summary>
    /// Gets the size of the operands for the current instruction in bytes.
    /// </summary>
    public byte OperandSize
    {
        readonly get => trace.OperandSize;
        set => trace = trace with { OperandSize = value };
    }

    /// <summary>
    /// Gets the operands for the current instruction (up to 4 bytes).
    /// </summary>
    public OperandBuffer Operands
    {
        readonly get => trace.Operands;
        set => trace = trace with { Operands = value };
    }

    /// <summary>
    /// Gets the effective address calculated for the current instruction.
    /// </summary>
    public Addr EffectiveAddress
    {
        readonly get => trace.EffectiveAddress;
        set => trace = trace with { EffectiveAddress = value };
    }

    /// <summary>
    /// Gets the number of cycles the current instruction took to execute.
    /// </summary>
    public byte InstructionCycles
    {
        readonly get => (byte)trace.InstructionCycles.Value;
        set => trace = trace with { InstructionCycles = new Cycle(value) };
    }

    /// <summary>
    /// Sets a single operand byte at the specified index.
    /// </summary>
    /// <param name="index">The index of the operand byte (0-3).</param>
    /// <param name="value">The value to set.</param>
    /// <remarks>
    /// This method is provided because the <see cref="Operands"/> property returns a copy
    /// of the operand buffer, so direct indexing on the property does not modify the state.
    /// Use this method to set individual operand bytes.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOperand(int index, byte value)
    {
        var operands = trace.Operands;
        operands[index] = value;
        trace = trace with { Operands = operands };
    }

    /// <summary>
    /// Gets the current instruction trace containing debug-related state information.
    /// </summary>
    /// <returns>An <see cref="InstructionTrace"/> representing the current instruction's execution details.</returns>
    /// <remarks>
    /// This method returns a snapshot of the instruction trace at the time of the call.
    /// The trace includes the start PC, opcode, instruction, addressing mode, operands,
    /// effective address, start cycle, and instruction cycles.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly InstructionTrace GetInstructionTrace()
    {
        return trace with { StartCycle = new Cycle(Cycles - trace.InstructionCycles.Value) };
    }

    /// <summary>
    /// Resets the debug-related state information of the CPU.
    /// </summary>
    /// <remarks>
    /// This method clears all debug-related fields, including the program counter at the start of the instruction,
    /// opcode, sub-opcode, instruction, operand size, effective address, instruction cycles, and operand data.
    /// It is intended to prepare the CPU state for the next instruction or debugging session.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ClearDebugStateInformation()
    {
        trace = new InstructionTrace(
            StartPC: Registers.PC.addr,
            OpCode: default,
            Instruction: CpuInstructions.None,
            AddressingMode: CpuAddressingModes.None,
            OperandSize: 0,
            Operands: default,
            EffectiveAddress: 0,
            StartCycle: new Cycle(Cycles),
            InstructionCycles: Cycle.Zero);
    }
}