// <copyright file="Cpu65C02OpcodeTableBuilder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using Core.Cpu;
using Core.Interfaces.Cpu;

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
    /// <returns>An <see cref="OpcodeTable"/> configured for the 65C02 CPU.</returns>
    public static OpcodeTable Build()
    {
        var handlers = new OpcodeHandler[256];

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
        handlers[0xB2] = Instructions.LDA(AddressingModes.ZeroPageIndirect); // 65C02

        // STA - Store Accumulator
        handlers[0x85] = Instructions.STA(AddressingModes.ZeroPage);
        handlers[0x95] = Instructions.STA(AddressingModes.ZeroPageX);
        handlers[0x8D] = Instructions.STA(AddressingModes.Absolute);
        handlers[0x9D] = Instructions.STA(AddressingModes.AbsoluteXWrite); // Write version always takes max cycles
        handlers[0x99] = Instructions.STA(AddressingModes.AbsoluteYWrite); // Write version always takes max cycles
        handlers[0x81] = Instructions.STA(AddressingModes.IndirectX);
        handlers[0x91] = Instructions.STA(AddressingModes.IndirectYWrite); // Write version always takes max cycles
        handlers[0x92] = Instructions.STA(AddressingModes.ZeroPageIndirect); // 65C02

        // LDX - Load X Register
        handlers[0xA2] = Instructions.LDX(AddressingModes.Immediate);
        handlers[0xA6] = Instructions.LDX(AddressingModes.ZeroPage);
        handlers[0xB6] = Instructions.LDX(AddressingModes.ZeroPageY);
        handlers[0xAE] = Instructions.LDX(AddressingModes.Absolute);
        handlers[0xBE] = Instructions.LDX(AddressingModes.AbsoluteY);

        // LDY - Load Y Register
        handlers[0xA0] = Instructions.LDY(AddressingModes.Immediate);
        handlers[0xA4] = Instructions.LDY(AddressingModes.ZeroPage);
        handlers[0xB4] = Instructions.LDY(AddressingModes.ZeroPageX);
        handlers[0xAC] = Instructions.LDY(AddressingModes.Absolute);
        handlers[0xBC] = Instructions.LDY(AddressingModes.AbsoluteX);

        // NOP - No Operation
        handlers[0xEA] = Instructions.NOP(AddressingModes.Implied);

        // WDC W65C02S "unused" opcode NOP slots (see "65C02 Apple II Emulator
        // Correctness Checklist.md" section 6 and the WDC W65C02S datasheet).
        // All of these preserve flags and registers. Software (notably ProDOS
        // 2.4.3's boot1 IIgs-detection idiom at $102C: `C2 02`) relies on these
        // being silent NOPs on a 65C02 (whereas on a 65816 they decode as
        // REP/SEP/PEI/MVN/JML/etc. and have observable side effects).
        // The disassembler renders these as `NOP <operand>` via the standard
        // Instructions.NOP + AddressingModes.* composition (reflection-based
        // analysis in OpcodeTableAnalyzer).

        // 2 bytes, 2 cycles: immediate-mode NOPs.
        handlers[0x02] = Instructions.NOP(AddressingModes.Immediate);
        handlers[0x22] = Instructions.NOP(AddressingModes.Immediate);
        handlers[0x42] = Instructions.NOP(AddressingModes.Immediate);
        handlers[0x62] = Instructions.NOP(AddressingModes.Immediate);
        handlers[0x82] = Instructions.NOP(AddressingModes.Immediate);
        handlers[0xC2] = Instructions.NOP(AddressingModes.Immediate);
        handlers[0xE2] = Instructions.NOP(AddressingModes.Immediate);

        // 2 bytes, 3 cycles: zero-page NOP.
        // (ZeroPage adds 1 cycle for the operand fetch; NOP adds 1; +1 from
        //  base opcode fetch = 3 cycles total.)
        handlers[0x44] = Instructions.NOP(AddressingModes.ZeroPage);

        // 2 bytes, 4 cycles: zero-page,X NOPs.
        // (ZeroPageX adds 2 cycles (fetch + indexing); NOP adds 1; +1 from
        //  base opcode fetch = 4 cycles total.)
        handlers[0x54] = Instructions.NOP(AddressingModes.ZeroPageX);
        handlers[0xD4] = Instructions.NOP(AddressingModes.ZeroPageX);
        handlers[0xF4] = Instructions.NOP(AddressingModes.ZeroPageX);

        // 3 bytes, 4 cycles: absolute NOPs.
        // (Absolute adds 2 cycles for the 16-bit operand fetch; NOP adds 1;
        //  +1 from base opcode fetch = 4 cycles total.)
        handlers[0xDC] = Instructions.NOP(AddressingModes.Absolute);
        handlers[0xFC] = Instructions.NOP(AddressingModes.Absolute);

        // 3 bytes, 8 cycles: "loud" absolute NOP. Per the WDC datasheet this is
        // the only unused-slot NOP that consumes 8 cycles (versus 4 for the
        // other 3-byte absolute NOPs). Modelled with extra instruction cycles
        // so the PC advances by 3 and the bus arbiter sees the correct cycle
        // pressure; the actual bus read at the formed address is not performed
        // since no shipping 65C02 software in scope for this emulator relies on
        // the side effect.
        handlers[0x5C] = Instructions.NOP(AddressingModes.Absolute, instructionCycles: 5);

        // 1 byte, 1 cycle: column-3 single-cycle NOPs. These are the slots
        // reserved by WDC for future expansion; on the 65C02 they fully decode
        // in a single cycle with no side effects (no operand fetch, no PC
        // advance beyond the opcode byte itself).
        handlers[0x03] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x13] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x23] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x33] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x43] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x53] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x63] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x73] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x83] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x93] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0xA3] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0xB3] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0xC3] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0xD3] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0xE3] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0xF3] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);

        // 1 byte, 1 cycle: column-B single-cycle NOPs. Additional reserved slots
        // per WDC datasheet; same behavior as column-3 (no operand, no side effects).
        handlers[0x0B] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x1B] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x2B] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x3B] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x4B] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x5B] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x6B] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x7B] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x8B] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0x9B] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0xAB] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0xBB] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0xEB] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);
        handlers[0xFB] = Instructions.NOP(AddressingModes.Implied, instructionCycles: 0);

        // STX - Store X Register
        handlers[0x86] = Instructions.STX(AddressingModes.ZeroPage);
        handlers[0x96] = Instructions.STX(AddressingModes.ZeroPageY);
        handlers[0x8E] = Instructions.STX(AddressingModes.Absolute);

        // STY - Store Y Register
        handlers[0x84] = Instructions.STY(AddressingModes.ZeroPage);
        handlers[0x94] = Instructions.STY(AddressingModes.ZeroPageX);
        handlers[0x8C] = Instructions.STY(AddressingModes.Absolute);

        // Flag manipulation instructions (all use Implied addressing)
        handlers[0x18] = Instructions.CLC(AddressingModes.Implied); // Clear Carry
        handlers[0x38] = Instructions.SEC(AddressingModes.Implied); // Set Carry
        handlers[0x58] = Instructions.CLI(AddressingModes.Implied); // Clear Interrupt Disable
        handlers[0x78] = Instructions.SEI(AddressingModes.Implied); // Set Interrupt Disable
        handlers[0xD8] = Instructions.CLD(AddressingModes.Implied); // Clear Decimal
        handlers[0xF8] = Instructions.SED(AddressingModes.Implied); // Set Decimal
        handlers[0xB8] = Instructions.CLV(AddressingModes.Implied); // Clear Overflow

        // Register Transfer Operations
        handlers[0xAA] = Instructions.TAX(AddressingModes.Implied); // Transfer RegisterAccumulator to X
        handlers[0xA8] = Instructions.TAY(AddressingModes.Implied); // Transfer RegisterAccumulator to Y
        handlers[0x8A] = Instructions.TXA(AddressingModes.Implied); // Transfer X to RegisterAccumulator
        handlers[0x98] = Instructions.TYA(AddressingModes.Implied); // Transfer Y to RegisterAccumulator
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

        // Rockwell/WDC Bit Manipulation Instructions (RMB, SMB, BBR, BBS)
        // Reset Memory Bit (RMB0-RMB7)
        handlers[0x07] = Instructions.RMB(AddressingModes.ZeroPage, 0);
        handlers[0x17] = Instructions.RMB(AddressingModes.ZeroPage, 1);
        handlers[0x27] = Instructions.RMB(AddressingModes.ZeroPage, 2);
        handlers[0x37] = Instructions.RMB(AddressingModes.ZeroPage, 3);
        handlers[0x47] = Instructions.RMB(AddressingModes.ZeroPage, 4);
        handlers[0x57] = Instructions.RMB(AddressingModes.ZeroPage, 5);
        handlers[0x67] = Instructions.RMB(AddressingModes.ZeroPage, 6);
        handlers[0x77] = Instructions.RMB(AddressingModes.ZeroPage, 7);

        // Set Memory Bit (SMB0-SMB7)
        handlers[0x87] = Instructions.SMB(AddressingModes.ZeroPage, 0);
        handlers[0x97] = Instructions.SMB(AddressingModes.ZeroPage, 1);
        handlers[0xA7] = Instructions.SMB(AddressingModes.ZeroPage, 2);
        handlers[0xB7] = Instructions.SMB(AddressingModes.ZeroPage, 3);
        handlers[0xC7] = Instructions.SMB(AddressingModes.ZeroPage, 4);
        handlers[0xD7] = Instructions.SMB(AddressingModes.ZeroPage, 5);
        handlers[0xE7] = Instructions.SMB(AddressingModes.ZeroPage, 6);
        handlers[0xF7] = Instructions.SMB(AddressingModes.ZeroPage, 7);

        // Branch if Bit Reset (BBR0-BBR7)
        handlers[0x0F] = Instructions.BBR(AddressingModes.ZeroPageRelative, 0);
        handlers[0x1F] = Instructions.BBR(AddressingModes.ZeroPageRelative, 1);
        handlers[0x2F] = Instructions.BBR(AddressingModes.ZeroPageRelative, 2);
        handlers[0x3F] = Instructions.BBR(AddressingModes.ZeroPageRelative, 3);
        handlers[0x4F] = Instructions.BBR(AddressingModes.ZeroPageRelative, 4);
        handlers[0x5F] = Instructions.BBR(AddressingModes.ZeroPageRelative, 5);
        handlers[0x6F] = Instructions.BBR(AddressingModes.ZeroPageRelative, 6);
        handlers[0x7F] = Instructions.BBR(AddressingModes.ZeroPageRelative, 7);

        // Branch if Bit Set (BBS0-BBS7)
        handlers[0x8F] = Instructions.BBS(AddressingModes.ZeroPageRelative, 0);
        handlers[0x9F] = Instructions.BBS(AddressingModes.ZeroPageRelative, 1);
        handlers[0xAF] = Instructions.BBS(AddressingModes.ZeroPageRelative, 2);
        handlers[0xBF] = Instructions.BBS(AddressingModes.ZeroPageRelative, 3);
        handlers[0xCF] = Instructions.BBS(AddressingModes.ZeroPageRelative, 4);
        handlers[0xDF] = Instructions.BBS(AddressingModes.ZeroPageRelative, 5);
        handlers[0xEF] = Instructions.BBS(AddressingModes.ZeroPageRelative, 6);
        handlers[0xFF] = Instructions.BBS(AddressingModes.ZeroPageRelative, 7);

        // Jump and Subroutine Operations
        handlers[0x4C] = Instructions.JMP(AddressingModes.Absolute); // Jump Absolute
        handlers[0x6C] = Instructions.JMP(AddressingModes.Indirect); // Jump Indirect
        handlers[0x7C] = Instructions.JMP(AddressingModes.AbsoluteIndirectX); // Jump Indirect X (65C02)
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
        handlers[0xD2] = Instructions.CMP(AddressingModes.ZeroPageIndirect); // 65C02

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
        handlers[0x72] = Instructions.ADC(AddressingModes.ZeroPageIndirect); // 65C02

        handlers[0xE9] = Instructions.SBC(AddressingModes.Immediate);
        handlers[0xE5] = Instructions.SBC(AddressingModes.ZeroPage);
        handlers[0xF5] = Instructions.SBC(AddressingModes.ZeroPageX);
        handlers[0xED] = Instructions.SBC(AddressingModes.Absolute);
        handlers[0xFD] = Instructions.SBC(AddressingModes.AbsoluteX);
        handlers[0xF9] = Instructions.SBC(AddressingModes.AbsoluteY);
        handlers[0xE1] = Instructions.SBC(AddressingModes.IndirectX);
        handlers[0xF1] = Instructions.SBC(AddressingModes.IndirectY);
        handlers[0xF2] = Instructions.SBC(AddressingModes.ZeroPageIndirect); // 65C02

        handlers[0xE6] = Instructions.INC(AddressingModes.ZeroPage);
        handlers[0xF6] = Instructions.INC(AddressingModes.ZeroPageX);
        handlers[0xEE] = Instructions.INC(AddressingModes.Absolute);
        handlers[0xFE] = Instructions.INC(AddressingModes.AbsoluteX);
        handlers[0x1A] = Instructions.INA(AddressingModes.Accumulator); // 65C02 INC A

        handlers[0xC6] = Instructions.DEC(AddressingModes.ZeroPage);
        handlers[0xD6] = Instructions.DEC(AddressingModes.ZeroPageX);
        handlers[0xCE] = Instructions.DEC(AddressingModes.Absolute);
        handlers[0xDE] = Instructions.DEC(AddressingModes.AbsoluteX);
        handlers[0x3A] = Instructions.DEA(AddressingModes.Accumulator); // 65C02 DEC A

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
        handlers[0x32] = Instructions.AND(AddressingModes.ZeroPageIndirect); // 65C02

        handlers[0x09] = Instructions.ORA(AddressingModes.Immediate);
        handlers[0x05] = Instructions.ORA(AddressingModes.ZeroPage);
        handlers[0x15] = Instructions.ORA(AddressingModes.ZeroPageX);
        handlers[0x0D] = Instructions.ORA(AddressingModes.Absolute);
        handlers[0x1D] = Instructions.ORA(AddressingModes.AbsoluteX);
        handlers[0x19] = Instructions.ORA(AddressingModes.AbsoluteY);
        handlers[0x01] = Instructions.ORA(AddressingModes.IndirectX);
        handlers[0x11] = Instructions.ORA(AddressingModes.IndirectY);
        handlers[0x12] = Instructions.ORA(AddressingModes.ZeroPageIndirect); // 65C02

        handlers[0x49] = Instructions.EOR(AddressingModes.Immediate);
        handlers[0x45] = Instructions.EOR(AddressingModes.ZeroPage);
        handlers[0x55] = Instructions.EOR(AddressingModes.ZeroPageX);
        handlers[0x4D] = Instructions.EOR(AddressingModes.Absolute);
        handlers[0x5D] = Instructions.EOR(AddressingModes.AbsoluteX);
        handlers[0x59] = Instructions.EOR(AddressingModes.AbsoluteY);
        handlers[0x41] = Instructions.EOR(AddressingModes.IndirectX);
        handlers[0x51] = Instructions.EOR(AddressingModes.IndirectY);
        handlers[0x52] = Instructions.EOR(AddressingModes.ZeroPageIndirect); // 65C02

        handlers[0x24] = Instructions.BIT(AddressingModes.ZeroPage);
        handlers[0x2C] = Instructions.BIT(AddressingModes.Absolute);
        handlers[0x34] = Instructions.BIT(AddressingModes.ZeroPageX); // 65C02
        handlers[0x3C] = Instructions.BIT(AddressingModes.AbsoluteX); // 65C02
        handlers[0x89] = Instructions.BITImmediate(AddressingModes.Immediate); // 65C02

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

        return new(handlers);
    }

    /// <summary>
    /// Handles illegal/undefined opcodes by halting execution.
    /// </summary>
    private static void IllegalOpcode(ICpu cpu)
    {
        cpu.HaltReason = HaltState.Stp; // Halt on illegal opcode (stop execution)
    }
}