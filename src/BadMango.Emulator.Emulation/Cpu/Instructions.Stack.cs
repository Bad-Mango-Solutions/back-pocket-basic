// <copyright file="Instructions.Stack.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core.Cpu;
using Core.Interfaces.Cpu;

/// <summary>
/// Stack operations (PHA, PHP, PLA, PLP, PHX, PLX, PHY, PLY).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// PHA - Push Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHA.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PHA(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Write8(cpu.State.PushByte(Cpu65C02Constants.StackBase), cpu.State.Registers.A.GetByte());
            opCycles += 2;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PHA };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// PHP - Push Processor Status instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHP.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PHP(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Write8(cpu.State.PushByte(Cpu65C02Constants.StackBase), (byte)(cpu.State.Registers.P | ProcessorStatusFlags.B));
            opCycles += 2;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PHP };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// PLA - Pull Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLA.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PLA(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte value = cpu.Read8(cpu.State.PopByte(Cpu65C02Constants.StackBase));
            cpu.State.Registers.A.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles += 3;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PLA };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// PLP - Pull Processor Status instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLP.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PLP(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.State.Registers.P = (ProcessorStatusFlags)cpu.Read8(cpu.State.PopByte(Cpu65C02Constants.StackBase));
            opCycles += 3;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PLP };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// PHX - Push X Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PHX(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Write8(cpu.State.PushByte(Cpu65C02Constants.StackBase), cpu.State.Registers.X.GetByte());
            opCycles += 2;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PHX };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// PLX - Pull X Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PLX(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte value = cpu.Read8(cpu.State.PopByte(Cpu65C02Constants.StackBase));
            cpu.State.Registers.X.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles += 3;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PLX };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// PHY - Push Y Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PHY(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Write8(cpu.State.PushByte(Cpu65C02Constants.StackBase), cpu.State.Registers.Y.GetByte());
            opCycles += 2;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PHY };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// PLY - Pull Y Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PLY(AddressingModeHandler<CpuState> addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            byte value = cpu.Read8(cpu.State.PopByte(Cpu65C02Constants.StackBase));
            cpu.State.Registers.Y.SetByte(value);
            cpu.State.Registers.P.SetZeroAndNegative(value);
            opCycles += 3;

            if (cpu.State.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PLY };
                cpu.State.InstructionCycles += opCycles;
            }

            cpu.State.Registers.TCU += opCycles;
        };
    }
}