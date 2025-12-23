// <copyright file="Instructions.Arithmetic.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;

/// <summary>
/// Arithmetic instructions (ADC, SBC, INC, DEC, INX, INY, DEX, DEY).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// ADC - Add with Carry instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ADC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ADC(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

            byte a = state.Registers.A.GetByte();
            byte carry = state.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)1 : (byte)0;

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.D))
            {
                // Decimal mode
                int al = (a & 0x0F) + (value & 0x0F) + carry;
                if (al > 9)
                {
                    al += 6;
                }

                int ah = (a >> 4) + (value >> 4) + (al > 15 ? 1 : 0);
                if (ah > 9)
                {
                    ah += 6;
                }

                byte result = (byte)(((ah << 4) | (al & 0x0F)) & 0xFF);

                if (ah > 15)
                {
                    state.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.C;
                }

                state.Registers.P.SetZeroAndNegative(result);
                state.Registers.A.SetByte(result);
            }
            else
            {
                // Binary mode
                int result = a + value + carry;
                byte result8 = (byte)(result & 0xFF);

                if (result > 0xFF)
                {
                    state.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.C;
                }

                // Set overflow: (A^result) & (value^result) & 0x80
                if (((a ^ result8) & (value ^ result8) & 0x80) != 0)
                {
                    state.Registers.P |= ProcessorStatusFlags.V;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.V;
                }

                state.Registers.P.SetZeroAndNegative(result8);
                state.Registers.A.SetByte(result8);
            }
        };
    }

    /// <summary>
    /// SBC - Subtract with Carry instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes SBC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SBC(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

            byte a = state.Registers.A.GetByte();
            byte borrow = state.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)0 : (byte)1;

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.D))
            {
                // Decimal mode
                int al = (a & 0x0F) - (value & 0x0F) - borrow;
                if (al < 0)
                {
                    al -= 6;
                }

                int ah = (a >> 4) - (value >> 4) - (al < 0 ? 1 : 0);
                if (ah < 0)
                {
                    ah -= 6;
                }

                byte result = (byte)(((ah << 4) | (al & 0x0F)) & 0xFF);

                if (ah >= 0)
                {
                    state.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.C;
                }

                state.Registers.P.SetZeroAndNegative(result);
                state.Registers.A.SetByte(result);
            }
            else
            {
                // Binary mode
                int result = a - value - borrow;
                byte result8 = (byte)(result & 0xFF);

                if (result >= 0)
                {
                    state.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.C;
                }

                // Set overflow: (A^value) & (A^result) & 0x80
                if (((a ^ value) & (a ^ result8) & 0x80) != 0)
                {
                    state.Registers.P |= ProcessorStatusFlags.V;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.V;
                }

                state.Registers.P.SetZeroAndNegative(result8);
                state.Registers.A.SetByte(result8);
            }
        };
    }

    /// <summary>
    /// INC - Increment Memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes INC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INC(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

            value++;
            memory.Write(address, value);
            state.Cycles++;

            state.Registers.P.SetZeroAndNegative(value);
            state.Cycles++;
        };
    }

    /// <summary>
    /// DEC - Decrement Memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes DEC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEC(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

            value--;
            memory.Write(address, value);
            state.Cycles++;

            state.Registers.P.SetZeroAndNegative(value);
            state.Cycles++;
        };
    }

    /// <summary>
    /// INX - Increment X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes INX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INX(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = (byte)(state.Registers.X.GetByte() + 1);
            state.Registers.X.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            state.Cycles++;
        };
    }

    /// <summary>
    /// INY - Increment Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes INY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INY(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = (byte)(state.Registers.Y.GetByte() + 1);
            state.Registers.Y.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            state.Cycles++;
        };
    }

    /// <summary>
    /// DEX - Decrement X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes DEX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEX(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = (byte)(state.Registers.X.GetByte() - 1);
            state.Registers.X.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            state.Cycles++;
        };
    }

    /// <summary>
    /// DEY - Decrement Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes DEY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEY(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = (byte)(state.Registers.Y.GetByte() - 1);
            state.Registers.Y.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            state.Cycles++;
        };
    }
}