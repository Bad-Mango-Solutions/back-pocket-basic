// <copyright file="Instructions.Transfer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;
using Core.Interfaces.Cpu;

/// <summary>
/// Register transfer instructions (TAX, TAY, TXA, TYA, TXS, TSX).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// TAX - Transfer Accumulator to X instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TAX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TAX(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte size = cpu.State.Registers.GetIndexSize();
            var value = cpu.State.Registers.A.GetValue(size);
            cpu.State.Registers.X.SetValue(value, size);
            cpu.State.Registers.P.SetZeroAndNegative(value, size);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.TAX;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// TAY - Transfer Accumulator to Y instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TAY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TAY(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte size = cpu.State.Registers.GetIndexSize();
            var value = cpu.State.Registers.A.GetValue(size);
            cpu.State.Registers.Y.SetValue(value, size);
            cpu.State.Registers.P.SetZeroAndNegative(value, size);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.TAY;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// TXA - Transfer X to Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TXA.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TXA(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte size = cpu.State.Registers.GetAccumulatorSize();
            var value = cpu.State.Registers.X.GetValue(size);
            cpu.State.Registers.A.SetValue(value, size);
            cpu.State.Registers.P.SetZeroAndNegative(value, size);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.TXA;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// TYA - Transfer Y to Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TYA.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TYA(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte size = cpu.State.Registers.GetAccumulatorSize();
            var value = cpu.State.Registers.Y.GetValue(size);
            cpu.State.Registers.A.SetValue(value, size);
            cpu.State.Registers.P.SetZeroAndNegative(value, size);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.TYA;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// TXS - Transfer X to Stack Pointer instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TXS.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TXS(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.State.Registers.SP.SetByte(cpu.State.Registers.X.GetByte());
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.TXS;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// TSX - Transfer Stack Pointer to X instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TSX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TSX(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte size = cpu.State.Registers.GetIndexSize();
            byte value = cpu.State.Registers.SP.GetByte();
            cpu.State.Registers.X.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value, size);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.TSX;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }
}