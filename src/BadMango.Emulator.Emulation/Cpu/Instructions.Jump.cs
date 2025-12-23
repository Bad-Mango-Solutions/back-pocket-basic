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
            Addr targetAddr = addressingMode(memory, ref state);

            // JSR pushes PC - 1 to the stack (6502 behavior)
            // This is because RTS increments the pulled address before setting PC
            Word returnAddr = (Word)(state.Registers.PC.GetWord() - 1);

            memory.Write(state.PushByte(Cpu65C02Constants.StackBase), returnAddr.HighByte());
            memory.Write(state.PushByte(Cpu65C02Constants.StackBase), returnAddr.LowByte());
            state.Registers.PC.SetAddr(targetAddr);
            state.Cycles += 3;
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
            addressingMode(memory, ref state);

            byte lo = memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            byte hi = memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            state.Registers.PC.SetWord((Word)(((hi << 8) | lo) + 1));
            state.Cycles += 5;
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
            addressingMode(memory, ref state);
            state.Registers.P = (ProcessorStatusFlags)memory.ReadByte(state.PopByte(Cpu65C02Constants.StackBase));
            byte lo = memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            byte hi = memory.Read(state.PopByte(Cpu65C02Constants.StackBase));
            state.Registers.PC.SetWord((Word)((hi << 8) | lo));
            state.HaltReason = HaltState.None;
            state.Cycles += 5;
        };
    }
}