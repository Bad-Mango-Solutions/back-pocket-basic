// <copyright file="Instructions.Shift.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;

/// <summary>
/// Shift and rotate instructions (ASL, LSR, ROL, ROR).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// ASLa - Arithmetic Shift Left Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes ASL on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ASLa(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = state.Registers.A.GetByte();

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                state.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value <<= 1;
            state.Registers.A.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            state.Cycles++;
        };
    }

    /// <summary>
    /// ASL - Arithmetic Shift Left memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ASL on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ASL(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                state.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value <<= 1;
            state.Registers.P.SetZeroAndNegative(value);

            memory.Write(address, value);
            state.Cycles += 2;
        };
    }

    /// <summary>
    /// LSRa - Logical Shift Right Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes LSR on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler LSRa(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = state.Registers.A.GetByte();

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                state.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value >>= 1;
            state.Registers.A.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            state.Cycles++;
        };
    }

    /// <summary>
    /// LSR - Logical Shift Right memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LSR on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler LSR(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                state.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value >>= 1;
            state.Registers.P.SetZeroAndNegative(value);

            memory.Write(address, value);
            state.Cycles += 2;
        };
    }

    /// <summary>
    /// ROLa - Rotate Left Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes ROL on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ROLa(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = state.Registers.A.GetByte();
            byte oldCarry = state.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)1 : (byte)0;

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                state.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value << 1) | oldCarry);
            state.Registers.A.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            state.Cycles++;
        };
    }

    /// <summary>
    /// ROL - Rotate Left memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ROL on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ROL(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

            byte oldCarry = state.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)1 : (byte)0;

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                state.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value << 1) | oldCarry);
            state.Registers.P.SetZeroAndNegative(value);

            memory.Write(address, value);
            state.Cycles += 2;
        };
    }

    /// <summary>
    /// RORa - Rotate Right Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes ROR on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler RORa(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = state.Registers.A.GetByte();
            byte oldCarry = state.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)0x80 : (byte)0;

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                state.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value >> 1) | oldCarry);
            state.Registers.A.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            state.Cycles++;
        };
    }

    /// <summary>
    /// ROR - Rotate Right memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ROR on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ROR(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++;

            byte oldCarry = state.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)0x80 : (byte)0;

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                state.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value >> 1) | oldCarry);
            state.Registers.P.SetZeroAndNegative(value);

            memory.Write(address, value);
            state.Cycles += 2;
        };
    }
}