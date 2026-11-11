// <copyright file="Instructions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using Core;
using Core.Cpu;

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
    public static OpcodeHandler LDA(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte size = cpu.Registers.GetAccumulatorSize();
            var value = cpu.ReadValue(address, size);
            opCycles++; // Memory read cycle

            if (cpu.Trace.AddressingMode == CpuAddressingModes.Immediate)
            {
                var operands = cpu.Trace.Operands;
                operands[0] = (byte)(value & 0xff);
                cpu.Trace = cpu.Trace with
                {
                    OperandSize = (byte)(size / 8),
                    Operands = operands,
                };
            }

            cpu.Registers.TCU += opCycles;
            cpu.Registers.P.SetZeroAndNegative(value, size);
            cpu.Registers.A.SetValue(value, size);
        };
    }

    /// <summary>
    /// LDX - Load X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LDX with the given addressing mode.</returns>
    public static OpcodeHandler LDX(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte size = cpu.Registers.GetIndexSize();
            var value = cpu.ReadValue(address, size);
            opCycles++; // Memory read cycle

            if (cpu.Trace.AddressingMode == CpuAddressingModes.Immediate)
            {
                var operands = cpu.Trace.Operands;
                operands[0] = (byte)(value & 0xff);
                cpu.Trace = cpu.Trace with
                {
                    OperandSize = (byte)(size / 8),
                    Operands = operands,
                };
            }

            cpu.Registers.TCU += opCycles;
            cpu.Registers.P.SetZeroAndNegative(value, size);
            cpu.Registers.X.SetValue(value, size);
        };
    }

    /// <summary>
    /// LDY - Load Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LDY with the given addressing mode.</returns>
    public static OpcodeHandler LDY(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte size = cpu.Registers.GetIndexSize();
            var value = cpu.ReadValue(address, size);
            opCycles++; // Memory read cycle

            if (cpu.Trace.AddressingMode == CpuAddressingModes.Immediate)
            {
                var operands = cpu.Trace.Operands;
                operands[0] = (byte)(value & 0xff);
                cpu.Trace = cpu.Trace with
                {
                    OperandSize = (byte)(size / 8),
                    Operands = operands,
                };
            }

            cpu.Registers.TCU += opCycles;
            cpu.Registers.P.SetZeroAndNegative(value, size);
            cpu.Registers.Y.SetValue(value, size);
        };
    }

    /// <summary>
    /// STA - Store Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STA with the given addressing mode.</returns>
    public static OpcodeHandler STA(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            Addr address = addressingMode(cpu);
            byte size = cpu.Registers.GetAccumulatorSize();
            cpu.WriteValue(address, cpu.Registers.A.GetValue(size), size);

            cpu.Registers.TCU += 1; // Memory write cycle
        };
    }

    /// <summary>
    /// STX - Store X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STX with the given addressing mode.</returns>
    public static OpcodeHandler STX(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            Addr address = addressingMode(cpu);
            byte size = cpu.Registers.GetIndexSize();
            cpu.WriteValue(address, cpu.Registers.X.GetValue(size), size);

            cpu.Registers.TCU += 1; // Memory write cycle
        };
    }

    /// <summary>
    /// STY - Store Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STY with the given addressing mode.</returns>
    public static OpcodeHandler STY(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            Addr address = addressingMode(cpu);
            byte size = cpu.Registers.GetIndexSize();
            cpu.WriteValue(address, cpu.Registers.Y.GetValue(size), size);

            cpu.Registers.TCU += 1; // Memory write cycle
        };
    }

    /// <summary>
    /// NOP - No Operation instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <param name="instructionCycles">
    /// Number of cycles the instruction itself consumes in addition to the base
    /// fetch cycle and any cycles already added by <paramref name="addressingMode"/>.
    /// Defaults to <c>1</c>, which produces a standard 2-cycle Implied NOP
    /// (<c>$EA</c>). Pass <c>0</c> for the WDC65C02S 1-byte/1-cycle "unused"
    /// NOP slots (column 3 of the opcode matrix). Pass a larger value to cover
    /// the 8-cycle "loud" NOP at <c>$5C</c>.
    /// </param>
    /// <returns>An opcode handler that executes NOP.</returns>
    /// <remarks>
    /// All NOP variants — including the WDC65C02S unused opcode slots that
    /// decode as multi-byte/multi-cycle NOPs — share this implementation. Flags
    /// and registers are never modified. PC advancement and addressing-mode
    /// cycles are entirely delegated to <paramref name="addressingMode"/>; the
    /// only state mutation here is the TCU increment.
    /// </remarks>
    public static OpcodeHandler NOP(AddressingModeHandler addressingMode, int instructionCycles = 1)
    {
        return cpu =>
        {
            addressingMode(cpu); // Call addressing mode (usually does nothing for Implied)

            if (instructionCycles > 0)
            {
                cpu.Registers.TCU += (ulong)instructionCycles;
            }
        };
    }

    /// <summary>
    /// BRK - Force Break instruction. Causes a software interrupt.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes BRK.</returns>
    public static OpcodeHandler BRK(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            byte opCycles = 0;
            addressingMode(cpu);

            // BRK causes a software interrupt
            // Total 7 cycles: 1 (opcode fetch) + 1 (PC increment) + 2 (push PC) + 1 (push P) + 2 (read IRQ vector)
            cpu.Registers.PC.Advance();
            opCycles++; // PC increment cycle

            Word pc = cpu.Registers.PC.GetWord();

            cpu.Write8(cpu.PushByte(Cpu65C02Constants.StackBase), pc.HighByte());
            opCycles++; // Push PC high byte
            cpu.Write8(cpu.PushByte(Cpu65C02Constants.StackBase), pc.LowByte());
            opCycles++; // Push PC low byte
            cpu.Write8(cpu.PushByte(Cpu65C02Constants.StackBase), (byte)(cpu.Registers.P | ProcessorStatusFlags.B | ProcessorStatusFlags.R));
            opCycles++; // Push P

            cpu.Registers.P |= ProcessorStatusFlags.I;

            // 65C02: BRK clears the D flag after pushing status (NMOS 6502 does not)
            cpu.Registers.P &= ~ProcessorStatusFlags.D;
            cpu.Registers.PC.SetWord(cpu.Read16(Cpu65C02Constants.IrqVector));
            opCycles += 2; // Read IRQ vector (2 bytes)

            cpu.Registers.TCU += opCycles;
        };
    }
}