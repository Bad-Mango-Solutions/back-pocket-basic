// <copyright file="Cpu65C02OpcodeTableBuilder.cs" company="Bad Mango Solutions">
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
        handlers[0x00] = Instructions.BRK(AddressingModes.Implied);

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
        handlers[0xEA] = Instructions.NOP(AddressingModes.Implied);

        // Flag manipulation instructions (all use Implied addressing)
        handlers[0x18] = Instructions.CLC(AddressingModes.Implied); // Clear Carry
        handlers[0x38] = Instructions.SEC(AddressingModes.Implied); // Set Carry
        handlers[0x58] = Instructions.CLI(AddressingModes.Implied); // Clear Interrupt Disable
        handlers[0x78] = Instructions.SEI(AddressingModes.Implied); // Set Interrupt Disable
        handlers[0xD8] = Instructions.CLD(AddressingModes.Implied); // Clear Decimal
        handlers[0xF8] = Instructions.SED(AddressingModes.Implied); // Set Decimal
        handlers[0xB8] = Instructions.CLV(AddressingModes.Implied); // Clear Overflow

        // LDX - Additional addressing modes
        handlers[0xA6] = Instructions.LDX(AddressingModes.ZeroPage);
        handlers[0xB6] = Instructions.LDX(AddressingModes.ZeroPageY);
        handlers[0xAE] = Instructions.LDX(AddressingModes.Absolute);
        handlers[0xBE] = Instructions.LDX(AddressingModes.AbsoluteY);

        // LDY - Additional addressing modes
        handlers[0xA4] = Instructions.LDY(AddressingModes.ZeroPage);
        handlers[0xB4] = Instructions.LDY(AddressingModes.ZeroPageX);
        handlers[0xAC] = Instructions.LDY(AddressingModes.Absolute);
        handlers[0xBC] = Instructions.LDY(AddressingModes.AbsoluteX);

        // STX - Store X Register
        handlers[0x86] = Instructions.STX(AddressingModes.ZeroPage);
        handlers[0x96] = Instructions.STX(AddressingModes.ZeroPageY);
        handlers[0x8E] = Instructions.STX(AddressingModes.Absolute);

        // STY - Store Y Register
        handlers[0x84] = Instructions.STY(AddressingModes.ZeroPage);
        handlers[0x94] = Instructions.STY(AddressingModes.ZeroPageX);
        handlers[0x8C] = Instructions.STY(AddressingModes.Absolute);

        // Register Transfer Operations
        handlers[0xAA] = Instructions.TAX(AddressingModes.Implied); // Transfer A to X
        handlers[0xA8] = Instructions.TAY(AddressingModes.Implied); // Transfer A to Y
        handlers[0x8A] = Instructions.TXA(AddressingModes.Implied); // Transfer X to A
        handlers[0x98] = Instructions.TYA(AddressingModes.Implied); // Transfer Y to A
        handlers[0x9A] = Instructions.TXS(AddressingModes.Implied); // Transfer X to SP
        handlers[0xBA] = Instructions.TSX(AddressingModes.Implied); // Transfer SP to X

        // Stack Operations
        handlers[0x48] = Instructions.PHA(AddressingModes.Implied); // Push Accumulator
        handlers[0x08] = Instructions.PHP(AddressingModes.Implied); // Push Processor Status
        handlers[0x68] = Instructions.PLA(AddressingModes.Implied); // Pull Accumulator
        handlers[0x28] = Instructions.PLP(AddressingModes.Implied); // Pull Processor Status
        handlers[0xDA] = Instructions.PHX(AddressingModes.Implied); // Push X (65C02)
        handlers[0xFA] = Instructions.PLX(AddressingModes.Implied); // Pull X (65C02)
        handlers[0x5A] = Instructions.PHY(AddressingModes.Implied); // Push Y (65C02)
        handlers[0x7A] = Instructions.PLY(AddressingModes.Implied); // Pull Y (65C02)

        // 65C02-Specific Instructions
        handlers[0x64] = Instructions.STZ(AddressingModes.ZeroPage); // Store Zero
        handlers[0x74] = Instructions.STZ(AddressingModes.ZeroPageX); // Store Zero
        handlers[0x9C] = Instructions.STZ(AddressingModes.Absolute); // Store Zero
        handlers[0x9E] = Instructions.STZ(AddressingModes.AbsoluteX); // Store Zero
        handlers[0x04] = Instructions.TSB(AddressingModes.ZeroPage); // Test and Set Bits
        handlers[0x0C] = Instructions.TSB(AddressingModes.Absolute); // Test and Set Bits
        handlers[0x14] = Instructions.TRB(AddressingModes.ZeroPage); // Test and Reset Bits
        handlers[0x1C] = Instructions.TRB(AddressingModes.Absolute); // Test and Reset Bits
        handlers[0xCB] = Instructions.WAI(AddressingModes.Implied); // Wait for Interrupt
        handlers[0xDB] = Instructions.STP(AddressingModes.Implied); // Stop Processor

        // Jump and Subroutine Operations
        handlers[0x4C] = Instructions.JMP(AddressingModes.Absolute); // Jump Absolute
        handlers[0x6C] = Instructions.JMP(AddressingModes.Indirect); // Jump Indirect
        handlers[0x20] = Instructions.JSR(AddressingModes.Absolute); // Jump to Subroutine
        handlers[0x60] = Instructions.RTS(AddressingModes.Implied); // Return from Subroutine
        handlers[0x40] = Instructions.RTI(AddressingModes.Implied); // Return from Interrupt

        // Comparison Operations
        handlers[0xC9] = Instructions.CMP(AddressingModes.Immediate);
        handlers[0xC5] = Instructions.CMP(AddressingModes.ZeroPage);
        handlers[0xD5] = Instructions.CMP(AddressingModes.ZeroPageX);
        handlers[0xCD] = Instructions.CMP(AddressingModes.Absolute);
        handlers[0xDD] = Instructions.CMP(AddressingModes.AbsoluteX);
        handlers[0xD9] = Instructions.CMP(AddressingModes.AbsoluteY);
        handlers[0xC1] = Instructions.CMP(AddressingModes.IndirectX);
        handlers[0xD1] = Instructions.CMP(AddressingModes.IndirectY);

        handlers[0xE0] = Instructions.CPX(AddressingModes.Immediate);
        handlers[0xE4] = Instructions.CPX(AddressingModes.ZeroPage);
        handlers[0xEC] = Instructions.CPX(AddressingModes.Absolute);

        handlers[0xC0] = Instructions.CPY(AddressingModes.Immediate);
        handlers[0xC4] = Instructions.CPY(AddressingModes.ZeroPage);
        handlers[0xCC] = Instructions.CPY(AddressingModes.Absolute);

        // Branch Operations
        handlers[0x90] = Instructions.BCC(AddressingModes.Relative); // Branch if Carry Clear
        handlers[0xB0] = Instructions.BCS(AddressingModes.Relative); // Branch if Carry Set
        handlers[0xF0] = Instructions.BEQ(AddressingModes.Relative); // Branch if Equal
        handlers[0xD0] = Instructions.BNE(AddressingModes.Relative); // Branch if Not Equal
        handlers[0x30] = Instructions.BMI(AddressingModes.Relative); // Branch if Minus
        handlers[0x10] = Instructions.BPL(AddressingModes.Relative); // Branch if Plus
        handlers[0x50] = Instructions.BVC(AddressingModes.Relative); // Branch if Overflow Clear
        handlers[0x70] = Instructions.BVS(AddressingModes.Relative); // Branch if Overflow Set
        handlers[0x80] = Instructions.BRA(AddressingModes.Relative); // Branch Always (65C02)

        // Arithmetic Operations
        handlers[0x69] = Instructions.ADC(AddressingModes.Immediate);
        handlers[0x65] = Instructions.ADC(AddressingModes.ZeroPage);
        handlers[0x75] = Instructions.ADC(AddressingModes.ZeroPageX);
        handlers[0x6D] = Instructions.ADC(AddressingModes.Absolute);
        handlers[0x7D] = Instructions.ADC(AddressingModes.AbsoluteX);
        handlers[0x79] = Instructions.ADC(AddressingModes.AbsoluteY);
        handlers[0x61] = Instructions.ADC(AddressingModes.IndirectX);
        handlers[0x71] = Instructions.ADC(AddressingModes.IndirectY);

        handlers[0xE9] = Instructions.SBC(AddressingModes.Immediate);
        handlers[0xE5] = Instructions.SBC(AddressingModes.ZeroPage);
        handlers[0xF5] = Instructions.SBC(AddressingModes.ZeroPageX);
        handlers[0xED] = Instructions.SBC(AddressingModes.Absolute);
        handlers[0xFD] = Instructions.SBC(AddressingModes.AbsoluteX);
        handlers[0xF9] = Instructions.SBC(AddressingModes.AbsoluteY);
        handlers[0xE1] = Instructions.SBC(AddressingModes.IndirectX);
        handlers[0xF1] = Instructions.SBC(AddressingModes.IndirectY);

        handlers[0xE6] = Instructions.INC(AddressingModes.ZeroPage);
        handlers[0xF6] = Instructions.INC(AddressingModes.ZeroPageX);
        handlers[0xEE] = Instructions.INC(AddressingModes.Absolute);
        handlers[0xFE] = Instructions.INC(AddressingModes.AbsoluteX);

        handlers[0xC6] = Instructions.DEC(AddressingModes.ZeroPage);
        handlers[0xD6] = Instructions.DEC(AddressingModes.ZeroPageX);
        handlers[0xCE] = Instructions.DEC(AddressingModes.Absolute);
        handlers[0xDE] = Instructions.DEC(AddressingModes.AbsoluteX);

        handlers[0xE8] = Instructions.INX(AddressingModes.Implied);
        handlers[0xC8] = Instructions.INY(AddressingModes.Implied);
        handlers[0xCA] = Instructions.DEX(AddressingModes.Implied);
        handlers[0x88] = Instructions.DEY(AddressingModes.Implied);

        // Logical Operations
        handlers[0x29] = Instructions.AND(AddressingModes.Immediate);
        handlers[0x25] = Instructions.AND(AddressingModes.ZeroPage);
        handlers[0x35] = Instructions.AND(AddressingModes.ZeroPageX);
        handlers[0x2D] = Instructions.AND(AddressingModes.Absolute);
        handlers[0x3D] = Instructions.AND(AddressingModes.AbsoluteX);
        handlers[0x39] = Instructions.AND(AddressingModes.AbsoluteY);
        handlers[0x21] = Instructions.AND(AddressingModes.IndirectX);
        handlers[0x31] = Instructions.AND(AddressingModes.IndirectY);

        handlers[0x09] = Instructions.ORA(AddressingModes.Immediate);
        handlers[0x05] = Instructions.ORA(AddressingModes.ZeroPage);
        handlers[0x15] = Instructions.ORA(AddressingModes.ZeroPageX);
        handlers[0x0D] = Instructions.ORA(AddressingModes.Absolute);
        handlers[0x1D] = Instructions.ORA(AddressingModes.AbsoluteX);
        handlers[0x19] = Instructions.ORA(AddressingModes.AbsoluteY);
        handlers[0x01] = Instructions.ORA(AddressingModes.IndirectX);
        handlers[0x11] = Instructions.ORA(AddressingModes.IndirectY);

        handlers[0x49] = Instructions.EOR(AddressingModes.Immediate);
        handlers[0x45] = Instructions.EOR(AddressingModes.ZeroPage);
        handlers[0x55] = Instructions.EOR(AddressingModes.ZeroPageX);
        handlers[0x4D] = Instructions.EOR(AddressingModes.Absolute);
        handlers[0x5D] = Instructions.EOR(AddressingModes.AbsoluteX);
        handlers[0x59] = Instructions.EOR(AddressingModes.AbsoluteY);
        handlers[0x41] = Instructions.EOR(AddressingModes.IndirectX);
        handlers[0x51] = Instructions.EOR(AddressingModes.IndirectY);

        handlers[0x24] = Instructions.BIT(AddressingModes.ZeroPage);
        handlers[0x2C] = Instructions.BIT(AddressingModes.Absolute);

        // Shift and Rotate Operations
        handlers[0x0A] = Instructions.ASLa(AddressingModes.Accumulator);
        handlers[0x06] = Instructions.ASL(AddressingModes.ZeroPage);
        handlers[0x16] = Instructions.ASL(AddressingModes.ZeroPageX);
        handlers[0x0E] = Instructions.ASL(AddressingModes.Absolute);
        handlers[0x1E] = Instructions.ASL(AddressingModes.AbsoluteX);

        handlers[0x4A] = Instructions.LSRa(AddressingModes.Accumulator);
        handlers[0x46] = Instructions.LSR(AddressingModes.ZeroPage);
        handlers[0x56] = Instructions.LSR(AddressingModes.ZeroPageX);
        handlers[0x4E] = Instructions.LSR(AddressingModes.Absolute);
        handlers[0x5E] = Instructions.LSR(AddressingModes.AbsoluteX);

        handlers[0x2A] = Instructions.ROLa(AddressingModes.Accumulator);
        handlers[0x26] = Instructions.ROL(AddressingModes.ZeroPage);
        handlers[0x36] = Instructions.ROL(AddressingModes.ZeroPageX);
        handlers[0x2E] = Instructions.ROL(AddressingModes.Absolute);
        handlers[0x3E] = Instructions.ROL(AddressingModes.AbsoluteX);

        handlers[0x6A] = Instructions.RORa(AddressingModes.Accumulator);
        handlers[0x66] = Instructions.ROR(AddressingModes.ZeroPage);
        handlers[0x76] = Instructions.ROR(AddressingModes.ZeroPageX);
        handlers[0x6E] = Instructions.ROR(AddressingModes.Absolute);
        handlers[0x7E] = Instructions.ROR(AddressingModes.AbsoluteX);

        return new OpcodeTable<Cpu65C02, Cpu65C02State>(handlers);
    }

    /// <summary>
    /// Handles illegal/undefined opcodes by halting execution.
    /// </summary>
    private static void IllegalOpcode(Cpu65C02 cpu, IMemory memory, ref Cpu65C02State state)
    {
        state.HaltReason = HaltState.Stp; // Halt on illegal opcode (stop execution)
    }
}