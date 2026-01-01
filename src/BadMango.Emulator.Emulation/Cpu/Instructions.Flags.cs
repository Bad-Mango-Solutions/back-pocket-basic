// <copyright file="Instructions.Flags.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;
using Core.Interfaces.Cpu;

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
    public static OpcodeHandler CLC(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.State.Registers.P &= ~ProcessorStatusFlags.C;
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.CLC;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// SEC - Set Carry Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SEC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SEC(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.State.Registers.P |= ProcessorStatusFlags.C;
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.SEC;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// CLI - Clear Interrupt Disable Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CLI(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.State.Registers.P &= ~ProcessorStatusFlags.I;
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.CLI;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// SEI - Set Interrupt Disable Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SEI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SEI(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.State.Registers.P |= ProcessorStatusFlags.I;
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.SEI;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// CLD - Clear Decimal Mode Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLD.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CLD(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.State.Registers.P &= ~ProcessorStatusFlags.D;
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.CLD;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// SED - Set Decimal Mode Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SED.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SED(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.State.Registers.P |= ProcessorStatusFlags.D;
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.SED;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// CLV - Clear Overflow Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLV.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CLV(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.State.Registers.P &= ~ProcessorStatusFlags.V;
            opCycles++;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.State.Instruction = CpuInstructions.CLV;
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }
}