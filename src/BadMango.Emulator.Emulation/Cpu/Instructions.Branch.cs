// <copyright file="Instructions.Branch.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;

/// <summary>
/// Branch instructions (BCC, BCS, BEQ, BNE, BMI, BPL, BVC, BVS, BRA).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// BCC - Branch if Carry Clear instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BCC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BCC(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if (!state.Registers.P.HasFlag(ProcessorStatusFlags.C))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                state.Cycles++;

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
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
    public static OpcodeHandler BCS(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.C))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                state.Cycles++;

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
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
    public static OpcodeHandler BEQ(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.Z))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                state.Cycles++;

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
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
    public static OpcodeHandler BNE(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if (!state.Registers.P.HasFlag(ProcessorStatusFlags.Z))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                state.Cycles++;

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
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
    public static OpcodeHandler BMI(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.N))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                state.Cycles++;

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
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
    public static OpcodeHandler BPL(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if (!state.Registers.P.HasFlag(ProcessorStatusFlags.N))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                state.Cycles++;

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
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
    public static OpcodeHandler BVC(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if (!state.Registers.P.HasFlag(ProcessorStatusFlags.V))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                state.Cycles++;

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
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
    public static OpcodeHandler BVS(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.V))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                state.Cycles++;

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
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
    public static OpcodeHandler BRA(AddressingMode<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            Addr targetAddr = addressingMode(memory, ref state);
            Word oldPC = state.Registers.PC.GetWord();
            state.Registers.PC.SetWord((Word)targetAddr);
            state.Cycles++;

            if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
            {
                state.Cycles++;
            }
        };
    }
}