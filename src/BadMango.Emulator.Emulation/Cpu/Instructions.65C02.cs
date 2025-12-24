// <copyright file="Instructions.65C02.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;

/// <summary>
/// 65C02-specific instructions (STZ, TSB, TRB, WAI, STP).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// STZ - Store Zero instruction (65C02 specific).
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes STZ with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler STZ(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            memory.Write(address, 0x00);
            opCycles++; // Memory write

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.STZ;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
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
    public static OpcodeHandler TSB(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            byte a = state.Registers.A.GetByte();

            // Set Z flag based on A AND M
            if ((a & value) == 0)
            {
                state.Registers.P |= ProcessorStatusFlags.Z;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.Z;
            }

            // Set bits in memory (M = M OR A)
            value |= a;
            memory.Write(address, value);
            opCycles += 2; // Memory write + internal operation

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.TSB;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
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
    public static OpcodeHandler TRB(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            byte a = state.Registers.A.GetByte();

            // Set Z flag based on A AND M
            if ((a & value) == 0)
            {
                state.Registers.P |= ProcessorStatusFlags.Z;
            }
            else
            {
                state.Registers.P &= ~ProcessorStatusFlags.Z;
            }

            // Clear bits in memory (M = M AND (NOT A))
            value &= (byte)~a;
            memory.Write(address, value);
            opCycles += 2; // Memory write + internal operation

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.TRB;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
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
    public static OpcodeHandler WAI(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            state.HaltReason = HaltState.Wai;
            opCycles += 2;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.WAI;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
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
    public static OpcodeHandler STP(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            state.HaltReason = HaltState.Stp;
            opCycles += 2;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.STP;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }
}