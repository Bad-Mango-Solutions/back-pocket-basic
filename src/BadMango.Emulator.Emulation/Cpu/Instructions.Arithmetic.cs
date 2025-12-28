// <copyright file="Instructions.Arithmetic.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;
using Core.Cpu;

/// <summary>
/// Arithmetic instructions (ADC, SBC, INC, DEC, INX, INY, DEX, DEY).
/// </summary>
public static partial class Instructions
{
    /// <summary>
    /// ADC - Add with Carry instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes ADC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler ADC(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            byte a = state.Registers.A.GetByte();
            byte carry = state.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)1 : (byte)0;

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.D))
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

                if (ah > 15)
                {
                    state.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.C;
                }

                state.Registers.P.SetZeroAndNegative(result);
                state.Registers.A.SetByte(result);
            }
            else
            {
                // Binary mode
                int result = a + value + carry;
                byte result8 = (byte)(result & 0xFF);

                if (result > 0xFF)
                {
                    state.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.C;
                }

                // Set overflow: (A^result) & (value^result) & 0x80
                if (((a ^ result8) & (value ^ result8) & 0x80) != 0)
                {
                    state.Registers.P |= ProcessorStatusFlags.V;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.V;
                }

                state.Registers.P.SetZeroAndNegative(result8);
                state.Registers.A.SetByte(result8);
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.ADC;

                if (state.AddressingMode == CpuAddressingModes.Immediate)
                {
                    state.OperandSize = 1;
                    state.Operands[0] = value;
                }

                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// SBC - Subtract with Carry instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes SBC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler SBC(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            byte a = state.Registers.A.GetByte();
            byte borrow = state.Registers.P.HasFlag(ProcessorStatusFlags.C) ? (byte)0 : (byte)1;

            if (state.Registers.P.HasFlag(ProcessorStatusFlags.D))
            {
                // Decimal mode
                int al = (a & 0x0F) - (value & 0x0F) - borrow;
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

                if (ah >= 0)
                {
                    state.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.C;
                }

                state.Registers.P.SetZeroAndNegative(result);
                state.Registers.A.SetByte(result);
            }
            else
            {
                // Binary mode
                int result = a - value - borrow;
                byte result8 = (byte)(result & 0xFF);

                if (result >= 0)
                {
                    state.Registers.P |= ProcessorStatusFlags.C;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.C;
                }

                // Set overflow: (A^value) & (A^result) & 0x80
                if (((a ^ value) & (a ^ result8) & 0x80) != 0)
                {
                    state.Registers.P |= ProcessorStatusFlags.V;
                }
                else
                {
                    state.Registers.P &= ~ProcessorStatusFlags.V;
                }

                state.Registers.P.SetZeroAndNegative(result8);
                state.Registers.A.SetByte(result8);
            }

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.SBC;

                if (state.AddressingMode == CpuAddressingModes.Immediate)
                {
                    state.OperandSize = 1;
                    state.Operands[0] = value;
                }

                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// INC - Increment Memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes INC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INC(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            value++;
            memory.Write(address, value);
            opCycles++; // Memory write

            state.Registers.P.SetZeroAndNegative(value);
            opCycles++; // Internal operation

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.INC;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// DEC - Decrement Memory instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use.</param>
    /// <returns>An opcode handler that executes DEC with the given addressing mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEC(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(memory, ref state);
            byte value = memory.Read(address);
            opCycles++; // Memory read

            value--;
            memory.Write(address, value);
            opCycles++; // Memory write

            state.Registers.P.SetZeroAndNegative(value);
            opCycles++; // Internal operation

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.DEC;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// INX - Increment X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes INX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INX(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte value = (byte)(state.Registers.X.GetByte() + 1);
            state.Registers.X.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.INX;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// INY - Increment Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes INY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler INY(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte value = (byte)(state.Registers.Y.GetByte() + 1);
            state.Registers.Y.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.INY;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// DEX - Decrement X Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes DEX.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEX(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte value = (byte)(state.Registers.X.GetByte() - 1);
            state.Registers.X.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.DEX;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }

    /// <summary>
    /// DEY - Decrement Y Register instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (typically Implied).</param>
    /// <returns>An opcode handler that executes DEY.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpcodeHandler DEY(AddressingModeHandler<CpuState> addressingMode)
    {
        return (memory, ref state) =>
        {
            byte opCycles = 0;
            addressingMode(memory, ref state);
            byte value = (byte)(state.Registers.Y.GetByte() - 1);
            state.Registers.Y.SetByte(value);
            state.Registers.P.SetZeroAndNegative(value);
            opCycles++;

            if (state.IsDebuggerAttached)
            {
                state.Instruction = CpuInstructions.DEY;
                state.InstructionCycles += opCycles;
            }

            state.Cycles += opCycles;
        };
    }
}