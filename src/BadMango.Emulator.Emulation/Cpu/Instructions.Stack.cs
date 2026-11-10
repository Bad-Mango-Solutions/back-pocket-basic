// <copyright file="Instructions.Stack.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using Core.Cpu;

/// <content>
/// Stack operations (PHA, PHP, PLA, PLP, PHX, PLX, PHY, PLY).
/// </content>
public static partial class Instructions
{
    /// <content>
    /// PHA - Push Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHA.</returns>
    public static OpcodeHandler PHA(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Write8(cpu.PushByte(Cpu65C02Constants.StackBase), cpu.Registers.A.GetByte());

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PHA };
            }

            cpu.Registers.TCU += 2;
        };
    }

    /// <summary>
    /// PHP - Push Processor Status instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHP.</returns>
    public static OpcodeHandler PHP(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Write8(cpu.PushByte(Cpu65C02Constants.StackBase), (byte)(cpu.Registers.P | ProcessorStatusFlags.B | ProcessorStatusFlags.R));

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PHP };
            }

            cpu.Registers.TCU += 2;
        };
    }

    /// <summary>
    /// PLA - Pull Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLA.</returns>
    public static OpcodeHandler PLA(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte value = cpu.Read8(cpu.PopByte(Cpu65C02Constants.StackBase));
            cpu.Registers.A.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PLA };
            }

            cpu.Registers.TCU += 3;
        };
    }

    /// <summary>
    /// PLP - Pull Processor Status instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLP.</returns>
    public static OpcodeHandler PLP(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Registers.P = (ProcessorStatusFlags)cpu.Read8(cpu.PopByte(Cpu65C02Constants.StackBase)) | ProcessorStatusFlags.R;

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PLP };
            }

            cpu.Registers.TCU += 3;
        };
    }

    /// <summary>
    /// PHX - Push X Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHX.</returns>
    public static OpcodeHandler PHX(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Write8(cpu.PushByte(Cpu65C02Constants.StackBase), cpu.Registers.X.GetByte());

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PHX };
            }

            cpu.Registers.TCU += 2;
        };
    }

    /// <summary>
    /// PLX - Pull X Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLX.</returns>
    public static OpcodeHandler PLX(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte value = cpu.Read8(cpu.PopByte(Cpu65C02Constants.StackBase));
            cpu.Registers.X.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PLX };
            }

            cpu.Registers.TCU += 3;
        };
    }

    /// <summary>
    /// PHY - Push Y Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHY.</returns>
    public static OpcodeHandler PHY(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Write8(cpu.PushByte(Cpu65C02Constants.StackBase), cpu.Registers.Y.GetByte());

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PHY };
            }

            cpu.Registers.TCU += 2;
        };
    }

    /// <summary>
    /// PLY - Pull Y Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLY.</returns>
    public static OpcodeHandler PLY(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            byte value = cpu.Read8(cpu.PopByte(Cpu65C02Constants.StackBase));
            cpu.Registers.Y.SetByte(value);
            cpu.Registers.P.SetZeroAndNegative(value);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.PLY };
            }

            cpu.Registers.TCU += 3;
        };
    }
}