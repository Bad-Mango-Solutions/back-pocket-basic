// <copyright file="Instructions.Compare.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;

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
    public static OpcodeHandler CMP(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte a = cpu.Registers.A.GetByte();
            byte result = (byte)(a - value);

            // Set carry if A >= value
            if (a >= value)
            {
                cpu.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.C;
            }

            cpu.Registers.P.SetZeroAndNegative(result);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.CMP };

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
    /// CPX - Compare X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes CPX with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CPX(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte x = cpu.Registers.X.GetByte();
            byte result = (byte)(x - value);

            // Set carry if X >= value
            if (x >= value)
            {
                cpu.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.C;
            }

            cpu.Registers.P.SetZeroAndNegative(result);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.CPX };

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
    /// CPY - Compare Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes CPY with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CPY(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte y = cpu.Registers.Y.GetByte();
            byte result = (byte)(y - value);

            // Set carry if Y >= value
            if (y >= value)
            {
                cpu.Registers.P |= ProcessorStatusFlags.C;
            }
            else
            {
                cpu.Registers.P &= ~ProcessorStatusFlags.C;
            }

            cpu.Registers.P.SetZeroAndNegative(result);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.CPY };

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