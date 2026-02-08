// <copyright file="Instructions.Arithmetic.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;

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
    public static OpcodeHandler ADC(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte a = cpu.Registers.A.GetByte();
            byte carry = (byte)((byte)cpu.Registers.P & (byte)ProcessorStatusFlags.C);

            if (((byte)cpu.Registers.P & (byte)ProcessorStatusFlags.D) != 0)
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
                    cpu.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    cpu.Registers.P &= ~ProcessorStatusFlags.C;
                }

                cpu.Registers.P.SetZeroAndNegative(result);
                cpu.Registers.A.SetByte(result);
            }
            else
            {
                // Binary mode
                int result = a + value + carry;
                byte result8 = (byte)(result & 0xFF);

                if (result > 0xFF)
                {
                    cpu.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    cpu.Registers.P &= ~ProcessorStatusFlags.C;
                }

                // Set overflow: (A^result) & (value^result) & 0x80
                if (((a ^ result8) & (value ^ result8) & 0x80) != 0)
                {
                    cpu.Registers.P |= ProcessorStatusFlags.V;
                }
                else
                {
                    cpu.Registers.P &= ~ProcessorStatusFlags.V;
                }

                cpu.Registers.P.SetZeroAndNegative(result8);
                cpu.Registers.A.SetByte(result8);
            }

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ADC };

                if (cpu.Trace.AddressingMode == CpuAddressingModes.Immediate)
                {
                    var operands = cpu.Trace.Operands;
                    operands[0] = value;
                    cpu.Trace = cpu.Trace with { OperandSize = 1, Operands = operands };
                }
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// SBC - Subtract with Carry instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes SBC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SBC(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte a = cpu.Registers.A.GetByte();
            byte borrow = (byte)(((byte)cpu.Registers.P & (byte)ProcessorStatusFlags.C) ^ 0x01);

            if (((byte)cpu.Registers.P & (byte)ProcessorStatusFlags.D) != 0)
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
                    cpu.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    cpu.Registers.P &= ~ProcessorStatusFlags.C;
                }

                cpu.Registers.P.SetZeroAndNegative(result);
                cpu.Registers.A.SetByte(result);
            }
            else
            {
                // Binary mode
                int result = a - value - borrow;
                byte result8 = (byte)(result & 0xFF);

                if (result >= 0)
                {
                    cpu.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    cpu.Registers.P &= ~ProcessorStatusFlags.C;
                }

                // Set overflow: (A^value) & (A^result) & 0x80
                if (((a ^ value) & (a ^ result8) & 0x80) != 0)
                {
                    cpu.Registers.P |= ProcessorStatusFlags.V;
                }
                else
                {
                    cpu.Registers.P &= ~ProcessorStatusFlags.V;
                }

                cpu.Registers.P.SetZeroAndNegative(result8);
                cpu.Registers.A.SetByte(result8);
            }

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.SBC };

                if (cpu.Trace.AddressingMode == CpuAddressingModes.Immediate)
                {
                    var operands = cpu.Trace.Operands;
                    operands[0] = value;
                    cpu.Trace = cpu.Trace with { OperandSize = 1, Operands = operands };
                }
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// INC - Increment Memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes INC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INC(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            value++;
            cpu.Write8(address, value);
            opCycles++; // Memory write

            cpu.Registers.P.SetZeroAndNegative(value);
            opCycles++; // Internal operation

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.INC };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// DEC - Decrement Memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes DEC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEC(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            value--;
            cpu.Write8(address, value);
            opCycles++; // Memory write

            cpu.Registers.P.SetZeroAndNegative(value);
            opCycles++; // Internal operation

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.DEC };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// INX - Increment X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes INX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INX(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte value = (byte)(cpu.Registers.X.GetByte() + 1);
            cpu.Registers.X.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.INX };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// INY - Increment Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes INY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INY(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte value = (byte)(cpu.Registers.Y.GetByte() + 1);
            cpu.Registers.Y.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.INY };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// DEX - Decrement X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes DEX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEX(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte value = (byte)(cpu.Registers.X.GetByte() - 1);
            cpu.Registers.X.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.DEX };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// DEY - Decrement Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes DEY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEY(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte value = (byte)(cpu.Registers.Y.GetByte() - 1);
            cpu.Registers.Y.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.DEY };
            }

            cpu.Registers.TCU += 1;
        };
    }
}