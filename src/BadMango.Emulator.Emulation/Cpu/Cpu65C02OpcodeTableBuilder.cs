// <copyright file="Cpu65C02OpcodeTableBuilder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

/// <summary>
/// Builds the opcode table for the 65C02 CPU.
/// </summary>
/// <remarks>
/// This class is responsible for mapping opcodes to instruction handlers for the 65C02 CPU.
/// Separating opcode table construction allows for easier maintenance and reuse across
/// different CPU variants (65C02, 65816, 65832).
/// </remarks>
public static class Cpu65C02OpcodeTableBuilder
{
    /// <summary>
    /// Builds the opcode table for the 65C02 CPU.
    /// </summary>
    /// <returns>An <see cref="OpcodeTable{TCpu}"/> configured for the 65C02 CPU.</returns>
    public static OpcodeTable<Cpu65C02> Build()
    {
        var handlers = new Action<Cpu65C02>[256];

        // Initialize all opcodes to illegal opcode handler
        for (int i = 0; i < 256; i++)
        {
            handlers[i] = cpu => cpu.IllegalOpcode();
        }

        // BRK - Force Break
        handlers[0x00] = cpu => cpu.BRK();

        // LDA - Load Accumulator
        handlers[0xA9] = cpu => cpu.LDA_Immediate();
        handlers[0xA5] = cpu => cpu.LDA_ZeroPage();
        handlers[0xB5] = cpu => cpu.LDA_ZeroPageX();
        handlers[0xAD] = cpu => cpu.LDA_Absolute();
        handlers[0xBD] = cpu => cpu.LDA_AbsoluteX();
        handlers[0xB9] = cpu => cpu.LDA_AbsoluteY();
        handlers[0xA1] = cpu => cpu.LDA_IndirectX();
        handlers[0xB1] = cpu => cpu.LDA_IndirectY();

        // STA - Store Accumulator
        handlers[0x85] = cpu => cpu.STA_ZeroPage();
        handlers[0x95] = cpu => cpu.STA_ZeroPageX();
        handlers[0x8D] = cpu => cpu.STA_Absolute();
        handlers[0x9D] = cpu => cpu.STA_AbsoluteX();
        handlers[0x99] = cpu => cpu.STA_AbsoluteY();
        handlers[0x81] = cpu => cpu.STA_IndirectX();
        handlers[0x91] = cpu => cpu.STA_IndirectY();

        // LDX - Load X Register
        handlers[0xA2] = cpu => cpu.LDX_Immediate();

        // LDY - Load Y Register
        handlers[0xA0] = cpu => cpu.LDY_Immediate();

        // NOP - No Operation
        handlers[0xEA] = cpu => cpu.NOP();

        return new OpcodeTable<Cpu65C02>(handlers);
    }
}