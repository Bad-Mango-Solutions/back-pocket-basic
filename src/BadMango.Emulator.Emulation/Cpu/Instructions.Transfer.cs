// <copyright file="Instructions.Transfer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;
using Core.Cpu;

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
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte size = state.Registers.GetIndexSize();
            var value = state.Registers.A.GetValue(size);
            state.Registers.X.SetValue(value, size);
            state.Registers.P.SetZeroAndNegative(value, size);
            opCycles++;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.TAX;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
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
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte size = state.Registers.GetIndexSize();
            var value = state.Registers.A.GetValue(size);
            state.Registers.Y.SetValue(value, size);
            state.Registers.P.SetZeroAndNegative(value, size);
            opCycles++;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.TAY;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
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
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte size = state.Registers.GetAccumulatorSize();
            var value = state.Registers.X.GetValue(size);
            state.Registers.A.SetValue(value, size);
            state.Registers.P.SetZeroAndNegative(value, size);
            opCycles++;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.TXA;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
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
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte size = state.Registers.GetAccumulatorSize();
            var value = state.Registers.Y.GetValue(size);
            state.Registers.A.SetValue(value, size);
            state.Registers.P.SetZeroAndNegative(value, size);
            opCycles++;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.TYA;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
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
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            state.Registers.SP.SetByte(state.Registers.X.GetByte());
            opCycles++;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.TXS;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
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
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte size = state.Registers.GetIndexSize();
            byte value = state.Registers.SP.GetByte();
            state.Registers.X.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value, size);
            opCycles++;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.TSX;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }
}