// <copyright file="Instructions.Flags.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;

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
    public static OpcodeHandler CLC(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.Registers.P &= ~ProcessorStatusFlags.C;
            state.Cycles++;
        };
    }

    /// <summary>
    /// SEC - Set Carry Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SEC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SEC(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.Registers.P |= ProcessorStatusFlags.C;
            state.Cycles++;
        };
    }

    /// <summary>
    /// CLI - Clear Interrupt Disable Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CLI(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.Registers.P &= ~ProcessorStatusFlags.I;
            state.Cycles++;
        };
    }

    /// <summary>
    /// SEI - Set Interrupt Disable Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SEI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SEI(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.Registers.P |= ProcessorStatusFlags.I;
            state.Cycles++;
        };
    }

    /// <summary>
    /// CLD - Clear Decimal Mode Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLD.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CLD(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.Registers.P &= ~ProcessorStatusFlags.D;
            state.Cycles++;
        };
    }

    /// <summary>
    /// SED - Set Decimal Mode Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SED.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SED(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.Registers.P |= ProcessorStatusFlags.D;
            state.Cycles++;
        };
    }

    /// <summary>
    /// CLV - Clear Overflow Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLV.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler CLV(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.Registers.P &= ~ProcessorStatusFlags.V;
            state.Cycles++;
        };
    }
}