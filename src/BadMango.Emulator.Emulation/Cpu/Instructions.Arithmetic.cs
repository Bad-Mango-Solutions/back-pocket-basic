// <copyright file="Instructions.Arithmetic.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;
using Core.Interfaces.Cpu;

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
    public static OpcodeHandler ADC(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte a = cpu.State.Registers.A.GetByte();
            byte carry = cpu.State.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)1 : (byte)0;

            if (cpu.State.Registers.P.HasFlag(ProcessorStatusFlags.D))
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
                    cpu.State.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
                }

                cpu.State.Registers.P.SetZeroAndNegative(result);
                cpu.State.Registers.A.SetByte(result);
            }
            else
            {
                // Binary mode
                int result = a + value + carry;
                byte result8 = (byte)(result & 0xFF);

                if (result > 0xFF)
                {
                    cpu.State.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
                }

                // Set overflow: (A^result) & (value^result) & 0x80
                if (((a ^ result8) & (value ^ result8) & 0x80) != 0)
                {
                    cpu.State.Registers.P |= ProcessorStatusFlags.V;
                }
                else
                {
                    cpu.State.Registers.P &= ~ProcessorStatusFlags.V;
                }

                cpu.State.Registers.P.SetZeroAndNegative(result8);
                cpu.State.Registers.A.SetByte(result8);
            }

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ADC };

                if (cpu.Trace.AddressingMode == CpuAddressingModes.Immediate)
                {
                    var operands = cpu.Trace.Operands;
                    operands[0] = value;
                    cpu.Trace = cpu.Trace with { OperandSize = 1, Operands = operands };
                }

                cpu.Trace = cpu.Trace with { InstructionCycles = cpu.Trace.InstructionCycles + opCycles };
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// SBC - Subtract with Carry instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes SBC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SBC(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte a = cpu.State.Registers.A.GetByte();
            byte borrow = cpu.State.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)0 : (byte)1;

            if (cpu.State.Registers.P.HasFlag(ProcessorStatusFlags.D))
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
                    cpu.State.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
                }

                cpu.State.Registers.P.SetZeroAndNegative(result);
                cpu.State.Registers.A.SetByte(result);
            }
            else
            {
                // Binary mode
                int result = a - value - borrow;
                byte result8 = (byte)(result & 0xFF);

                if (result >= 0)
                {
                    cpu.State.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
                }

                // Set overflow: (A^value) & (A^result) & 0x80
                if (((a ^ value) & (a ^ result8) & 0x80) != 0)
                {
                    cpu.State.Registers.P |= ProcessorStatusFlags.V;
                }
                else
                {
                    cpu.State.Registers.P &= ~ProcessorStatusFlags.V;
                }

                cpu.State.Registers.P.SetZeroAndNegative(result8);
                cpu.State.Registers.A.SetByte(result8);
            }

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.SBC };

                if (cpu.Trace.AddressingMode == CpuAddressingModes.Immediate)
                {
                    var operands = cpu.Trace.Operands;
                    operands[0] = value;
                    cpu.Trace = cpu.Trace with { OperandSize = 1, Operands = operands };
                }

                cpu.Trace = cpu.Trace with { InstructionCycles = cpu.Trace.InstructionCycles + opCycles };
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// INC - Increment Memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes INC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INC(AddressingModeHandler<CpuState> addressingMode)
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

            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles++; // Internal operation

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.INC };
                cpu.Trace = cpu.Trace with { InstructionCycles = cpu.Trace.InstructionCycles + opCycles };
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// DEC - Decrement Memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes DEC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEC(AddressingModeHandler<CpuState> addressingMode)
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

            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles++; // Internal operation

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.DEC };
                cpu.Trace = cpu.Trace with { InstructionCycles = cpu.Trace.InstructionCycles + opCycles };
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// INX - Increment X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes INX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INX(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte value = (byte)(cpu.State.Registers.X.GetByte() + 1);
            cpu.State.Registers.X.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.INX };
                cpu.Trace = cpu.Trace with { InstructionCycles = cpu.Trace.InstructionCycles + opCycles };
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// INY - Increment Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes INY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INY(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte value = (byte)(cpu.State.Registers.Y.GetByte() + 1);
            cpu.State.Registers.Y.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.INY };
                cpu.Trace = cpu.Trace with { InstructionCycles = cpu.Trace.InstructionCycles + opCycles };
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// DEX - Decrement X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes DEX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEX(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte value = (byte)(cpu.State.Registers.X.GetByte() - 1);
            cpu.State.Registers.X.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.DEX };
                cpu.Trace = cpu.Trace with { InstructionCycles = cpu.Trace.InstructionCycles + opCycles };
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// DEY - Decrement Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes DEY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEY(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte value = (byte)(cpu.State.Registers.Y.GetByte() - 1);
            cpu.State.Registers.Y.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.DEY };
                cpu.Trace = cpu.Trace with { InstructionCycles = cpu.Trace.InstructionCycles + opCycles };
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }
}