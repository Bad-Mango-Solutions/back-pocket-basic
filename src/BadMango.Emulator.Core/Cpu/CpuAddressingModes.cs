// <copyright file="CpuAddressingModes.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

/// <summary>Represents the various addressing modes supported by the 65C02 CPU.</summary>
/// <remarks>
/// Addressing modes define how the CPU determines the location of operands
/// for instructions. Each mode specifies a unique way of interpreting
/// instruction operands and calculating effective addresses.
/// </remarks>
public enum CpuAddressingModes : byte
{
    /// <summary>
    /// No addressing mode or unknown addressing mode.
    /// </summary>
    None = 0,

    /// <summary>
    /// Implied addressing mode, where the operand is implied by the instruction itself.
    /// </summary>
    Implied = 1,

    /// <summary>
    /// Immediate addressing mode, where the operand is provided immediately after the instruction.
    /// </summary>
    Immediate = 2,

    /// <summary>
    /// Zero-page addressing mode, where the operand is located within the defined zero-page area.
    /// </summary>
    ZeroPage = 3,

    /// <summary>
    /// Zero-page X addressing mode, where the operand is located within the defined zero-page area,
    /// offset by the X register.
    /// </summary>
    ZeroPageX = 4,

    /// <summary>
    /// Zero-page Y addressing mode, where the operand is located within the defined zero-page area,
    /// offset by the Y register.
    /// </summary>
    ZeroPageY = 5,

    /// <summary>
    /// Absolute addressing mode, where the operand is located at a specific 16-bit memory address.
    /// </summary>
    Absolute = 6,

    /// <summary>
    /// Absolute X addressing mode, where the operand is located at a specific 16-bit memory address,
    /// offset by the X register.
    /// </summary>
    AbsoluteX = 7,

    /// <summary>
    /// Absolute Y addressing mode, where the operand is located at a specific 16-bit memory address,
    /// offset by the Y register.
    /// </summary>
    AbsoluteY = 8,

    /// <summary>
    /// Indirect addressing mode, where the operand address is determined by reading a pointer from memory.
    /// </summary>
    Indirect = 9,

    /// <summary>
    /// Indirect X addressing mode, where the operand address is determined by adding the X register
    /// to a zero-page pointer.
    /// </summary>
    IndirectX = 10,

    /// <summary>
    /// Indirect Y addressing mode, where the operand address is determined by reading a zero-page pointer
    /// and adding the Y register.
    /// </summary>
    IndirectY = 11,

    /// <summary>
    /// Accumulator addressing mode, where the operand is the accumulator register.
    /// </summary>
    Accumulator = 12,

    /// <summary>
    /// Relative addressing mode, used for branch instructions, where the operand is a signed offset
    /// relative to the program counter.
    /// </summary>
    Relative = 13,
}