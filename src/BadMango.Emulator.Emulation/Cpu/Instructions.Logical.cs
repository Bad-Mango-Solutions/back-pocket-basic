// <copyright file="Instructions.Logical.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;

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
    public static OpcodeHandler AND(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte result = (byte)(cpu.Registers.A.GetByte() & value);
            cpu.Registers.A.SetByte(result);
            cpu.Registers.P.SetZeroAndNegative(result);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.AND };

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
    /// ORA - Logical OR instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ORA with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ORA(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte result = (byte)(cpu.Registers.A.GetByte() | value);
            cpu.Registers.A.SetByte(result);
            cpu.Registers.P.SetZeroAndNegative(result);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.ORA };

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
    /// EOR - Exclusive OR instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes EOR with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler EOR(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte result = (byte)(cpu.Registers.A.GetByte() ^ value);
            cpu.Registers.A.SetByte(result);
            cpu.Registers.P.SetZeroAndNegative(result);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.EOR };

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
    /// BIT - Bit Test instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes BIT with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BIT(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte result = (byte)(cpu.Registers.A.GetByte() & value);

            // Set Z flag based on result
            if (result == 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.Z;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.Z;
            }

            // Set N flag from bit 7 of memory value
            if ((value & 0x80) != 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.N;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.N;
            }

            // Set V flag from bit 6 of memory value
            if ((value & 0x40) != 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.V;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.V;
            }

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.BIT };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// BIT immediate - Bit Test instruction with immediate addressing (65C02).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Immediate).</param>
    /// <returns>An opcode handler that executes BIT #imm.</returns>
    /// <remarks>
    /// Unlike other BIT addressing modes, BIT #imm only affects the Z flag based on A AND operand.
    /// The N and V flags are NOT modified (they are only set from memory bits 7 and 6 in non-immediate modes).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BITImmediate(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte result = (byte)(cpu.Registers.A.GetByte() & value);

            // Set Z flag based on result. N and V are NOT affected for immediate mode.
            if (result == 0)
            {
                cpu.Registers.P |= ProcessorStatusFlags.Z;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.Z;
            }

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.BIT };

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
}