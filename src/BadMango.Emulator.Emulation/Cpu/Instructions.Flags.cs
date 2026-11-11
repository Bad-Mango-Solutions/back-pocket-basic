// <copyright file="Instructions.Flags.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using Core.Cpu;

/// <content>
/// Flag manipulation instructions (CLC, SEC, CLI, SEI, CLD, SED, CLV).
/// </content>
public static partial class Instructions
{
    /// <summary>
    /// CLC - Clear Carry Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLC.</returns>
    public static OpcodeHandler CLC(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Registers.P &= ~ProcessorStatusFlags.C;

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// SEC - Set Carry Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SEC.</returns>
    public static OpcodeHandler SEC(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Registers.P |= ProcessorStatusFlags.C;

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// CLI - Clear Interrupt Disable Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLI.</returns>
    public static OpcodeHandler CLI(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Registers.P &= ~ProcessorStatusFlags.I;

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// SEI - Set Interrupt Disable Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SEI.</returns>
    public static OpcodeHandler SEI(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Registers.P |= ProcessorStatusFlags.I;

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// CLD - Clear Decimal Mode Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLD.</returns>
    public static OpcodeHandler CLD(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Registers.P &= ~ProcessorStatusFlags.D;

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// SED - Set Decimal Mode Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SED.</returns>
    public static OpcodeHandler SED(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Registers.P |= ProcessorStatusFlags.D;

            cpu.Registers.TCU += 1;
        };
    }

    /// <summary>
    /// CLV - Clear Overflow Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLV.</returns>
    public static OpcodeHandler CLV(AddressingModeHandler addressingMode)
    {
        return cpu =>
        {
            addressingMode(cpu);
            cpu.Registers.P &= ~ProcessorStatusFlags.V;

            cpu.Registers.TCU += 1;
        };
    }
}