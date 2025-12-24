// <copyright file="Instructions.Compare.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;

/// <summary>
/// Comparison instructions (CMP, CPX, CPY).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// CMP - Compare Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes CMP with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CMP(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            byte a = state.Registers.A.GetByte();
            byte result = (byte)(a - value);

            // Set carry if A >= value
            if (a >= value)
            {
                state.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.C;
            }

            state.Registers.P.SetZeroAndNegative(result);

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.CMP;

                if (state.AddressingMode == CpuAddressingModes.Immediate)
                {
                    state.OperandSize = 1;
                    state.Operands[0] = value;
                }

                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// CPX - Compare X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes CPX with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CPX(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            byte x = state.Registers.X.GetByte();
            byte result = (byte)(x - value);

            // Set carry if X >= value
            if (x >= value)
            {
                state.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.C;
            }

            state.Registers.P.SetZeroAndNegative(result);

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.CPX;

                if (state.AddressingMode == CpuAddressingModes.Immediate)
                {
                    state.OperandSize = 1;
                    state.Operands[0] = value;
                }

                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// CPY - Compare Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes CPY with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CPY(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            byte y = state.Registers.Y.GetByte();
            byte result = (byte)(y - value);

            // Set carry if Y >= value
            if (y >= value)
            {
                state.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.C;
            }

            state.Registers.P.SetZeroAndNegative(result);

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.CPY;

                if (state.AddressingMode == CpuAddressingModes.Immediate)
                {
                    state.OperandSize = 1;
                    state.Operands[0] = value;
                }

                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }
}