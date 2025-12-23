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
    public static OpcodeHandler CMP(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

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
        };
    }

    /// <summary>
    /// CPX - Compare X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes CPX with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CPX(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

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
        };
    }

    /// <summary>
    /// CPY - Compare Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes CPY with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CPY(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

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
        };
    }
}