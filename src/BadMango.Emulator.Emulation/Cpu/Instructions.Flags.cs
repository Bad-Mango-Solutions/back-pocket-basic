// <copyright file="Instructions.Flags.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;

/// <summary>
/// Flag manipulation instructions (CLC, SEC, CLI, SEI, CLD, SED, CLV).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// CLC - Clear Carry Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CLC(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Registers.P &= ~ProcessorStatusFlags.C;
            opCycles++;

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.CLC };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// SEC - Set Carry Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SEC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SEC(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Registers.P |= ProcessorStatusFlags.C;
            opCycles++;

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.SEC };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// CLI - Clear Interrupt Disable Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CLI(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Registers.P &= ~ProcessorStatusFlags.I;
            opCycles++;

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.CLI };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// SEI - Set Interrupt Disable Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SEI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SEI(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Registers.P |= ProcessorStatusFlags.I;
            opCycles++;

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.SEI };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// CLD - Clear Decimal Mode Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLD.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CLD(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Registers.P &= ~ProcessorStatusFlags.D;
            opCycles++;

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.CLD };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// SED - Set Decimal Mode Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SED.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SED(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Registers.P |= ProcessorStatusFlags.D;
            opCycles++;

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.SED };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// CLV - Clear Overflow Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLV.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CLV(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Registers.P &= ~ProcessorStatusFlags.V;
            opCycles++;

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.CLV };
            }

            cpu.Registers.TCU += opCycles;
        };
    }
}