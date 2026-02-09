// <copyright file="ProcessorStatusFlagsHelpers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

using System.Runtime.CompilerServices;

/// <summary>
/// Helper methods for Processor Status Flags (P register) manipulation.
/// </summary>
/// <remarks>
/// Provides extension methods for getting and setting individual flags in the P register.
/// The P register contains the following flags (65xx family standard):
/// <list type="bullet">
/// <item><description>N (Negative) - Bit 7</description></item>
/// <item><description>V (Overflow) - Bit 6</description></item>
/// <item><description>M (Memory/Accumulator size) - Bit 5 (65816/65832)</description></item>
/// <item><description>X (Index register size) - Bit 4 (65816/65832)</description></item>
/// <item><description>D (Decimal mode) - Bit 3</description></item>
/// <item><description>I (Interrupt disable) - Bit 2</description></item>
/// <item><description>Z (Zero) - Bit 1</description></item>
/// <item><description>C (Carry) - Bit 0</description></item>
/// </list>
/// </remarks>
public static class ProcessorStatusFlagsHelpers
{
    // Flag bit masks
    private const byte CarryBit = 0x01;        // Bit 0
    private const byte ZeroBit = 0x02;         // Bit 1
    private const byte InterruptBit = 0x04;    // Bit 2
    private const byte DecimalBit = 0x08;      // Bit 3
    private const byte IndexSizeBit = 0x10;    // Bit 4 (X flag - 65816/65832)
    private const byte MemorySizeBit = 0x20;   // Bit 5 (M flag - 65816/65832)
    private const byte OverflowBit = 0x40;     // Bit 6
    private const byte NegativeBit = 0x80;     // Bit 7

    #region Carry Flag (C)

    /// <param name="p">The processor status flags.</param>
    extension(ProcessorStatusFlags p)
    {
        /// <summary>Gets whether the Carry flag is set.</summary>
        /// <returns>True if the Carry flag is set, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCarrySet()
            => ((byte)p & CarryBit) != 0;

        /// <summary>Sets the Carry flag.</summary>
        /// <param name="value">Whether to set the flag.</param>
        /// <returns>The updated processor status flags.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetCarry(bool value)
            => value ? (ProcessorStatusFlags)((byte)p | CarryBit) : (ProcessorStatusFlags)((byte)p & ~CarryBit);
    }

    #endregion

    #region Zero Flag (Z)

    /// <param name="p">The processor status flags.</param>
    extension(ref ProcessorStatusFlags p)
    {
        /// <summary>Gets whether the Zero flag is set.</summary>
        /// <returns>True if the Zero flag is set, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsZeroSet()
            => ((byte)p & ZeroBit) != 0;

        /// <summary>Sets the Zero flag.</summary>
        /// <param name="value">Whether to set the flag.</param>
        /// <returns>The updated processor status flags.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetZero(bool value)
            => value ? (ProcessorStatusFlags)((byte)p | ZeroBit) : (ProcessorStatusFlags)((byte)p & ~ZeroBit);
    }

    #endregion

    #region Interrupt Disable Flag (I)

    /// <param name="p">The processor status flags.</param>
    extension(ref ProcessorStatusFlags p)
    {
        /// <summary>Gets whether the Interrupt Disable flag is set.</summary>
        /// <returns>True if the Interrupt Disable flag is set, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInterruptDisabled()
            => ((byte)p & InterruptBit) != 0;

        /// <summary>Sets the Interrupt Disable flag.</summary>
        /// <param name="value">Whether to disable interrupts.</param>
        /// <returns>The updated processor status flags.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetInterruptDisable(bool value)
            => value ? (ProcessorStatusFlags)((byte)p | InterruptBit) : (ProcessorStatusFlags)((byte)p & ~InterruptBit);
    }

    #endregion

    #region Decimal Mode Flag (D)

    /// <param name="p">The processor status flags.</param>
    extension(ref ProcessorStatusFlags p)
    {
        /// <summary>Gets whether Decimal mode is enabled.</summary>
        /// <returns>True if Decimal mode is enabled, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDecimalModeEnabled()
            => ((byte)p & DecimalBit) != 0;

        /// <summary>Sets the Decimal mode flag.</summary>
        /// <param name="value">Whether to enable Decimal mode.</param>
        /// <returns>The updated processor status flags.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetDecimalMode(bool value)
            => value ? (ProcessorStatusFlags)((byte)p | DecimalBit) : (ProcessorStatusFlags)((byte)p & ~DecimalBit);
    }

    #endregion

    #region Index Register Size Flag (X) - 65816/65832 only

    /// <param name="p">The processor status flags.</param>
    extension(ref ProcessorStatusFlags p)
    {
        /// <summary>Gets whether the Index registers (X/Y) are in 8-bit mode.</summary>
        /// <returns>True if X/Y are 8-bit, false if 16-bit.</returns>
        /// <remarks>Only meaningful in 65816/65832 native modes. In 6502 mode, this bit has different meaning.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsIndexSize8Bit()
            => ((byte)p & IndexSizeBit) != 0;

        /// <summary>Sets the Index register size flag (X flag).</summary>
        /// <param name="is8Bit">True for 8-bit mode, false for 16-bit mode.</param>
        /// <returns>The updated processor status flags.</returns>
        /// <remarks>Only meaningful in 65816/65832 native modes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetIndexSize8Bit(bool is8Bit)
            => is8Bit ? (ProcessorStatusFlags)((byte)p | IndexSizeBit) : (ProcessorStatusFlags)((byte)p & ~IndexSizeBit);
    }

    #endregion

    #region Memory/Accumulator Size Flag (M) - 65816/65832 only

    /// <param name="p">The processor status flags.</param>
    extension(ref ProcessorStatusFlags p)
    {
        /// <summary>Gets whether the Accumulator is in 8-bit mode.</summary>
        /// <returns>True if A is 8-bit, false if 16-bit.</returns>
        /// <remarks>Only meaningful in 65816/65832 native modes. In 6502 mode, this bit has different meaning.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMemorySize8Bit()
            => ((byte)p & MemorySizeBit) != 0;

        /// <summary>Sets the Memory/Accumulator size flag (M flag).</summary>
        /// <param name="is8Bit">True for 8-bit mode, false for 16-bit mode.</param>
        /// <returns>The updated processor status flags.</returns>
        /// <remarks>Only meaningful in 65816/65832 native modes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetMemorySize8Bit(bool is8Bit)
            => is8Bit ? (ProcessorStatusFlags)((byte)p | MemorySizeBit) : (ProcessorStatusFlags)((byte)p & ~MemorySizeBit);
    }

    #endregion

    #region Overflow Flag (V)

    /// <param name="p">The processor status flags.</param>
    extension(ref ProcessorStatusFlags p)
    {
        /// <summary>Gets whether the Overflow flag is set.</summary>
        /// <returns>True if the Overflow flag is set, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOverflowSet()
            => ((byte)p & OverflowBit) != 0;

        /// <summary>Sets the Overflow flag.</summary>
        /// <param name="value">Whether to set the flag.</param>
        /// <returns>The updated processor status flags.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetOverflow(bool value)
            => value ? (ProcessorStatusFlags)((byte)p | OverflowBit) : (ProcessorStatusFlags)((byte)p & ~OverflowBit);
    }

    #endregion

    #region Negative Flag (N)

    /// <param name="p">The processor status flags.</param>
    extension(ref ProcessorStatusFlags p)
    {
        /// <summary>Gets whether the Negative flag is set.</summary>
        /// <returns>True if the Negative flag is set, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNegativeSet()
            => ((byte)p & NegativeBit) != 0;

        /// <summary>Sets the Negative flag.</summary>
        /// <param name="value">Whether to set the flag.</param>
        /// <returns>The updated processor status flags.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetNegative(bool value)
            => value ? (ProcessorStatusFlags)((byte)p | NegativeBit) : (ProcessorStatusFlags)((byte)p & ~NegativeBit);
    }

    #endregion

    #region Combined Operations

    /// <param name="p">The processor status flags.</param>
    extension(ref ProcessorStatusFlags p)
    {
        /// <summary>Sets the Zero and Negative flags based on a byte value.</summary>
        /// <param name="value">The value to test.</param>
        /// <returns>The updated processor status flags with Z and N set appropriately.</returns>
        /// <remarks>
        /// Uses branchless bitmask operations: clears both Z and N, then sets them
        /// in a single pass to avoid multiple conditional branches in the hot path.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetZeroAndNegative(byte value)
        {
            // Clear Z and N, then set them branchlessly:
            // - Z bit (0x02): set when value == 0.
            // - N bit (0x80): copy bit 7 directly from value.
            byte flags = (byte)((byte)p & ~(ZeroBit | NegativeBit));
            flags |= (byte)((((uint)value - 1) >> 31) << 1); // Z flag: underflow sets bit 31
            flags |= (byte)(value & NegativeBit);             // N flag
            p = (ProcessorStatusFlags)flags;
            return p;
        }

        /// <summary>Sets the Zero and Negative flags based on a 16-bit value.</summary>
        /// <param name="value">The value to test.</param>
        /// <returns>The updated processor status flags with Z and N set appropriately.</returns>
        /// <remarks>
        /// Uses branchless bitmask operations for the 16-bit case.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetZeroAndNegative(Word value)
        {
            byte flags = (byte)((byte)p & ~(ZeroBit | NegativeBit));
            flags |= (byte)((((uint)value - 1) >> 31) << 1); // Z flag
            flags |= (byte)((value >> 8) & NegativeBit);      // N flag
            p = (ProcessorStatusFlags)flags;
            return p;
        }

        /// <summary>Sets the Zero and Negative flags based on a 32-bit value.</summary>
        /// <param name="value">The value to test.</param>
        /// <returns>The updated processor status flags with Z and N set appropriately.</returns>
        /// <remarks>
        /// Uses branchless bitmask operations for the 32-bit case.
        /// Only meaningful in 65832 native-32 mode.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetZeroAndNegative(uint value)
        {
            byte flags = (byte)((byte)p & ~(ZeroBit | NegativeBit));
            flags |= (byte)((((ulong)value - 1) >> 63) << 1); // Z flag
            flags |= (byte)((value >> 24) & NegativeBit);      // N flag
            p = (ProcessorStatusFlags)flags;
            return p;
        }

        /// <summary>Sets the Zero and Negative flags based on a value and its bit width.</summary>
        /// <param name="value">The value to test.</param>
        /// <param name="sizeInBits">The size of the value in bits (8, 16, or 32).</param>
        /// <returns>The updated processor status flags with Z and N set appropriately.</returns>
        /// <remarks>
        /// This method enables size-aware flag setting that adapts to different register widths:
        /// - 8-bit: Tests bit 7 for negative
        /// - 16-bit: Tests bit 15 for negative
        /// - 32-bit: Tests bit 31 for negative.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProcessorStatusFlags SetZeroAndNegative(DWord value, byte sizeInBits)
        {
            return sizeInBits switch
            {
                8 => p.SetZeroAndNegative((byte)(value & 0xFF)),
                16 => p.SetZeroAndNegative((Word)(value & 0xFFFF)),
                32 => p.SetZeroAndNegative(value),
                _ => throw new ArgumentException($"Invalid size: {sizeInBits}. Must be 8, 16, or 32.", nameof(sizeInBits)),
            };
        }
    }

    #endregion
}