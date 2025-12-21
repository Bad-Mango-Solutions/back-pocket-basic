// <copyright file="InstructionsNew.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Core;

/// <summary>
/// Provides instruction implementations that compose with addressing modes.
/// </summary>
/// <remarks>
/// Instructions are higher-order functions that take addressing mode delegates
/// and return opcode handlers. This enables true composition and eliminates
/// the need for separate methods for each instruction/addressing-mode combination.
/// </remarks>
public static class Instructions
{
    private const byte FlagZ = 0x02;
    private const byte FlagN = 0x80;

    /// <summary>
    /// LDA - Load Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LDA with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> LDA(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            ushort address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle
            
            byte a = value;
            byte p = state.P;
            SetZN(value, ref p);
            
            state.A = a;
            state.P = p;
        };
    }

    /// <summary>
    /// LDX - Load X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LDX with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> LDX(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            ushort address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle
            
            byte x = value;
            byte p = state.P;
            SetZN(value, ref p);
            
            state.X = x;
            state.P = p;
        };
    }

    /// <summary>
    /// LDY - Load Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LDY with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> LDY(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            ushort address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle
            
            byte y = value;
            byte p = state.P;
            SetZN(value, ref p);
            
            state.Y = y;
            state.P = p;
        };
    }

    /// <summary>
    /// STA - Store Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STA with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> STA(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            ushort address = addressingMode(memory, ref state);
            memory.Write(address, state.A);
            state.Cycles++; // Memory write cycle
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetZN(byte value, ref byte p)
    {
        if (value == 0)
        {
            p |= FlagZ;
        }
        else
        {
            p &= unchecked((byte)~FlagZ);
        }

        if ((value & 0x80) != 0)
        {
            p |= FlagN;
        }
        else
        {
            p &= unchecked((byte)~FlagN);
        }
    }
}
