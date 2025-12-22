// <copyright file="InstructionsFor.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Numerics;
using System.Runtime.CompilerServices;

using Core;

/// <summary>
/// Provides generic instruction implementations for 6502-family CPUs.
/// </summary>
/// <typeparam name="TCpu">The CPU type.</typeparam>
/// <typeparam name="TRegisters">The CPU registers type.</typeparam>
/// <typeparam name="TAccumulator">The accumulator register type.</typeparam>
/// <typeparam name="TIndex">The index register type (X, Y).</typeparam>
/// <typeparam name="TStack">The stack pointer type.</typeparam>
/// <typeparam name="TProgram">The program counter type.</typeparam>
/// <typeparam name="TState">The CPU state type.</typeparam>
/// <remarks>
/// <para>
/// This generic class provides instruction implementations that work with any CPU type
/// in the 6502 family (65C02, 65816, 65832, etc.). Type parameters are specified at the class level
/// to reduce verbosity when calling methods.
/// </para>
/// <para>
/// Instructions are higher-order functions that take addressing mode delegates
/// and return opcode handlers. This enables true composition and eliminates
/// the need for separate methods for each instruction/addressing-mode combination.
/// </para>
/// <para>
/// Usage for 65C02:
/// <code>
/// using Cpu65C02Instructions = InstructionsFor&lt;Cpu65C02, Cpu65C02Registers, byte, byte, byte, Word, Cpu65C02State&gt;;
/// var handler = Cpu65C02Instructions.LDA(Cpu65C02AddressingModes.Immediate);
/// </code>
/// </para>
/// </remarks>
public static class InstructionsFor<TCpu, TRegisters, TAccumulator, TIndex, TStack, TProgram, TState>
    where TRegisters : ICpuRegisters<TAccumulator, TIndex, TStack, TProgram>
    where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    where TAccumulator : struct, INumber<TAccumulator>
    where TIndex : struct, INumber<TIndex>
    where TStack : struct, INumber<TStack>
    where TProgram : struct, IIncrementOperators<TProgram>, IBinaryInteger<TProgram>
{
    /// <summary>
    /// LDA - Load Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LDA with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<TCpu, TState> LDA(AddressingMode<TState> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = byte.CreateTruncating(state.P);
            SetZN(value, ref p);

            state.A = TAccumulator.CreateTruncating(value);
            state.P = p;
        };
    }

    /// <summary>
    /// LDX - Load X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LDX with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<TCpu, TState> LDX(AddressingMode<TState> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = byte.CreateTruncating(state.P);
            SetZN(value, ref p);

            state.X = TIndex.CreateTruncating(value);
            state.P = p;
        };
    }

    /// <summary>
    /// LDY - Load Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LDY with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<TCpu, TState> LDY(AddressingMode<TState> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = byte.CreateTruncating(state.P);
            SetZN(value, ref p);

            state.Y = TIndex.CreateTruncating(value);
            state.P = p;
        };
    }

    /// <summary>
    /// STA - Store Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STA with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<TCpu, TState> STA(AddressingMode<TState> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = byte.CreateTruncating(state.A);
            memory.Write(address, value);
            state.Cycles++; // Memory write cycle
        };
    }

    /// <summary>
    /// STX - Store X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STX with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<TCpu, TState> STX(AddressingMode<TState> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = byte.CreateTruncating(state.X);
            memory.Write(address, value);
            state.Cycles++; // Memory write cycle
        };
    }

    /// <summary>
    /// STY - Store Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STY with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<TCpu, TState> STY(AddressingMode<TState> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = byte.CreateTruncating(state.Y);
            memory.Write(address, value);
            state.Cycles++; // Memory write cycle
        };
    }

    /// <summary>
    /// NOP - No Operation instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes NOP.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<TCpu, TState> NOP(AddressingMode<TState> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state); // Call addressing mode (usually does nothing for Implied)
            state.Cycles++; // NOP takes 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// Sets the Zero and Negative flags based on a value.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="p">Reference to the processor status register.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetZN(byte value, ref byte p)
    {
        if (value == 0)
        {
            p |= 0x02; // FlagZ
        }
        else
        {
            p &= unchecked((byte)~0x02);
        }

        if ((value & 0x80) != 0)
        {
            p |= 0x80; // FlagN
        }
        else
        {
            p &= unchecked((byte)~0x80);
        }
    }

    // TODO: Add remaining instruction implementations following the same pattern.
    // The existing Instructions.cs file contains ~70 instruction methods that should be ported here.
    // Each instruction follows the same pattern:
    // 1. Accept an AddressingMode<TState> parameter
    // 2. Return an OpcodeHandler<TCpu, TState>
    // 3. Use generic type conversions (CreateTruncating) where needed
    // 4. Maintain the same cycle counting and flag logic as the original
}

