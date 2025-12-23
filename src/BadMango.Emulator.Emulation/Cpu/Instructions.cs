// <copyright file="Instructions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;

/// <summary>
/// Provides instruction implementations that compose with addressing modes.
/// </summary>
/// <remarks>
/// <para>
/// Instructions are higher-order functions that take addressing mode delegates
/// and return opcode handlers. This enables true composition and eliminates
/// the need for separate methods for each instruction/addressing-mode combination.
/// </para>
/// <para>
/// This partial class is split across multiple files by instruction category:
/// <list type="bullet">
/// <item><description><c>Instructions.cs</c> - Load/Store, NOP, BRK</description></item>
/// <item><description><c>Instructions.Flags.cs</c> - Flag manipulation (CLC, SEC, CLI, SEI, CLD, SED, CLV)</description></item>
/// <item><description><c>Instructions.Transfer.cs</c> - Register transfers (TAX, TAY, TXA, TYA, TXS, TSX)</description></item>
/// <item><description><c>Instructions.Stack.cs</c> - Stack operations (PHA, PHP, PLA, PLP, PHX, PLX, PHY, PLY)</description></item>
/// <item><description><c>Instructions.Jump.cs</c> - Jumps and subroutines (JMP, JSR, RTS, RTI)</description></item>
/// <item><description><c>Instructions.Branch.cs</c> - Branches (BCC, BCS, BEQ, BNE, BMI, BPL, BVC, BVS, BRA)</description></item>
/// <item><description><c>Instructions.Arithmetic.cs</c> - Arithmetic (ADC, SBC, INC, DEC, INX, INY, DEX, DEY)</description></item>
/// <item><description><c>Instructions.Logical.cs</c> - Logical operations (AND, ORA, EOR, BIT)</description></item>
/// <item><description><c>Instructions.Shift.cs</c> - Shifts and rotates (ASL, LSR, ROL, ROR)</description></item>
/// <item><description><c>Instructions.Compare.cs</c> - Comparisons (CMP, CPX, CPY)</description></item>
/// <item><description><c>Instructions.65C02.cs</c> - 65C02-specific (STZ, TSB, TRB, WAI, STP)</description></item>
/// </list>
/// </para>
/// </remarks>
public static partial class Instructions
{
    /// <summary>
    /// LDA - Load Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LDA with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler LDA(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte size = state.Registers.GetAccumulatorSize();
            var value = memory.ReadValue(address, size);
            state.Cycles++; // Memory read cycle

            state.Registers.P.SetZeroAndNegative(value, size);
            state.Registers.A.SetValue(value, size);
        };
    }

    /// <summary>
    /// LDX - Load X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LDX with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler LDX(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte size = state.Registers.GetIndexSize();
            var value = memory.ReadValue(address, size);
            state.Cycles++; // Memory read cycle

            state.Registers.P.SetZeroAndNegative(value, size);
            state.Registers.X.SetValue(value, size);
        };
    }

    /// <summary>
    /// LDY - Load Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LDY with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler LDY(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte size = state.Registers.GetIndexSize();
            var value = memory.ReadValue(address, size);
            state.Cycles++; // Memory read cycle

            state.Registers.P.SetZeroAndNegative(value, size);
            state.Registers.Y.SetValue(value, size);
        };
    }

    /// <summary>
    /// STA - Store Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STA with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler STA(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte size = state.Registers.GetAccumulatorSize();
            memory.WriteValue(address, state.Registers.A.GetValue(size), size);
            state.Cycles++; // Memory write cycle
        };
    }

    /// <summary>
    /// STX - Store X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STX with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler STX(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte size = state.Registers.GetIndexSize();
            memory.WriteValue(address, state.Registers.X.GetValue(size), size);
            state.Cycles++; // Memory write cycle
        };
    }

    /// <summary>
    /// STY - Store Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STY with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler STY(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte size = state.Registers.GetIndexSize();
            memory.WriteValue(address, state.Registers.Y.GetValue(size), size);
            state.Cycles++; // Memory write cycle
        };
    }

    /// <summary>
    /// NOP - No Operation instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes NOP.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler NOP(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state); // Call addressing mode (usually does nothing for Implied)
            state.Cycles++; // NOP takes 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// BRK - Force Break instruction. Causes a software interrupt.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes BRK.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BRK(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);

            // BRK causes a software interrupt
            // Total 7 cycles: 1 (opcode fetch) + 1 (PC increment) + 2 (push PC) + 1 (push P) + 2 (read IRQ vector)
            state.Registers.PC.Advance();

            Word pc = state.Registers.PC.GetWord();

            memory.Write(state.PushByte(Cpu65C02Constants.StackBase), pc.HighByte());
            memory.Write(state.PushByte(Cpu65C02Constants.StackBase), pc.LowByte());
            memory.Write(state.PushByte(Cpu65C02Constants.StackBase), (byte)(state.Registers.P | ProcessorStatusFlags.B));

            state.Registers.P |= ProcessorStatusFlags.I;
            state.Registers.PC.SetWord(memory.ReadWord(Cpu65C02Constants.IrqVector));

            state.Cycles += 6;
        };
    }
}