// <copyright file="Instructions.Shift.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;

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
    public static OpcodeHandler ASLa(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte value = cpu.Registers.A.GetByte();

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value <<= 1;
            cpu.Registers.A.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ASL };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// ASL - Arithmetic Shift Left memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ASL on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ASL(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value <<= 1;
            cpu.Registers.P.SetZeroAndNegative(value);

            cpu.Write8(address, value);
            opCycles += 2; // Memory write + internal operation

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ASL };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// LSRa - Logical Shift Right Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes LSR on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler LSRa(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte value = cpu.Registers.A.GetByte();

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value >>= 1;
            cpu.Registers.A.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.LSR };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// LSR - Logical Shift Right memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LSR on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler LSR(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value >>= 1;
            cpu.Registers.P.SetZeroAndNegative(value);

            cpu.Write8(address, value);
            opCycles += 2; // Memory write + internal operation

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.LSR };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// ROLa - Rotate Left Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes ROL on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ROLa(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte value = cpu.Registers.A.GetByte();
            byte oldCarry = cpu.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)1 : (byte)0;

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value << 1) | oldCarry);
            cpu.Registers.A.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ROL };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// ROL - Rotate Left memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ROL on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ROL(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte oldCarry = cpu.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)1 : (byte)0;

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value << 1) | oldCarry);
            cpu.Registers.P.SetZeroAndNegative(value);

            cpu.Write8(address, value);
            opCycles += 2; // Memory write + internal operation

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ROL };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// RORa - Rotate Right Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes ROR on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler RORa(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte value = cpu.Registers.A.GetByte();
            byte oldCarry = cpu.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)0x80 : (byte)0;

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value >> 1) | oldCarry);
            cpu.Registers.A.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ROR };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// ROR - Rotate Right memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ROR on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ROR(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte oldCarry = cpu.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)0x80 : (byte)0;

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value >> 1) | oldCarry);
            cpu.Registers.P.SetZeroAndNegative(value);

            cpu.Write8(address, value);
            opCycles += 2; // Memory write + internal operation

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ROR };
            }

            cpu.Registers.TCU += opCycles;
        };
    }
}