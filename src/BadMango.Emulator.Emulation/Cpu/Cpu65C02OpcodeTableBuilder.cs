// <copyright file="Cpu65C02OpcodeTableBuilder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using BadMango.Emulator.Core;

/// <summary>
/// Builds the opcode table for the 65C02 CPU.
/// </summary>
/// <remarks>
/// This class is responsible for mapping opcodes to instruction handlers for the 65C02 CPU.
/// Separating opcode table construction allows for easier maintenance and reuse across
/// different CPU variants (65C02, 65816, 65832).
/// Uses compositional pattern with shared AddressingModes and Instructions for maximum code reuse.
/// Handlers receive machine state via Cpu65C02State structure passed by reference.
/// </remarks>
public static class Cpu65C02OpcodeTableBuilder
{
    /// <summary>
    /// Builds the opcode table for the 65C02 CPU.
    /// </summary>
    /// <returns>An <see cref="OpcodeTable{TCpu, TState}"/> configured for the 65C02 CPU.</returns>
    public static OpcodeTable<Cpu65C02, Cpu65C02State> Build()
    {
        var handlers = new OpcodeHandler<Cpu65C02, Cpu65C02State>[256];

        // Initialize all opcodes to illegal opcode handler
        for (int i = 0; i < 256; i++)
        {
            handlers[i] = (cpu, memory, ref state) => cpu.IllegalOpcode();
        }

        // BRK - Force Break
        handlers[0x00] = (cpu, memory, ref state) =>
        {
            cpu.SetState(state);
            cpu.BRK();
            state = cpu.GetState();
        };

        // LDA - Load Accumulator (compositional pattern with shared addressing modes and instruction logic)
        handlers[0xA9] = LDA_Immediate;
        handlers[0xA5] = LDA_ZeroPage;
        handlers[0xB5] = LDA_ZeroPageX;
        handlers[0xAD] = LDA_Absolute;
        handlers[0xBD] = LDA_AbsoluteX;
        handlers[0xB9] = LDA_AbsoluteY;
        handlers[0xA1] = LDA_IndirectX;
        handlers[0xB1] = LDA_IndirectY;

        // STA - Store Accumulator
        handlers[0x85] = STA_ZeroPage;
        handlers[0x95] = STA_ZeroPageX;
        handlers[0x8D] = STA_Absolute;
        handlers[0x9D] = STA_AbsoluteX;
        handlers[0x99] = STA_AbsoluteY;
        handlers[0x81] = STA_IndirectX;
        handlers[0x91] = STA_IndirectY;

        // LDX - Load X Register
        handlers[0xA2] = LDX_Immediate;

        // LDY - Load Y Register
        handlers[0xA0] = LDY_Immediate;

        // NOP - No Operation
        handlers[0xEA] = (cpu, memory, ref state) =>
        {
            cpu.SetState(state);
            cpu.NOP();
            state = cpu.GetState();
        };

        return new OpcodeTable<Cpu65C02, Cpu65C02State>(handlers);
    }

    // Compositional instruction handlers using shared AddressingModes and Instructions

    /// <summary>
    /// LDA Immediate - Composes addressing mode with instruction logic.
    /// </summary>
    private static void LDA_Immediate(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;
        byte a = state.A;
        byte p = state.P;

        byte value = AddressingModes.ReadImmediate(memory, ref pc, ref cycles);
        Instructions.LDA(value, ref a, ref p);

        state.PC = pc;
        state.Cycles = cycles;
        state.A = a;
        state.P = p;
    }

    /// <summary>
    /// LDA Zero Page - Composes addressing mode with instruction logic.
    /// </summary>
    private static void LDA_ZeroPage(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;
        byte a = state.A;
        byte p = state.P;

        byte value = AddressingModes.ReadZeroPage(memory, ref pc, ref cycles);
        Instructions.LDA(value, ref a, ref p);

        state.PC = pc;
        state.Cycles = cycles;
        state.A = a;
        state.P = p;
    }

    /// <summary>
    /// LDA Zero Page,X - Composes addressing mode with instruction logic.
    /// </summary>
    private static void LDA_ZeroPageX(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;
        byte a = state.A;
        byte p = state.P;

        byte value = AddressingModes.ReadZeroPageX(memory, ref pc, state.X, ref cycles);
        Instructions.LDA(value, ref a, ref p);

        state.PC = pc;
        state.Cycles = cycles;
        state.A = a;
        state.P = p;
    }

    /// <summary>
    /// LDA Absolute - Composes addressing mode with instruction logic.
    /// </summary>
    private static void LDA_Absolute(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;
        byte a = state.A;
        byte p = state.P;

        byte value = AddressingModes.ReadAbsolute(memory, ref pc, ref cycles);
        Instructions.LDA(value, ref a, ref p);

        state.PC = pc;
        state.Cycles = cycles;
        state.A = a;
        state.P = p;
    }

    /// <summary>
    /// LDA Absolute,X - Composes addressing mode with instruction logic.
    /// </summary>
    private static void LDA_AbsoluteX(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;
        byte a = state.A;
        byte p = state.P;

        byte value = AddressingModes.ReadAbsoluteX(memory, ref pc, state.X, ref cycles);
        Instructions.LDA(value, ref a, ref p);

        state.PC = pc;
        state.Cycles = cycles;
        state.A = a;
        state.P = p;
    }

    /// <summary>
    /// LDA Absolute,Y - Composes addressing mode with instruction logic.
    /// </summary>
    private static void LDA_AbsoluteY(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;
        byte a = state.A;
        byte p = state.P;

        byte value = AddressingModes.ReadAbsoluteY(memory, ref pc, state.Y, ref cycles);
        Instructions.LDA(value, ref a, ref p);

        state.PC = pc;
        state.Cycles = cycles;
        state.A = a;
        state.P = p;
    }

    /// <summary>
    /// LDA Indirect,X - Composes addressing mode with instruction logic.
    /// </summary>
    private static void LDA_IndirectX(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;
        byte a = state.A;
        byte p = state.P;

        byte value = AddressingModes.ReadIndirectX(memory, ref pc, state.X, ref cycles);
        Instructions.LDA(value, ref a, ref p);

        state.PC = pc;
        state.Cycles = cycles;
        state.A = a;
        state.P = p;
    }

    /// <summary>
    /// LDA Indirect,Y - Composes addressing mode with instruction logic.
    /// </summary>
    private static void LDA_IndirectY(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;
        byte a = state.A;
        byte p = state.P;

        byte value = AddressingModes.ReadIndirectY(memory, ref pc, state.Y, ref cycles);
        Instructions.LDA(value, ref a, ref p);

        state.PC = pc;
        state.Cycles = cycles;
        state.A = a;
        state.P = p;
    }

    /// <summary>
    /// STA Zero Page - Composes addressing mode with store operation.
    /// </summary>
    private static void STA_ZeroPage(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;

        AddressingModes.WriteZeroPage(memory, ref pc, state.A, ref cycles);

        state.PC = pc;
        state.Cycles = cycles;
    }

    /// <summary>
    /// STA Zero Page,X - Composes addressing mode with store operation.
    /// </summary>
    private static void STA_ZeroPageX(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;

        AddressingModes.WriteZeroPageX(memory, ref pc, state.X, state.A, ref cycles);

        state.PC = pc;
        state.Cycles = cycles;
    }

    /// <summary>
    /// STA Absolute - Composes addressing mode with store operation.
    /// </summary>
    private static void STA_Absolute(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;

        AddressingModes.WriteAbsolute(memory, ref pc, state.A, ref cycles);

        state.PC = pc;
        state.Cycles = cycles;
    }

    /// <summary>
    /// STA Absolute,X - Composes addressing mode with store operation.
    /// </summary>
    private static void STA_AbsoluteX(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;

        AddressingModes.WriteAbsoluteX(memory, ref pc, state.X, state.A, ref cycles);

        state.PC = pc;
        state.Cycles = cycles;
    }

    /// <summary>
    /// STA Absolute,Y - Composes addressing mode with store operation.
    /// </summary>
    private static void STA_AbsoluteY(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;

        AddressingModes.WriteAbsoluteY(memory, ref pc, state.Y, state.A, ref cycles);

        state.PC = pc;
        state.Cycles = cycles;
    }

    /// <summary>
    /// STA Indirect,X - Composes addressing mode with store operation.
    /// </summary>
    private static void STA_IndirectX(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;

        AddressingModes.WriteIndirectX(memory, ref pc, state.X, state.A, ref cycles);

        state.PC = pc;
        state.Cycles = cycles;
    }

    /// <summary>
    /// STA Indirect,Y - Composes addressing mode with store operation.
    /// </summary>
    private static void STA_IndirectY(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;

        AddressingModes.WriteIndirectY(memory, ref pc, state.Y, state.A, ref cycles);

        state.PC = pc;
        state.Cycles = cycles;
    }

    /// <summary>
    /// LDX Immediate - Composes addressing mode with instruction logic.
    /// </summary>
    private static void LDX_Immediate(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;
        byte x = state.X;
        byte p = state.P;

        byte value = AddressingModes.ReadImmediate(memory, ref pc, ref cycles);
        Instructions.LDX(value, ref x, ref p);

        state.PC = pc;
        state.Cycles = cycles;
        state.X = x;
        state.P = p;
    }

    /// <summary>
    /// LDY Immediate - Composes addressing mode with instruction logic.
    /// </summary>
    private static void LDY_Immediate(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        ushort pc = state.PC;
        ulong cycles = state.Cycles;
        byte y = state.Y;
        byte p = state.P;

        byte value = AddressingModes.ReadImmediate(memory, ref pc, ref cycles);
        Instructions.LDY(value, ref y, ref p);

        state.PC = pc;
        state.Cycles = cycles;
        state.Y = y;
        state.P = p;
    }
}