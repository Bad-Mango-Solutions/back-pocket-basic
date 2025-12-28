// <copyright file="ProcessorStatusFlags.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

/// <summary>
/// Represents the processor status flags used in the emulator.
/// </summary>
/// <remarks>
/// This enumeration defines the individual flags within the processor status register.
/// Each flag corresponds to a specific bit in the register and has a distinct purpose.
/// </remarks>
[Flags]
public enum ProcessorStatusFlags : byte
{
    /// <summary>
    /// No 0 (Negative) flag. Set if the result of an operation is negative.
    /// </summary>
    N = 1 << 7,

    /// <summary>
    /// Overflow flag. Set if an arithmetic operation results in an overflow.
    /// </summary>
    V = 1 << 6,

#pragma warning disable CA1069 // Enums values should not be duplicated
    /// <summary>
    /// Reserved flag. Always set to 1.
    /// </summary>
    R = 1 << 5,

    /// <summary>
    /// Accumulator width flag. Set if the accumulator is in 8-bit mode.
    /// </summary>
    /// <remarks>
    /// This flag is used in processors that support both 8-bit and 16-bit accumulator modes.
    /// When CP is cleared (indicating native mode), this flag is ignored.
    /// </remarks>
    M = 1 << 5,
#pragma warning restore CA1069 // Enums values should not be duplicated

#pragma warning disable CA1069 // Enums values should not be duplicated
    /// <summary>
    /// Break flag. Set if the interrupt was a non-maskable interrupt (NMI) or a software interrupt (BRK).
    /// </summary>
    /// <remarks>
    /// This flag indicates the source of an interrupt and is used during interrupt handling.
    /// It's only relevant when both CP and E processor flags are set (indicating 65C02 emulation mode).
    /// </remarks>
    B = 1 << 4,

    /// <summary>
    /// Index width flag. Set if the index registers are in 8-bit mode.
    /// </summary>
    /// <remarks>
    /// When CP is cleared (indicating native mode), this flag is ignored.
    /// </remarks>
    X = 1 << 4,
#pragma warning restore CA1069 // Enums values should not be duplicated

    /// <summary>
    /// Decimal mode flag. Set if the processor is in decimal (BCD) mode.
    /// </summary>
    D = 1 << 3,

    /// <summary>
    /// Interrupt disable flag. Set if interrupts are disabled.
    /// </summary>
    I = 1 << 2,

    /// <summary>
    /// Zero flag. Set if the result of an operation is zero.
    /// </summary>
    Z = 1 << 1,

    /// <summary>
    /// Carry flag. Set if an arithmetic operation results in a carry.
    /// </summary>
    C = 1 << 0,

    /// <summary>
    /// System Reset state (M, X, and I flags set).
    /// </summary>
    Reset = M | X | I,
}