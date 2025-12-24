// <copyright file="Instructions.Logical.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;

/// <summary>
/// Logical operations (AND, ORA, EOR, BIT).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// AND - Logical AND instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes AND with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler AND(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            byte result = (byte)(state.Registers.A.GetByte() & value);
            state.Registers.A.SetByte(result);
            state.Registers.P.SetZeroAndNegative(result);

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.AND;

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
    /// ORA - Logical OR instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ORA with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ORA(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            byte result = (byte)(state.Registers.A.GetByte() | value);
            state.Registers.A.SetByte(result);
            state.Registers.P.SetZeroAndNegative(result);

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.ORA;

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
    /// EOR - Exclusive OR instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes EOR with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler EOR(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            byte result = (byte)(state.Registers.A.GetByte() ^ value);
            state.Registers.A.SetByte(result);
            state.Registers.P.SetZeroAndNegative(result);

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.EOR;

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
    /// BIT - Bit Test instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes BIT with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BIT(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            byte result = (byte)(state.Registers.A.GetByte() & value);

            // Set Z flag based on result
            if (result == 0)
            {
                state.Registers.P |= ProcessorStatusFlags.Z;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.Z;
            }

            // Set N flag from bit 7 of memory value
            if ((value & 0x80) != 0)
            {
                state.Registers.P |= ProcessorStatusFlags.N;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.N;
            }

            // Set V flag from bit 6 of memory value
            if ((value & 0x40) != 0)
            {
                state.Registers.P |= ProcessorStatusFlags.V;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.V;
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.BIT;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }
}