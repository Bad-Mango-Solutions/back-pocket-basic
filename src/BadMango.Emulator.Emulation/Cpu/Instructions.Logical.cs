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
    public static OpcodeHandler AND(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

            byte result = (byte)(state.Registers.A.GetByte() & value);
            state.Registers.A.SetByte(result);
            state.Registers.P.SetZeroAndNegative(result);
        };
    }

    /// <summary>
    /// ORA - Logical OR instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ORA with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ORA(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

            byte result = (byte)(state.Registers.A.GetByte() | value);
            state.Registers.A.SetByte(result);
            state.Registers.P.SetZeroAndNegative(result);
        };
    }

    /// <summary>
    /// EOR - Exclusive OR instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes EOR with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler EOR(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

            byte result = (byte)(state.Registers.A.GetByte() ^ value);
            state.Registers.A.SetByte(result);
            state.Registers.P.SetZeroAndNegative(result);
        };
    }

    /// <summary>
    /// BIT - Bit Test instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes BIT with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BIT(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

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
        };
    }
}