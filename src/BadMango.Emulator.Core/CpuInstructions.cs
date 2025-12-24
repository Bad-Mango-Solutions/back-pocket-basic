// <copyright file="CpuInstructions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Core;

/// <summary>
/// Represents the instruction mnemonics for the 65C02 CPU and variants.
/// </summary>
/// <remarks>
/// This enumeration provides a complete list of instruction mnemonics
/// used for debugging and disassembly purposes.
/// </remarks>
public enum CpuInstructions : byte
{
    /// <summary>No instruction or unknown instruction.</summary>
    None = 0,

    // Load/Store instructions

    /// <summary>LDA - Load Accumulator.</summary>
    LDA,

    /// <summary>LDX - Load X Register.</summary>
    LDX,

    /// <summary>LDY - Load Y Register.</summary>
    LDY,

    /// <summary>STA - Store Accumulator.</summary>
    STA,

    /// <summary>STX - Store X Register.</summary>
    STX,

    /// <summary>STY - Store Y Register.</summary>
    STY,

    /// <summary>NOP - No Operation.</summary>
    NOP,

    /// <summary>BRK - Force Break.</summary>
    BRK,

    // Flag manipulation instructions

    /// <summary>CLC - Clear Carry Flag.</summary>
    CLC,

    /// <summary>SEC - Set Carry Flag.</summary>
    SEC,

    /// <summary>CLI - Clear Interrupt Disable Flag.</summary>
    CLI,

    /// <summary>SEI - Set Interrupt Disable Flag.</summary>
    SEI,

    /// <summary>CLD - Clear Decimal Mode Flag.</summary>
    CLD,

    /// <summary>SED - Set Decimal Mode Flag.</summary>
    SED,

    /// <summary>CLV - Clear Overflow Flag.</summary>
    CLV,

    // Register transfer instructions

    /// <summary>TAX - Transfer Accumulator to X.</summary>
    TAX,

    /// <summary>TAY - Transfer Accumulator to Y.</summary>
    TAY,

    /// <summary>TXA - Transfer X to Accumulator.</summary>
    TXA,

    /// <summary>TYA - Transfer Y to Accumulator.</summary>
    TYA,

    /// <summary>TXS - Transfer X to Stack Pointer.</summary>
    TXS,

    /// <summary>TSX - Transfer Stack Pointer to X.</summary>
    TSX,

    // Stack operations

    /// <summary>PHA - Push Accumulator.</summary>
    PHA,

    /// <summary>PHP - Push Processor Status.</summary>
    PHP,

    /// <summary>PLA - Pull Accumulator.</summary>
    PLA,

    /// <summary>PLP - Pull Processor Status.</summary>
    PLP,

    /// <summary>PHX - Push X Register (65C02).</summary>
    PHX,

    /// <summary>PLX - Pull X Register (65C02).</summary>
    PLX,

    /// <summary>PHY - Push Y Register (65C02).</summary>
    PHY,

    /// <summary>PLY - Pull Y Register (65C02).</summary>
    PLY,

    // Jump and subroutine instructions

    /// <summary>JMP - Jump.</summary>
    JMP,

    /// <summary>JSR - Jump to Subroutine.</summary>
    JSR,

    /// <summary>RTS - Return from Subroutine.</summary>
    RTS,

    /// <summary>RTI - Return from Interrupt.</summary>
    RTI,

    // Branch instructions

    /// <summary>BCC - Branch if Carry Clear.</summary>
    BCC,

    /// <summary>BCS - Branch if Carry Set.</summary>
    BCS,

    /// <summary>BEQ - Branch if Equal (Zero Set).</summary>
    BEQ,

    /// <summary>BNE - Branch if Not Equal (Zero Clear).</summary>
    BNE,

    /// <summary>BMI - Branch if Minus (Negative Set).</summary>
    BMI,

    /// <summary>BPL - Branch if Plus (Negative Clear).</summary>
    BPL,

    /// <summary>BVC - Branch if Overflow Clear.</summary>
    BVC,

    /// <summary>BVS - Branch if Overflow Set.</summary>
    BVS,

    /// <summary>BRA - Branch Always (65C02).</summary>
    BRA,

    // Arithmetic instructions

    /// <summary>ADC - Add with Carry.</summary>
    ADC,

    /// <summary>SBC - Subtract with Carry.</summary>
    SBC,

    /// <summary>INC - Increment Memory.</summary>
    INC,

    /// <summary>DEC - Decrement Memory.</summary>
    DEC,

    /// <summary>INX - Increment X Register.</summary>
    INX,

    /// <summary>INY - Increment Y Register.</summary>
    INY,

    /// <summary>DEX - Decrement X Register.</summary>
    DEX,

    /// <summary>DEY - Decrement Y Register.</summary>
    DEY,

    // Logical operations

    /// <summary>AND - Logical AND.</summary>
    AND,

    /// <summary>ORA - Logical OR.</summary>
    ORA,

    /// <summary>EOR - Exclusive OR.</summary>
    EOR,

    /// <summary>BIT - Bit Test.</summary>
    BIT,

    // Shift and rotate instructions

    /// <summary>ASL - Arithmetic Shift Left.</summary>
    ASL,

    /// <summary>LSR - Logical Shift Right.</summary>
    LSR,

    /// <summary>ROL - Rotate Left.</summary>
    ROL,

    /// <summary>ROR - Rotate Right.</summary>
    ROR,

    // Comparison instructions

    /// <summary>CMP - Compare Accumulator.</summary>
    CMP,

    /// <summary>CPX - Compare X Register.</summary>
    CPX,

    /// <summary>CPY - Compare Y Register.</summary>
    CPY,

    // 65C02-specific instructions

    /// <summary>STZ - Store Zero (65C02).</summary>
    STZ,

    /// <summary>TSB - Test and Set Bits (65C02).</summary>
    TSB,

    /// <summary>TRB - Test and Reset Bits (65C02).</summary>
    TRB,

    /// <summary>WAI - Wait for Interrupt (65C02).</summary>
    WAI,

    /// <summary>STP - Stop Processor (65C02).</summary>
    STP,
}