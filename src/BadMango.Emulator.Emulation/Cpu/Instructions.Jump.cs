// <copyright file="Instructions.Jump.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;
using Core.Cpu;
using Core.Interfaces.Cpu;

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
    public static OpcodeHandler JMP(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            Addr targetAddr = addressingMode(cpu);
            cpu.Registers.PC.SetWord((Word)targetAddr);

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.JMP };
            }
        };
    }

    /// <summary>
    /// JSR - Jump to Subroutine instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Absolute).</param>
    /// <returns>An opcode handler that executes JSR.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler JSR(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr targetAddr = addressingMode(cpu);

            // JSR pushes PC - 1 to the stack (6502 behavior)
            // This is because RTS increments the pulled address before setting PC
            Word returnAddr = (Word)(cpu.Registers.PC.GetWord() - 1);

            cpu.Write8(cpu.PushByte(Cpu65C02Constants.StackBase), returnAddr.HighByte());
            opCycles++; // Push high byte
            cpu.Write8(cpu.PushByte(Cpu65C02Constants.StackBase), returnAddr.LowByte());
            opCycles++; // Push low byte
            cpu.Registers.PC.SetAddr(targetAddr);
            opCycles++; // Internal operation

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.JSR };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// RTS - Return from Subroutine instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes RTS.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler RTS(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);

            byte lo = cpu.Read8(cpu.PopByte(Cpu65C02Constants.StackBase));
            opCycles++; // Pull low byte
            byte hi = cpu.Read8(cpu.PopByte(Cpu65C02Constants.StackBase));
            opCycles++; // Pull high byte
            cpu.Registers.PC.SetWord((Word)(((hi << 8) | lo) + 1));
            opCycles += 3; // Internal operations

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.RTS };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// RTI - Return from Interrupt instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes RTI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler RTI(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);
            cpu.Registers.P = (ProcessorStatusFlags)cpu.Read8(cpu.PopByte(Cpu65C02Constants.StackBase));
            opCycles++; // Pull P
            byte lo = cpu.Read8(cpu.PopByte(Cpu65C02Constants.StackBase));
            opCycles++; // Pull PC low byte
            byte hi = cpu.Read8(cpu.PopByte(Cpu65C02Constants.StackBase));
            opCycles++; // Pull PC high byte
            cpu.Registers.PC.SetWord((Word)((hi << 8) | lo));
            cpu.HaltReason = HaltState.None;
            opCycles += 2; // Internal operations

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.RTI };
            }

            cpu.Registers.TCU += opCycles;
        };
    }
}