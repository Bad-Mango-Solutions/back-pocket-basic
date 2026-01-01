// <copyright file="Instructions.Logical.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;
using Core.Interfaces.Cpu;

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
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte result = (byte)(cpu.State.Registers.A.GetByte() & value);
            cpu.State.Registers.A.SetByte(result);
            cpu.State.Registers.P.SetZeroAndNegative(result);

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.AND };

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
    /// ORA - Logical OR instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ORA with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ORA(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte result = (byte)(cpu.State.Registers.A.GetByte() | value);
            cpu.State.Registers.A.SetByte(result);
            cpu.State.Registers.P.SetZeroAndNegative(result);

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ORA };

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
    /// EOR - Exclusive OR instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes EOR with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler EOR(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte result = (byte)(cpu.State.Registers.A.GetByte() ^ value);
            cpu.State.Registers.A.SetByte(result);
            cpu.State.Registers.P.SetZeroAndNegative(result);

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.EOR };

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
    /// BIT - Bit Test instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes BIT with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BIT(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte result = (byte)(cpu.State.Registers.A.GetByte() & value);

            // Set Z flag based on result
            if (result == 0)
            {
                cpu.State.Registers.P |= ProcessorStatusFlags.Z;
            }
            else
            {
                cpu.State.Registers.P &= ~ProcessorStatusFlags.Z;
            }

            // Set N flag from bit 7 of memory value
            if ((value & 0x80) != 0)
            {
                cpu.State.Registers.P |= ProcessorStatusFlags.N;
            }
            else
            {
                cpu.State.Registers.P &= ~ProcessorStatusFlags.N;
            }

            // Set V flag from bit 6 of memory value
            if ((value & 0x40) != 0)
            {
                cpu.State.Registers.P |= ProcessorStatusFlags.V;
            }
            else
            {
                cpu.State.Registers.P &= ~ProcessorStatusFlags.V;
            }

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.BIT };
                cpu.Trace = cpu.Trace with { InstructionCycles = cpu.Trace.InstructionCycles + opCycles };
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }
}