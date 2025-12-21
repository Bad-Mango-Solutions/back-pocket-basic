// <copyright file="Instructions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
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
    private const byte FlagC = 0x01; // Carry flag
    private const byte FlagZ = 0x02; // Zero flag
    private const byte FlagI = 0x04; // Interrupt disable flag
    private const byte FlagD = 0x08; // Decimal mode flag
    private const byte FlagB = 0x10; // Break flag
    private const byte FlagV = 0x40; // Overflow flag
    private const byte FlagN = 0x80; // Negative flag
    private const Word StackBase = 0x0100;

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
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = state.P;
            SetZN(value, ref p);

            state.A = value;
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
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = state.P;
            SetZN(value, ref p);

            state.X = value;
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
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = state.P;
            SetZN(value, ref p);

            state.Y = value;
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
            Addr address = addressingMode(memory, ref state);
            memory.Write(address, state.A);
            state.Cycles++; // Memory write cycle
        };
    }

    /// <summary>
    /// NOP - No Operation instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes NOP.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> NOP(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
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
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> BRK(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state); // Call addressing mode (usually does nothing for Implied)

            // BRK causes a software interrupt
            // Total 7 cycles: 1 (opcode fetch) + 1 (PC increment) + 2 (push PC) + 1 (push P) + 2 (read IRQ vector)
            Word pc = state.PC;
            byte s = state.SP;
            byte p = state.P;

            pc++;
            memory.Write((Word)(StackBase + s--), (byte)(pc >> 8));
            memory.Write((Word)(StackBase + s--), (byte)(pc & 0xFF));
            memory.Write((Word)(StackBase + s--), (byte)(p | FlagB));
            p |= FlagI;
            pc = memory.ReadWord(0xFFFE);
            state.Cycles += 6; // 6 cycles in handler + 1 from opcode fetch in Step()

            state.PC = pc;
            state.SP = s;
            state.P = p;
            state.Halted = true; // Halt on BRK
        };
    }

    /// <summary>
    /// CLC - Clear Carry Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> CLC(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.P &= unchecked((byte)~FlagC); // Clear carry flag
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// SEC - Set Carry Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SEC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> SEC(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.P |= FlagC; // Set carry flag
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// CLI - Clear Interrupt Disable Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> CLI(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.P &= unchecked((byte)~FlagI); // Clear interrupt disable flag
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// SEI - Set Interrupt Disable Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SEI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> SEI(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.P |= FlagI; // Set interrupt disable flag
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// CLD - Clear Decimal Mode Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLD.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> CLD(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.P &= unchecked((byte)~FlagD); // Clear decimal mode flag
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// SED - Set Decimal Mode Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes SED.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> SED(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.P |= FlagD; // Set decimal mode flag
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// CLV - Clear Overflow Flag instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes CLV.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> CLV(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.P &= unchecked((byte)~FlagV); // Clear overflow flag
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
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