// <copyright file="Instructions.Stack.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;

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
    public static OpcodeHandler PHA(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            memory.Write(state.PushByte(Cpu65C02Constants.StackBase), state.Registers.A.GetByte());
            opCycles += 2;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.PHA;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// PHP - Push Processor Status instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHP.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PHP(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            memory.Write(state.PushByte(Cpu65C02Constants.StackBase), (byte)(state.Registers.P | ProcessorStatusFlags.B));
            opCycles += 2;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.PHP;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// PLA - Pull Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLA.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PLA(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte value = memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            state.Registers.A.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            opCycles += 3;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.PLA;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// PLP - Pull Processor Status instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLP.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PLP(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            state.Registers.P = (ProcessorStatusFlags)memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            opCycles += 3;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.PLP;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// PHX - Push X Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PHX(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            memory.Write(state.PushByte(Cpu65C02Constants.StackBase), state.Registers.X.GetByte());
            opCycles += 2;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.PHX;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// PLX - Pull X Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PLX(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte value = memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            state.Registers.X.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            opCycles += 3;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.PLX;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// PHY - Push Y Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PHY(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            memory.Write(state.PushByte(Cpu65C02Constants.StackBase), state.Registers.Y.GetByte());
            opCycles += 2;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.PHY;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// PLY - Pull Y Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler PLY(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte value = memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            state.Registers.Y.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            opCycles += 3;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.PLY;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }
}