// <copyright file="Instructions.Shift.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;
using Core.Interfaces.Cpu;

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
    public static OpcodeHandler ASLa(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte value = cpu.State.Registers.A.GetByte();

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                cpu.State.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value <<= 1;
            cpu.State.Registers.A.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ASL };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// ASL - Arithmetic Shift Left memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ASL on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ASL(AddressingModeHandler<CpuState> addressingMode)
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
                cpu.State.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value <<= 1;
            cpu.State.Registers.P.SetZeroAndNegative(value);

            cpu.Write8(address, value);
            opCycles += 2; // Memory write + internal operation

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ASL };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// LSRa - Logical Shift Right Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes LSR on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler LSRa(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte value = cpu.State.Registers.A.GetByte();

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                cpu.State.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value >>= 1;
            cpu.State.Registers.A.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.LSR };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// LSR - Logical Shift Right memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LSR on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler LSR(AddressingModeHandler<CpuState> addressingMode)
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
                cpu.State.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value >>= 1;
            cpu.State.Registers.P.SetZeroAndNegative(value);

            cpu.Write8(address, value);
            opCycles += 2; // Memory write + internal operation

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.LSR };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// ROLa - Rotate Left Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes ROL on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ROLa(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte value = cpu.State.Registers.A.GetByte();
            byte oldCarry = cpu.State.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)1 : (byte)0;

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                cpu.State.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value << 1) | oldCarry);
            cpu.State.Registers.A.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ROL };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// ROL - Rotate Left memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ROL on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ROL(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte oldCarry = cpu.State.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)1 : (byte)0;

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                cpu.State.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value << 1) | oldCarry);
            cpu.State.Registers.P.SetZeroAndNegative(value);

            cpu.Write8(address, value);
            opCycles += 2; // Memory write + internal operation

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ROL };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// RORa - Rotate Right Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes ROR on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler RORa(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte value = cpu.State.Registers.A.GetByte();
            byte oldCarry = cpu.State.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)0x80 : (byte)0;

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                cpu.State.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value >> 1) | oldCarry);
            cpu.State.Registers.A.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ROR };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// ROR - Rotate Right memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ROR on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ROR(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte oldCarry = cpu.State.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)0x80 : (byte)0;

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                cpu.State.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
            }

            value = (byte)((value >> 1) | oldCarry);
            cpu.State.Registers.P.SetZeroAndNegative(value);

            cpu.Write8(address, value);
            opCycles += 2; // Memory write + internal operation

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ROR };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }
}