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
    public static OpcodeHandler BCC(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr targetAddr = addressingMode(memory, ref state);

            if (!state.Registers.P.HasFlag(ProcessorStatusFlags.C))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                opCycles++; // Branch taken

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
                {
                    opCycles++; // Page boundary crossed
                }
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.BCC;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// BCS - Branch if Carry Set instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BCS.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BCS(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr targetAddr = addressingMode(memory, ref state);

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.C))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                opCycles++; // Branch taken

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
                {
                    opCycles++; // Page boundary crossed
                }
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.BCS;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// BEQ - Branch if Equal (Zero set) instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BEQ.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BEQ(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr targetAddr = addressingMode(memory, ref state);

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.Z))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                opCycles++; // Branch taken

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
                {
                    opCycles++; // Page boundary crossed
                }
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.BEQ;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// BNE - Branch if Not Equal (Zero clear) instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BNE.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BNE(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr targetAddr = addressingMode(memory, ref state);

            if (!state.Registers.P.HasFlag(ProcessorStatusFlags.Z))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                opCycles++; // Branch taken

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
                {
                    opCycles++; // Page boundary crossed
                }
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.BNE;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// BMI - Branch if Minus (Negative set) instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BMI.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BMI(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr targetAddr = addressingMode(memory, ref state);

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.N))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                opCycles++; // Branch taken

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
                {
                    opCycles++; // Page boundary crossed
                }
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.BMI;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// BPL - Branch if Plus (Negative clear) instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BPL.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BPL(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr targetAddr = addressingMode(memory, ref state);

            if (!state.Registers.P.HasFlag(ProcessorStatusFlags.N))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                opCycles++; // Branch taken

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
                {
                    opCycles++; // Page boundary crossed
                }
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.BPL;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// BVC - Branch if Overflow Clear instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BVC.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BVC(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr targetAddr = addressingMode(memory, ref state);

            if (!state.Registers.P.HasFlag(ProcessorStatusFlags.V))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                opCycles++; // Branch taken

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
                {
                    opCycles++; // Page boundary crossed
                }
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.BVC;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// BVS - Branch if Overflow Set instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Relative).</param>
    /// <returns>An opcode handler that executes BVS.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler BVS(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr targetAddr = addressingMode(memory, ref state);

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.V))
            {
                Word oldPC = state.Registers.PC.GetWord();
                state.Registers.PC.SetWord((Word)targetAddr);
                opCycles++; // Branch taken

                if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
                {
                    opCycles++; // Page boundary crossed
                }
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.BVS;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
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
    public static OpcodeHandler BRA(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr targetAddr = addressingMode(memory, ref state);
            Word oldPC = state.Registers.PC.GetWord();
            state.Registers.PC.SetWord((Word)targetAddr);
            opCycles++; // Branch always taken

            if ((oldPC & 0xFF00) != (targetAddr & 0xFF00))
            {
                opCycles++; // Page boundary crossed
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.BRA;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }
}