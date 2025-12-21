// <copyright file="Cpu6502Registers.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Basic.Emulation;

/// <summary>
/// Represents the 6502/65816 CPU registers.
/// </summary>
public class Cpu6502Registers
{
    // Status flag bit positions
    private const byte CarryFlag = 0x01;
    private const byte ZeroFlag = 0x02;
    private const byte InterruptDisable = 0x04;
    private const byte DecimalMode = 0x08;
    private const byte BreakCommand = 0x10;
    private const byte OverflowFlag = 0x40;
    private const byte NegativeFlag = 0x80;

    /// <summary>Accumulator.</summary>
    public byte A { get; set; }

    /// <summary>X Index Register.</summary>
    public byte X { get; set; }

    /// <summary>Y Index Register.</summary>
    public byte Y { get; set; }

    /// <summary>Stack Pointer.</summary>
    public byte SP { get; set; } = 0xFF;

    /// <summary>Program Counter.</summary>
    public ushort PC { get; set; }

    /// <summary>Processor Status Register.</summary>
    public byte P { get; set; } = 0x20; // Bit 5 always set

    /// <summary>
    /// Gets or sets the state of the carry flag in the CPU registers.
    /// </summary>
    /// <value>
    /// <c>true</c> if the carry flag is set; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// The carry flag is a status flag used in arithmetic operations to indicate an overflow or borrow.
    /// </remarks>
    public bool Carry
    {
        get => (P & CarryFlag) != 0;
        set => P = value ? (byte)(P | CarryFlag) : (byte)(P & ~CarryFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the zero flag is set in the processor status register.
    /// </summary>
    /// <value>
    /// <c>true</c> if the zero flag is set; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// The zero flag indicates whether the result of the last operation was zero.
    /// Setting this property updates the processor status register accordingly.
    /// </remarks>
    public bool Zero
    {
        get => (P & ZeroFlag) != 0;
        set => P = value ? (byte)(P | ZeroFlag) : (byte)(P & ~ZeroFlag);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the interrupt disable flag is set.
    /// </summary>
    /// <remarks>
    /// When this property is <see langword="true"/>, interrupts are disabled.
    /// When <see langword="false"/>, interrupts are enabled.
    /// </remarks>
    public bool InterruptDisabled
    {
        get => (P & InterruptDisable) != 0;
        set => P = value ? (byte)(P | InterruptDisable) : (byte)(P & ~InterruptDisable);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the CPU is operating in decimal mode.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, the decimal mode flag is enabled, affecting arithmetic operations.
    /// When set to <c>false</c>, the decimal mode flag is disabled.
    /// </remarks>
    public bool Decimal
    {
        get => (P & DecimalMode) != 0;
        set => P = value ? (byte)(P | DecimalMode) : (byte)(P & ~DecimalMode);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Break flag is set in the processor status register (P).
    /// This flag is used to indicate a software interrupt (BRK instruction) in the 6502/65816 CPU.
    /// </summary>
    public bool Break
    {
        get => (P & BreakCommand) != 0;
        set => P = value ? (byte)(P | BreakCommand) : (byte)(P & ~BreakCommand);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the overflow flag is set in the CPU status register.
    /// </summary>
    /// <remarks>
    /// The overflow flag is used to indicate that an arithmetic operation has produced a result
    /// that exceeds the range of the destination operand.
    /// </remarks>
    public bool Overflow
    {
        get => (P & OverflowFlag) != 0;
        set => P = value ? (byte)(P | OverflowFlag) : (byte)(P & ~OverflowFlag);
    }

    /// <summary>
    /// Gets or sets the state of the Negative flag in the CPU status register.
    /// </summary>
    /// <value>
    /// <c>true</c> if the Negative flag is set; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// The Negative flag is determined by the most significant bit of the result of the last operation.
    /// Setting this property updates the CPU status register accordingly.
    /// </remarks>
    public bool Negative
    {
        get => (P & NegativeFlag) != 0;
        set => P = value ? (byte)(P | NegativeFlag) : (byte)(P & ~NegativeFlag);
    }

    /// <summary>
    /// Resets the CPU registers to their default state.
    /// </summary>
    /// <remarks>
    /// This method initializes the CPU registers to their default values:
    /// <list type="bullet">
    /// <item><description>Accumulator (A), Index registers (X, Y) are set to 0.</description></item>
    /// <item><description>Stack Pointer (SP) is set to <c>0xFF</c>.</description></item>
    /// <item><description>Program Counter (PC) is set to 0.</description></item>
    /// <item><description>Processor Status (P) is set to <c>0x20</c>, with bit 5 always set.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to reset the CPU registers:
    /// <code>
    /// var registers = new Cpu6502Registers();
    /// registers.Reset();
    /// </code>
    /// </example>
    public virtual void Reset()
    {
        A = 0;
        X = 0;
        Y = 0;
        SP = 0xFF;
        PC = 0;
        P = 0x20;
    }

    /// <summary>
    /// Sets the Negative (N) and Zero (Z) flags based on the specified value.
    /// </summary>
    /// <param name="value">The byte value used to determine the state of the flags.</param>
    /// <remarks>
    /// The Zero (Z) flag is set to <c>true</c> if the value is zero; otherwise, it is set to <c>false</c>.
    /// The Negative (N) flag is set to <c>true</c> if the most significant bit (MSB) of the value is 1; otherwise, it is set to <c>false</c>.
    /// </remarks>
    public void SetNZ(byte value)
    {
        Zero = value == 0;
        Negative = (value & 0x80) != 0;
    }
}