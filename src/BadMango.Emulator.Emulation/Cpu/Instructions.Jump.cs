// <copyright file="Instructions.Jump.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;

/// <summary>
/// Jump and subroutine instructions (JMP, JSR, RTS, RTI).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// JMP - Jump instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (Absolute or Indirect).</param>
    /// <returns>An opcode handler that executes JMP.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler JMP(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);
            state.Registers.PC.SetWord((Word)targetAddr);

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.JMP;
            }
        };
    }

    /// <summary>
    /// JSR - Jump to Subroutine instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Absolute).</param>
    /// <returns>An opcode handler that executes JSR.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler JSR(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr targetAddr = addressingMode(memory, ref state);

            // JSR pushes PC - 1 to the stack (6502 behavior)
            // This is because RTS increments the pulled address before setting PC
            Word returnAddr = (Word)(state.Registers.PC.GetWord() - 1);

            memory.Write(state.PushByte(Cpu65C02Constants.StackBase), returnAddr.HighByte());
            opCycles++; // Push high byte
            memory.Write(state.PushByte(Cpu65C02Constants.StackBase), returnAddr.LowByte());
            opCycles++; // Push low byte
            state.Registers.PC.SetAddr(targetAddr);
            opCycles++; // Internal operation

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.JSR;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// RTS - Return from Subroutine instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes RTS.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler RTS(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);

            byte lo = memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            opCycles++; // Pull low byte
            byte hi = memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            opCycles++; // Pull high byte
            state.Registers.PC.SetWord((Word)(((hi << 8) | lo) + 1));
            opCycles += 3; // Internal operations

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.RTS;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// RTI - Return from Interrupt instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes RTI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler RTI(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            state.Registers.P = (ProcessorStatusFlags)memory.ReadByte(state.PopByte(Cpu65C02Constants.StackBase));
            opCycles++; // Pull P
            byte lo = memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            opCycles++; // Pull PC low byte
            byte hi = memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            opCycles++; // Pull PC high byte
            state.Registers.PC.SetWord((Word)((hi << 8) | lo));
            state.HaltReason = HaltState.None;
            opCycles += 2; // Internal operations

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.RTI;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }
}