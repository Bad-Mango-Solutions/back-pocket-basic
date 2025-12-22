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
            memory.Write((Word)(Cpu65C02Constants.StackBase + s--), (byte)(pc >> 8));
            memory.Write((Word)(Cpu65C02Constants.StackBase + s--), (byte)(pc & 0xFF));
            memory.Write((Word)(Cpu65C02Constants.StackBase + s--), (byte)(p | Cpu65C02Constants.FlagB));
            p |= Cpu65C02Constants.FlagI;
            pc = memory.ReadWord(Cpu65C02Constants.IrqVector);
            state.Cycles += 6; // 6 cycles in handler + 1 from opcode fetch in Step()

            state.PC = pc;
            state.SP = s;
            state.P = p;

            // BRK does not halt the CPU - execution continues from the IRQ vector
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
            state.P &= unchecked((byte)~Cpu65C02Constants.FlagC); // Clear carry flag
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
            state.P |= Cpu65C02Constants.FlagC; // Set carry flag
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
            state.P &= unchecked((byte)~Cpu65C02Constants.FlagI); // Clear interrupt disable flag
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
            state.P |= Cpu65C02Constants.FlagI; // Set interrupt disable flag
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
            state.P &= unchecked((byte)~Cpu65C02Constants.FlagD); // Clear decimal mode flag
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
            state.P |= Cpu65C02Constants.FlagD; // Set decimal mode flag
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
            state.P &= unchecked((byte)~Cpu65C02Constants.FlagV); // Clear overflow flag
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// STX - Store X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STX with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> STX(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            memory.Write(address, state.X);
            state.Cycles++; // Memory write cycle
        };
    }

    /// <summary>
    /// STY - Store Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STY with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> STY(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            memory.Write(address, state.Y);
            state.Cycles++; // Memory write cycle
        };
    }

    #region Register Transfer Operations

    /// <summary>
    /// TAX - Transfer Accumulator to X instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TAX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> TAX(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.X = state.A;
            byte p = state.P;
            SetZN(state.X, ref p);
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// TAY - Transfer Accumulator to Y instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TAY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> TAY(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.Y = state.A;
            byte p = state.P;
            SetZN(state.Y, ref p);
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// TXA - Transfer X to Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TXA.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> TXA(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.A = state.X;
            byte p = state.P;
            SetZN(state.A, ref p);
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// TYA - Transfer Y to Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TYA.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> TYA(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.A = state.Y;
            byte p = state.P;
            SetZN(state.A, ref p);
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// TXS - Transfer X to Stack Pointer instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TXS.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> TXS(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.SP = state.X;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// TSX - Transfer Stack Pointer to X instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes TSX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> TSX(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.X = state.SP;
            byte p = state.P;
            SetZN(state.X, ref p);
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    #endregion

    #region Stack Operations

    /// <summary>
    /// PHA - Push Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHA.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> PHA(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            memory.Write((Word)(Cpu65C02Constants.StackBase + state.SP--), state.A);
            state.Cycles += 2; // 3 cycles total (1 from fetch + 2 here)
        };
    }

    /// <summary>
    /// PHP - Push Processor Status instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHP.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> PHP(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            memory.Write((Word)(Cpu65C02Constants.StackBase + state.SP--), (byte)(state.P | Cpu65C02Constants.FlagB));
            state.Cycles += 2; // 3 cycles total (1 from fetch + 2 here)
        };
    }

    /// <summary>
    /// PLA - Pull Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLA.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> PLA(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.A = memory.Read((Word)(Cpu65C02Constants.StackBase + ++state.SP));
            byte p = state.P;
            SetZN(state.A, ref p);
            state.P = p;
            state.Cycles += 3; // 4 cycles total (1 from fetch + 3 here)
        };
    }

    /// <summary>
    /// PLP - Pull Processor Status instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLP.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> PLP(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.P = memory.Read((Word)(Cpu65C02Constants.StackBase + ++state.SP));
            state.Cycles += 3; // 4 cycles total (1 from fetch + 3 here)
        };
    }

    /// <summary>
    /// PHX - Push X Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> PHX(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            memory.Write((Word)(Cpu65C02Constants.StackBase + state.SP--), state.X);
            state.Cycles += 2; // 3 cycles total (1 from fetch + 2 here)
        };
    }

    /// <summary>
    /// PLX - Pull X Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> PLX(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.X = memory.Read((Word)(Cpu65C02Constants.StackBase + ++state.SP));
            byte p = state.P;
            SetZN(state.X, ref p);
            state.P = p;
            state.Cycles += 3; // 4 cycles total (1 from fetch + 3 here)
        };
    }

    /// <summary>
    /// PHY - Push Y Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PHY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> PHY(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            memory.Write((Word)(Cpu65C02Constants.StackBase + state.SP--), state.Y);
            state.Cycles += 2; // 3 cycles total (1 from fetch + 2 here)
        };
    }

    /// <summary>
    /// PLY - Pull Y Register instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes PLY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> PLY(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.Y = memory.Read((Word)(Cpu65C02Constants.StackBase + ++state.SP));
            byte p = state.P;
            SetZN(state.Y, ref p);
            state.P = p;
            state.Cycles += 3; // 4 cycles total (1 from fetch + 3 here)
        };
    }

    #endregion

    #region 65C02-Specific Instructions

    /// <summary>
    /// STZ - Store Zero instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STZ with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> STZ(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            memory.Write(address, 0x00);
            state.Cycles++; // Memory write cycle
        };
    }

    /// <summary>
    /// TSB - Test and Set Bits instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes TSB with the given addressing mode.</returns>
    /// <remarks>
    /// Tests the bits in the accumulator against memory, sets the Z flag if (A AND M) is zero,
    /// then sets the bits in memory that are set in the accumulator (M = M OR A).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> TSB(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = state.P;

            // Set Z flag based on A AND M
            if ((state.A & value) == 0)
            {
                p |= Cpu65C02Constants.FlagZ;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagZ);
            }

            state.P = p;

            // Set bits in memory (M = M OR A)
            value |= state.A;
            memory.Write(address, value);
            state.Cycles += 2; // Memory write cycle + extra cycle
        };
    }

    /// <summary>
    /// TRB - Test and Reset Bits instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes TRB with the given addressing mode.</returns>
    /// <remarks>
    /// Tests the bits in the accumulator against memory, sets the Z flag if (A AND M) is zero,
    /// then clears the bits in memory that are set in the accumulator (M = M AND (NOT A)).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> TRB(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = state.P;

            // Set Z flag based on A AND M
            if ((state.A & value) == 0)
            {
                p |= Cpu65C02Constants.FlagZ;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagZ);
            }

            state.P = p;

            // Clear bits in memory (M = M AND (NOT A))
            value &= unchecked((byte)~state.A);
            memory.Write(address, value);
            state.Cycles += 2; // Memory write cycle + extra cycle
        };
    }

    /// <summary>
    /// WAI - Wait for Interrupt instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes WAI.</returns>
    /// <remarks>
    /// Puts the processor into a low-power state until an interrupt occurs.
    /// The CPU will resume execution when IRQ (if I flag clear) or NMI is signaled.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> WAI(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.HaltReason = HaltState.Wai; // Wait for interrupt
            state.Cycles += 2; // 3 cycles total (1 from fetch + 2 here)
        };
    }

    /// <summary>
    /// STP - Stop the processor instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes STP.</returns>
    /// <remarks>
    /// Stops the processor permanently until a hardware reset occurs.
    /// This is the deepest halt state and cannot be resumed by interrupts.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> STP(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.HaltReason = HaltState.Stp; // Permanent halt
            state.Cycles += 2; // 3 cycles total (1 from fetch + 2 here)
        };
    }

    #endregion

    #region Jump and Subroutine Operations

    /// <summary>
    /// JMP - Jump instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (Absolute or Indirect).</param>
    /// <returns>An opcode handler that executes JMP.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> JMP(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);
            state.PC = (Word)targetAddr;

            // No additional cycles - addressing mode handles it
        };
    }

    /// <summary>
    /// JSR - Jump to Subroutine instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Absolute).</param>
    /// <returns>An opcode handler that executes JSR.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> JSR(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            // JSR pushes PC - 1 to the stack (6502 behavior)
            // This is because RTS increments the pulled address before setting PC
            Word returnAddr = (Word)(state.PC - 1);
            memory.Write((Word)(Cpu65C02Constants.StackBase + state.SP--), (byte)(returnAddr >> 8));
            memory.Write((Word)(Cpu65C02Constants.StackBase + state.SP--), (byte)(returnAddr & 0xFF));
            state.PC = (Word)targetAddr;
            state.Cycles += 3; // 6 cycles total (1 from fetch + 2 from addressing + 3 here)
        };
    }

    /// <summary>
    /// RTS - Return from Subroutine instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes RTS.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> RTS(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte lo = memory.Read((Word)(Cpu65C02Constants.StackBase + ++state.SP));
            byte hi = memory.Read((Word)(Cpu65C02Constants.StackBase + ++state.SP));
            state.PC = (Word)((hi << 8) | lo);
            state.PC++;
            state.Cycles += 5; // 6 cycles total (1 from fetch + 5 here)
        };
    }

    /// <summary>
    /// RTI - Return from Interrupt instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes RTI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> RTI(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.P = memory.Read((Word)(Cpu65C02Constants.StackBase + ++state.SP));
            byte lo = memory.Read((Word)(Cpu65C02Constants.StackBase + ++state.SP));
            byte hi = memory.Read((Word)(Cpu65C02Constants.StackBase + ++state.SP));
            state.PC = (Word)((hi << 8) | lo);
            state.HaltReason = HaltState.None; // Clear halt state when returning from interrupt
            state.Cycles += 5; // 6 cycles total (1 from fetch + 5 here)
        };
    }

    #endregion

    #region Comparison Operations

    /// <summary>
    /// CMP - Compare Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes CMP with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> CMP(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte result = (byte)(state.A - value);
            byte p = state.P;

            // Set carry if A >= value
            if (state.A >= value)
            {
                p |= Cpu65C02Constants.FlagC;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagC);
            }

            SetZN(result, ref p);
            state.P = p;
        };
    }

    /// <summary>
    /// CPX - Compare X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes CPX with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> CPX(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte result = (byte)(state.X - value);
            byte p = state.P;

            // Set carry if X >= value
            if (state.X >= value)
            {
                p |= Cpu65C02Constants.FlagC;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagC);
            }

            SetZN(result, ref p);
            state.P = p;
        };
    }

    /// <summary>
    /// CPY - Compare Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes CPY with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> CPY(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte result = (byte)(state.Y - value);
            byte p = state.P;

            // Set carry if Y >= value
            if (state.Y >= value)
            {
                p |= Cpu65C02Constants.FlagC;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagC);
            }

            SetZN(result, ref p);
            state.P = p;
        };
    }

    #endregion

    #region Branch Operations

    /// <summary>
    /// BCC - Branch if Carry Clear instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BCC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> BCC(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if ((state.P & Cpu65C02Constants.FlagC) == 0)
            {
                Word oldPC = state.PC;
                state.PC = (Word)targetAddr;
                state.Cycles++; // Add 1 cycle for branch taken

                // Add 1 more cycle if page boundary crossed
                if ((oldPC & 0xFF00) != (state.PC & 0xFF00))
                {
                    state.Cycles++;
                }
            }
        };
    }

    /// <summary>
    /// BCS - Branch if Carry Set instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BCS.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> BCS(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if ((state.P & Cpu65C02Constants.FlagC) != 0)
            {
                Word oldPC = state.PC;
                state.PC = (Word)targetAddr;
                state.Cycles++; // Add 1 cycle for branch taken

                // Add 1 more cycle if page boundary crossed
                if ((oldPC & 0xFF00) != (state.PC & 0xFF00))
                {
                    state.Cycles++;
                }
            }
        };
    }

    /// <summary>
    /// BEQ - Branch if Equal (Zero set) instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BEQ.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> BEQ(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if ((state.P & Cpu65C02Constants.FlagZ) != 0)
            {
                Word oldPC = state.PC;
                state.PC = (Word)targetAddr;
                state.Cycles++; // Add 1 cycle for branch taken

                // Add 1 more cycle if page boundary crossed
                if ((oldPC & 0xFF00) != (state.PC & 0xFF00))
                {
                    state.Cycles++;
                }
            }
        };
    }

    /// <summary>
    /// BNE - Branch if Not Equal (Zero clear) instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BNE.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> BNE(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if ((state.P & Cpu65C02Constants.FlagZ) == 0)
            {
                Word oldPC = state.PC;
                state.PC = (Word)targetAddr;
                state.Cycles++; // Add 1 cycle for branch taken

                // Add 1 more cycle if page boundary crossed
                if ((oldPC & 0xFF00) != (state.PC & 0xFF00))
                {
                    state.Cycles++;
                }
            }
        };
    }

    /// <summary>
    /// BMI - Branch if Minus (Negative set) instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BMI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> BMI(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if ((state.P & Cpu65C02Constants.FlagN) != 0)
            {
                Word oldPC = state.PC;
                state.PC = (Word)targetAddr;
                state.Cycles++; // Add 1 cycle for branch taken

                // Add 1 more cycle if page boundary crossed
                if ((oldPC & 0xFF00) != (state.PC & 0xFF00))
                {
                    state.Cycles++;
                }
            }
        };
    }

    /// <summary>
    /// BPL - Branch if Plus (Negative clear) instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BPL.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> BPL(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if ((state.P & Cpu65C02Constants.FlagN) == 0)
            {
                Word oldPC = state.PC;
                state.PC = (Word)targetAddr;
                state.Cycles++; // Add 1 cycle for branch taken

                // Add 1 more cycle if page boundary crossed
                if ((oldPC & 0xFF00) != (state.PC & 0xFF00))
                {
                    state.Cycles++;
                }
            }
        };
    }

    /// <summary>
    /// BVC - Branch if Overflow Clear instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BVC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> BVC(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if ((state.P & Cpu65C02Constants.FlagV) == 0)
            {
                Word oldPC = state.PC;
                state.PC = (Word)targetAddr;
                state.Cycles++; // Add 1 cycle for branch taken

                // Add 1 more cycle if page boundary crossed
                if ((oldPC & 0xFF00) != (state.PC & 0xFF00))
                {
                    state.Cycles++;
                }
            }
        };
    }

    /// <summary>
    /// BVS - Branch if Overflow Set instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BVS.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> BVS(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if ((state.P & Cpu65C02Constants.FlagV) != 0)
            {
                Word oldPC = state.PC;
                state.PC = (Word)targetAddr;
                state.Cycles++; // Add 1 cycle for branch taken

                // Add 1 more cycle if page boundary crossed
                if ((oldPC & 0xFF00) != (state.PC & 0xFF00))
                {
                    state.Cycles++;
                }
            }
        };
    }

    /// <summary>
    /// BRA - Branch Always instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BRA.</returns>
    /// <remarks>
    /// This instruction is unique to the 65C02 and was not present in the original 6502.
    /// It always branches unconditionally.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> BRA(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);
            Word oldPC = state.PC;
            state.PC = (Word)targetAddr;
            state.Cycles++; // Add 1 cycle for branch taken

            // Add 1 more cycle if page boundary crossed
            if ((oldPC & 0xFF00) != (state.PC & 0xFF00))
            {
                state.Cycles++;
            }
        };
    }

    #endregion

    #region Arithmetic Operations

    /// <summary>
    /// ADC - Add with Carry instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ADC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> ADC(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte a = state.A;
            byte p = state.P;
            byte carry = (byte)((p & Cpu65C02Constants.FlagC) != 0 ? 1 : 0);

            if ((p & Cpu65C02Constants.FlagD) != 0)
            {
                // Decimal mode
                int al = (a & 0x0F) + (value & 0x0F) + carry;
                if (al > 9)
                {
                    al += 6;
                }

                int ah = (a >> 4) + (value >> 4) + (al > 15 ? 1 : 0);
                if (ah > 9)
                {
                    ah += 6;
                }

                byte result = (byte)(((ah << 4) | (al & 0x0F)) & 0xFF);

                // Set carry
                if (ah > 15)
                {
                    p |= Cpu65C02Constants.FlagC;
                }
                else
                {
                    p &= unchecked((byte)~Cpu65C02Constants.FlagC);
                }

                // Set overflow (not affected in decimal mode on 65C02)
                SetZN(result, ref p);
                state.A = result;
            }
            else
            {
                // Binary mode
                int result = a + value + carry;
                byte result8 = (byte)(result & 0xFF);

                // Set carry
                if (result > 0xFF)
                {
                    p |= Cpu65C02Constants.FlagC;
                }
                else
                {
                    p &= unchecked((byte)~Cpu65C02Constants.FlagC);
                }

                // Set overflow: (A^result) & (value^result) & 0x80
                if (((a ^ result8) & (value ^ result8) & 0x80) != 0)
                {
                    p |= Cpu65C02Constants.FlagV;
                }
                else
                {
                    p &= unchecked((byte)~Cpu65C02Constants.FlagV);
                }

                SetZN(result8, ref p);
                state.A = result8;
            }

            state.P = p;
        };
    }

    /// <summary>
    /// SBC - Subtract with Carry instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes SBC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> SBC(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte a = state.A;
            byte p = state.P;
            byte carry = (byte)((p & Cpu65C02Constants.FlagC) != 0 ? 0 : 1); // Borrow is inverted carry

            if ((p & Cpu65C02Constants.FlagD) != 0)
            {
                // Decimal mode
                int al = (a & 0x0F) - (value & 0x0F) - carry;
                if (al < 0)
                {
                    al -= 6;
                }

                int ah = (a >> 4) - (value >> 4) - (al < 0 ? 1 : 0);
                if (ah < 0)
                {
                    ah -= 6;
                }

                byte result = (byte)(((ah << 4) | (al & 0x0F)) & 0xFF);

                // Set carry (inverted borrow)
                if (ah >= 0)
                {
                    p |= Cpu65C02Constants.FlagC;
                }
                else
                {
                    p &= unchecked((byte)~Cpu65C02Constants.FlagC);
                }

                SetZN(result, ref p);
                state.A = result;
            }
            else
            {
                // Binary mode
                int result = a - value - carry;
                byte result8 = (byte)(result & 0xFF);

                // Set carry (inverted borrow)
                if (result >= 0)
                {
                    p |= Cpu65C02Constants.FlagC;
                }
                else
                {
                    p &= unchecked((byte)~Cpu65C02Constants.FlagC);
                }

                // Set overflow: (A^value) & (A^result) & 0x80
                if (((a ^ value) & (a ^ result8) & 0x80) != 0)
                {
                    p |= Cpu65C02Constants.FlagV;
                }
                else
                {
                    p &= unchecked((byte)~Cpu65C02Constants.FlagV);
                }

                SetZN(result8, ref p);
                state.A = result8;
            }

            state.P = p;
        };
    }

    /// <summary>
    /// INC - Increment Memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes INC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> INC(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            value++;
            memory.Write(address, value);
            state.Cycles++; // Memory write cycle

            byte p = state.P;
            SetZN(value, ref p);
            state.P = p;

            // Extra cycle for read-modify-write operations (6502 hardware behavior)
            state.Cycles++;
        };
    }

    /// <summary>
    /// DEC - Decrement Memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes DEC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> DEC(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            value--;
            memory.Write(address, value);
            state.Cycles++; // Memory write cycle

            byte p = state.P;
            SetZN(value, ref p);
            state.P = p;

            // Extra cycle for read-modify-write operations (6502 hardware behavior)
            state.Cycles++;
        };
    }

    /// <summary>
    /// INX - Increment X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes INX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> INX(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.X++;
            byte p = state.P;
            SetZN(state.X, ref p);
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// INY - Increment Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes INY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> INY(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.Y++;
            byte p = state.P;
            SetZN(state.Y, ref p);
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// DEX - Decrement X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes DEX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> DEX(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.X--;
            byte p = state.P;
            SetZN(state.X, ref p);
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// DEY - Decrement Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes DEY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> DEY(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            state.Y--;
            byte p = state.P;
            SetZN(state.Y, ref p);
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    #endregion

    #region Logical Operations

    /// <summary>
    /// AND - Logical AND instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes AND with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> AND(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            state.A &= value;
            byte p = state.P;
            SetZN(state.A, ref p);
            state.P = p;
        };
    }

    /// <summary>
    /// ORA - Logical OR instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ORA with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> ORA(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            state.A |= value;
            byte p = state.P;
            SetZN(state.A, ref p);
            state.P = p;
        };
    }

    /// <summary>
    /// EOR - Exclusive OR instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes EOR with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> EOR(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            state.A ^= value;
            byte p = state.P;
            SetZN(state.A, ref p);
            state.P = p;
        };
    }

    /// <summary>
    /// BIT - Bit Test instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes BIT with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> BIT(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = state.P;
            byte result = (byte)(state.A & value);

            // Set Z flag based on result
            if (result == 0)
            {
                p |= Cpu65C02Constants.FlagZ;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagZ);
            }

            // Set N flag from bit 7 of memory value
            if ((value & 0x80) != 0)
            {
                p |= Cpu65C02Constants.FlagN;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagN);
            }

            // Set V flag from bit 6 of memory value
            if ((value & 0x40) != 0)
            {
                p |= Cpu65C02Constants.FlagV;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagV);
            }

            state.P = p;
        };
    }

    #endregion

    #region Shift and Rotate Operations

    /// <summary>
    /// ASLa - Arithmetic Shift Left Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes ASL on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> ASLa(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = state.A;
            byte p = state.P;

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                p |= Cpu65C02Constants.FlagC;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagC);
            }

            value <<= 1;
            SetZN(value, ref p);
            state.A = value;
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// ASL - Arithmetic Shift Left memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ASL on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> ASL(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = state.P;

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                p |= Cpu65C02Constants.FlagC;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagC);
            }

            value <<= 1;
            SetZN(value, ref p);
            state.P = p;

            memory.Write(address, value);
            state.Cycles += 2; // Memory write cycle + extra cycle
        };
    }

    /// <summary>
    /// LSRa - Logical Shift Right Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes LSR on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> LSRa(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = state.A;
            byte p = state.P;

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                p |= Cpu65C02Constants.FlagC;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagC);
            }

            value >>= 1;
            SetZN(value, ref p);
            state.A = value;
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// LSR - Logical Shift Right memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes LSR on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> LSR(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = state.P;

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                p |= Cpu65C02Constants.FlagC;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagC);
            }

            value >>= 1;
            SetZN(value, ref p);
            state.P = p;

            memory.Write(address, value);
            state.Cycles += 2; // Memory write cycle + extra cycle
        };
    }

    /// <summary>
    /// ROLa - Rotate Left Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes ROL on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> ROLa(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = state.A;
            byte p = state.P;
            byte oldCarry = (byte)((p & Cpu65C02Constants.FlagC) != 0 ? 1 : 0);

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                p |= Cpu65C02Constants.FlagC;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagC);
            }

            value = (byte)((value << 1) | oldCarry);
            SetZN(value, ref p);
            state.A = value;
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// ROL - Rotate Left memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ROL on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> ROL(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = state.P;
            byte oldCarry = (byte)((p & Cpu65C02Constants.FlagC) != 0 ? 1 : 0);

            // Set carry from bit 7
            if ((value & 0x80) != 0)
            {
                p |= Cpu65C02Constants.FlagC;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagC);
            }

            value = (byte)((value << 1) | oldCarry);
            SetZN(value, ref p);
            state.P = p;

            memory.Write(address, value);
            state.Cycles += 2; // Memory write cycle + extra cycle
        };
    }

    /// <summary>
    /// RORa - Rotate Right Accumulator instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be Accumulator).</param>
    /// <returns>An opcode handler that executes ROR on the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> RORa(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            addressingMode(memory, ref state);
            byte value = state.A;
            byte p = state.P;
            byte oldCarry = (byte)((p & Cpu65C02Constants.FlagC) != 0 ? 0x80 : 0);

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                p |= Cpu65C02Constants.FlagC;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagC);
            }

            value = (byte)((value >> 1) | oldCarry);
            SetZN(value, ref p);
            state.A = value;
            state.P = p;
            state.Cycles++; // 2 cycles total (1 from fetch + 1 here)
        };
    }

    /// <summary>
    /// ROR - Rotate Right memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ROR on memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler<Cpu65C02, Cpu65C02State> ROR(AddressingMode<Cpu65C02State> addressingMode)
    {
        return (cpu, memory, ref state) =>
        {
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            state.Cycles++; // Memory read cycle

            byte p = state.P;
            byte oldCarry = (byte)((p & Cpu65C02Constants.FlagC) != 0 ? 0x80 : 0);

            // Set carry from bit 0
            if ((value & 0x01) != 0)
            {
                p |= Cpu65C02Constants.FlagC;
            }
            else
            {
                p &= unchecked((byte)~Cpu65C02Constants.FlagC);
            }

            value = (byte)((value >> 1) | oldCarry);
            SetZN(value, ref p);
            state.P = p;

            memory.Write(address, value);
            state.Cycles += 2; // Memory write cycle + extra cycle
        };
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetZN(byte value, ref byte p)
    {
        if (value == 0)
        {
            p |= Cpu65C02Constants.FlagZ;
        }
        else
        {
            p &= unchecked((byte)~Cpu65C02Constants.FlagZ);
        }

        if ((value & 0x80) != 0)
        {
            p |= Cpu65C02Constants.FlagN;
        }
        else
        {
            p &= unchecked((byte)~Cpu65C02Constants.FlagN);
        }
    }
}