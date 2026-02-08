// <copyright file="Instructions.Transfer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;

/// <summary>
/// Register transfer instructions (TAX, TAY, TXA, TYA, TXS, TSX).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// TAX - Transfer Accumulator to X instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TAX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TAX(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte size = cpu.Registers.GetIndexSize();
            var value = cpu.Registers.A.GetValue(size);
            cpu.Registers.X.SetValue(value, size);
            cpu.Registers.P.SetZeroAndNegative(value, size);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.TAX };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// TAY - Transfer Accumulator to Y instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TAY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TAY(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte size = cpu.Registers.GetIndexSize();
            var value = cpu.Registers.A.GetValue(size);
            cpu.Registers.Y.SetValue(value, size);
            cpu.Registers.P.SetZeroAndNegative(value, size);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.TAY };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// TXA - Transfer X to Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TXA.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TXA(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte size = cpu.Registers.GetAccumulatorSize();
            var value = cpu.Registers.X.GetValue(size);
            cpu.Registers.A.SetValue(value, size);
            cpu.Registers.P.SetZeroAndNegative(value, size);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.TXA };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// TYA - Transfer Y to Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TYA.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TYA(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte size = cpu.Registers.GetAccumulatorSize();
            var value = cpu.Registers.Y.GetValue(size);
            cpu.Registers.A.SetValue(value, size);
            cpu.Registers.P.SetZeroAndNegative(value, size);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.TYA };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// TXS - Transfer X to Stack Pointer instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TXS.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TXS(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Registers.SP.SetByte(cpu.Registers.X.GetByte());

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.TXS };
            }

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// TSX - Transfer Stack Pointer to X instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TSX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler TSX(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte size = cpu.Registers.GetIndexSize();
            byte value = cpu.Registers.SP.GetByte();
            cpu.Registers.X.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value, size);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.TSX };
            }

            cpu.Registers.TCU += 1;
        };
    }
}