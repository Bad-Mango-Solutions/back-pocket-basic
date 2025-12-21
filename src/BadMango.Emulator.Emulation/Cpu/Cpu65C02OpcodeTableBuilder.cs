// <copyright file="Cpu65C02OpcodeTableBuilderNew.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using BadMango.Emulator.Core;

/// <summary>
/// Builds the opcode table for the 65C02 CPU using compositional pattern.
/// </summary>
/// <remarks>
/// This builder uses true composition where addressing modes return addresses
/// and instructions are higher-order functions that accept addressing mode delegates.
/// This pattern eliminates duplication and makes it easy to add new instructions
/// and addressing modes without creating combinatorial explosion of methods.
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
            handlers[i] = IllegalOpcode;
        }

        // BRK - Force Break
        handlers[0x00] = BRK;

        // LDA - Load Accumulator (true compositional pattern)
        handlers[0xA9] = Instructions.LDA(AddressingModes.Immediate);
        handlers[0xA5] = Instructions.LDA(AddressingModes.ZeroPage);
        handlers[0xB5] = Instructions.LDA(AddressingModes.ZeroPageX);
        handlers[0xAD] = Instructions.LDA(AddressingModes.Absolute);
        handlers[0xBD] = Instructions.LDA(AddressingModes.AbsoluteX);
        handlers[0xB9] = Instructions.LDA(AddressingModes.AbsoluteY);
        handlers[0xA1] = Instructions.LDA(AddressingModes.IndirectX);
        handlers[0xB1] = Instructions.LDA(AddressingModes.IndirectY);

        // STA - Store Accumulator
        handlers[0x85] = Instructions.STA(AddressingModes.ZeroPage);
        handlers[0x95] = Instructions.STA(AddressingModes.ZeroPageX);
        handlers[0x8D] = Instructions.STA(AddressingModes.Absolute);
        handlers[0x9D] = Instructions.STA(AddressingModes.AbsoluteXWrite); // Write version always takes max cycles
        handlers[0x99] = Instructions.STA(AddressingModes.AbsoluteYWrite); // Write version always takes max cycles
        handlers[0x81] = Instructions.STA(AddressingModes.IndirectX);
        handlers[0x91] = Instructions.STA(AddressingModes.IndirectYWrite); // Write version always takes max cycles

        // LDX - Load X Register
        handlers[0xA2] = Instructions.LDX(AddressingModes.Immediate);

        // LDY - Load Y Register
        handlers[0xA0] = Instructions.LDY(AddressingModes.Immediate);

        // NOP - No Operation
        handlers[0xEA] = NOP;

        return new OpcodeTable<Cpu65C02, Cpu65C02State>(handlers);
    }

    /// <summary>
    /// BRK - Force Break instruction. Causes a software interrupt.
    /// </summary>
    private static void BRK(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        const byte FlagB = 0x10;
        const byte FlagI = 0x04;
        const ushort StackBase = 0x0100;

        // BRK causes a software interrupt
        // Total 7 cycles: 1 (opcode fetch) + 1 (PC increment) + 2 (push PC) + 1 (push P) + 2 (read IRQ vector)
        ushort pc = state.PC;
        byte s = state.S;
        byte p = state.P;
        ulong cycles = state.Cycles;

        pc++;
        memory.Write((ushort)(StackBase + s--), (byte)(pc >> 8));
        memory.Write((ushort)(StackBase + s--), (byte)(pc & 0xFF));
        memory.Write((ushort)(StackBase + s--), (byte)(p | FlagB));
        p |= FlagI;
        pc = memory.ReadWord(0xFFFE);
        cycles += 6; // 6 cycles in handler + 1 from opcode fetch in Step()

        state.PC = pc;
        state.S = s;
        state.P = p;
        state.Cycles = cycles;
        state.Halted = true; // Halt on BRK
    }

    /// <summary>
    /// NOP - No Operation instruction.
    /// </summary>
    private static void NOP(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        state.Cycles++; // Total 2 cycles (1 from FetchByte + 1 here)
    }

    /// <summary>
    /// Handles illegal/undefined opcodes by halting execution.
    /// </summary>
    private static void IllegalOpcode(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        state.Halted = true; // Halt on illegal opcode
    }
}
