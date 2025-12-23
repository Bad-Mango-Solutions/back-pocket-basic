// <copyright file="RegisterHelpers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.CompilerServices;

/// <summary>
/// Helper methods for register manipulation and size view access.
/// </summary>
public static class RegisterHelpers
{
    #region Accumulator Register (A)

    /// <param name="a">The accumulator register.</param>
    extension(ref RegisterAccumulator a)
    {
        /// <summary>Gets the 8-bit value of the accumulator.</summary>
        /// <returns>The 8-bit value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte() => (byte)(a.acc & 0xFF);

        /// <summary>Sets the 8-bit value of the accumulator.</summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The updated accumulator register.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterAccumulator SetByte(byte value)
        {
            a.acc = value;
            return a;
        }

        /// <summary>Gets the 16-bit value of the accumulator.</summary>
        /// <returns>The 16-bit value.</returns>
        /// <remarks>Available in 65816 native mode.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Word GetWord() => (Word)(a.acc & 0xFFFF);

        /// <summary>Sets the 16-bit value of the accumulator.</summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The updated accumulator register.</returns>
        /// <remarks>Available in 65816 native mode.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterAccumulator SetWord(Word value)
        {
            a.acc = value;
            return a;
        }

        /// <summary>Gets the 32-bit value of the accumulator.</summary>
        /// <returns>The 32-bit value.</returns>
        /// <remarks>Only available in 65832 native mode.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DWord GetDWord() => a.acc;

        /// <summary>Sets the 32-bit value of the accumulator.</summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The updated accumulator register.</returns>
        /// <remarks>Only available in 65832 native mode.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterAccumulator SetDWord(DWord value)
        {
            a.acc = value;
            return a;
        }

        /// <summary>Gets the full address value of the accumulator.</summary>
        /// <returns>The full 32-bit address value.</returns>
        /// <remarks>Used for address arithmetic in some 65832 addressing modes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Addr GetAddr() => a.acc;

        /// <summary>Gets the accumulator value based on the specified size.</summary>
        /// <param name="sizeInBits">The size to read (8, 16, or 32 bits).</param>
        /// <returns>The accumulator value, masked to the specified size.</returns>
        /// <remarks>
        /// This method enables size-aware register access that adapts to different modes:
        /// - 8-bit: Returns low byte only
        /// - 16-bit: Returns low word only
        /// - 32-bit: Returns full double-word.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DWord GetValue(byte sizeInBits)
        {
            return sizeInBits switch
            {
                8 => (byte)(a.acc & 0xFF),
                16 => (Word)(a.acc & 0xFFFF),
                32 => a.acc,
                _ => throw new ArgumentException($"Invalid size: {sizeInBits}. Must be 8, 16, or 32.", nameof(sizeInBits)),
            };
        }

        /// <summary>Sets the accumulator value based on the specified size.</summary>
        /// <param name="value">The value to set.</param>
        /// <param name="sizeInBits">The size to write (8, 16, or 32 bits).</param>
        /// <returns>The updated accumulator register.</returns>
        /// <remarks>
        /// This method enables size-aware register writes that adapt to different modes:
        /// - 8-bit: Sets low byte, preserves upper bits
        /// - 16-bit: Sets low word, preserves upper word
        /// - 32-bit: Sets full double-word.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterAccumulator SetValue(DWord value, byte sizeInBits)
        {
            switch (sizeInBits)
            {
                case 8:
                    a.acc = (a.acc & 0xFFFFFF00) | (value & 0xFF);
                    break;
                case 16:
                    a.acc = (a.acc & 0xFFFF0000) | (value & 0xFFFF);
                    break;
                case 32:
                    a.acc = value;
                    break;
                default:
                    throw new ArgumentException($"Invalid size: {sizeInBits}. Must be 8, 16, or 32.", nameof(sizeInBits));
            }

            return a;
        }

        /// <summary>Clears the accumulator to zero.</summary>
        /// <returns>The cleared accumulator register.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterAccumulator Clear()
        {
            a.acc = 0;
            return a;
        }
    }

    #endregion

    #region Index Registers (X, Y)

    /// <param name="r">The index register.</param>
    extension(ref RegisterIndex r)
    {
        /// <summary>Gets the 8-bit value of the index register.</summary>
        /// <returns>The 8-bit value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte() => (byte)(r.index & 0xFF);

        /// <summary>Sets the 8-bit value of the index register.</summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The updated index register.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterIndex SetByte(byte value)
        {
            r.index = value;
            return r;
        }

        /// <summary>Gets the 16-bit value of the index register.</summary>
        /// <returns>The 16-bit value.</returns>
        /// <remarks>Available in 65816 native mode.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Word GetWord() => (Word)(r.index & 0xFFFF);

        /// <summary>Sets the 16-bit value of the index register.</summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The updated index register.</returns>
        /// <remarks>Available in 65816 native mode.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterIndex SetWord(Word value)
        {
            r.index = value;
            return r;
        }

        /// <summary>Gets the 32-bit value of the index register.</summary>
        /// <returns>The 32-bit value.</returns>
        /// <remarks>Only available in 65832 native mode.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DWord GetDWord() => r.index;

        /// <summary>Sets the 32-bit value of the index register.</summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The updated index register.</returns>
        /// <remarks>Only available in 65832 native mode.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterIndex SetDWord(DWord value)
        {
            r.index = value;
            return r;
        }

        /// <summary>Gets the full address value of the index register.</summary>
        /// <returns>The full 32-bit address value.</returns>
        /// <remarks>Used for address arithmetic in addressing modes.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Addr GetAddr() => r.index;

        /// <summary>Gets the index register value based on the specified size.</summary>
        /// <param name="sizeInBits">The size to read (8, 16, or 32 bits).</param>
        /// <returns>The index register value, masked to the specified size.</returns>
        /// <remarks>
        /// This method enables size-aware register access that adapts to different modes:
        /// - 8-bit: Returns low byte only
        /// - 16-bit: Returns low word only
        /// - 32-bit: Returns full double-word.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DWord GetValue(byte sizeInBits)
        {
            return sizeInBits switch
            {
                8 => (byte)(r.index & 0xFF),
                16 => (Word)(r.index & 0xFFFF),
                32 => r.index,
                _ => throw new ArgumentException($"Invalid size: {sizeInBits}. Must be 8, 16, or 32.", nameof(sizeInBits)),
            };
        }

        /// <summary>Sets the index register value based on the specified size.</summary>
        /// <param name="value">The value to set.</param>
        /// <param name="sizeInBits">The size to write (8, 16, or 32 bits).</param>
        /// <returns>The updated index register.</returns>
        /// <remarks>
        /// This method enables size-aware register writes that adapt to different modes:
        /// - 8-bit: Sets low byte, preserves upper bits
        /// - 16-bit: Sets low word, preserves upper word
        /// - 32-bit: Sets full double-word.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterIndex SetValue(DWord value, byte sizeInBits)
        {
            switch (sizeInBits)
            {
                case 8:
                    r.index = (r.index & 0xFFFFFF00) | (value & 0xFF);
                    break;
                case 16:
                    r.index = (r.index & 0xFFFF0000) | (value & 0xFFFF);
                    break;
                case 32:
                    r.index = value;
                    break;
                default:
                    throw new ArgumentException($"Invalid size: {sizeInBits}. Must be 8, 16, or 32.", nameof(sizeInBits));
            }

            return r;
        }

        /// <summary>Clears the index register to zero.</summary>
        /// <returns>The cleared index register.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterIndex Clear()
        {
            r.index = 0;
            return r;
        }
    }

    #endregion

    #region Stack Pointer Register (SP)

    /// <param name="state">The CPU state.</param>
    extension(ref CpuState state)
    {
        /// <summary>Pushes a byte onto the stack and decrements the stack pointer.</summary>
        /// <returns>The previous value of the stack pointer before it was decremented.</returns>
        /// <param name="stackBase">The base address of the stack, if specified.</param>
        /// <remarks>
        /// This method modifies the stack pointer register (`SP`) by decrementing it,
        /// effectively pushing a byte onto the stack. The returned value represents
        /// the stack pointer's state prior to the modification, so that it can be used
        /// to compute the address where the byte should be stored in memory.
        /// </remarks>
        public Addr PushByte(Addr stackBase = 0)
        {
            var old = state.Registers.SP.stack;
            state.Registers.SP.stack--;
            return stackBase + old;
        }

        /// <summary>Pops a byte from the stack and increments the stack pointer.</summary>
        /// <returns>The previous value of the stack pointer before it was incremented.</returns>
        /// <param name="stackBase">The base address of the stack, if specified.</param>
        /// <remarks>
        /// This method modifies the stack pointer register (`SP`) by incrementing it,
        /// effectively popping a byte from the stack. The returned value represents
        /// the stack pointer's state prior to the modification, so that it can be used
        /// to compute the address from which the byte should be retrieved in memory.
        /// </remarks>
        public Addr PopByte(Addr stackBase = 0)
        {
            var old = state.Registers.SP.stack + 1;
            state.Registers.SP.stack++;
            return stackBase + old;
        }
    }

    /// <param name="sp">The stack pointer register.</param>
    extension(ref RegisterStackPointer sp)
    {

        /// <summary>
        /// Pushes a single byte onto the stack by decrementing the stack pointer.
        /// </summary>
        /// <param name="state">The current CPU state, which includes registers and execution context.</param>
        /// <returns>The address of the stack pointer before the push operation.</returns>
        /// <remarks>
        /// This method updates the stack pointer register to reflect the push operation.
        /// It is designed for efficient execution using aggressive inlining.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Addr PushByte(ref CpuState state)
        {
            var old = sp.stack;
            sp.stack++;
            return old;
        }

        /// <summary>Pops (increments) the stack pointer.</summary>
        /// <param name="count">Number of bytes to pop (default 1).</param>
        /// <returns>The updated stack pointer register.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterStackPointer Pop(uint count = 1)
        {
            sp.stack += count;
            return sp;
        }
    }

    #endregion

    #region Direct Page Register (D)

    /// <param name="dp">The direct page register.</param>
    extension(ref RegisterDirectPage dp)
    {
        /// <summary>Calculates an effective address using direct page addressing.</summary>
        /// <param name="offset">The offset to add to the direct page base.</param>
        /// <returns>The effective address.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DWord CalculateAddress(Word offset) => dp.direct + offset;

        /// <summary>Checks if the direct page is aligned to page boundary (low byte is zero).</summary>
        /// <returns>True if aligned, false otherwise.</returns>
        /// <remarks>Aligned direct pages are faster on real 65816 hardware.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPageAligned() => (dp.direct & 0xFF) == 0;

        /// <summary>
        /// Clears the direct page register by setting its value to zero.
        /// </summary>
        /// <returns>The updated <see cref="RegisterDirectPage"/> instance with the cleared value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterDirectPage Clear()
        {
            dp.direct = 0;
            return dp;
        }
    }

    #endregion

    #region Program Counter Register (PC)

    /// <param name="pc">The program counter register.</param>
    extension(ref RegisterProgramCounter pc)
    {
        /// <summary>Gets the 32-bit value of the program counter.</summary>
        /// <returns>The 32-bit value.</returns>
        /// <remarks>Only available in 65832 native mode.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DWord GetDWord() => pc.addr;

        /// <summary>Sets the 32-bit value of the program counter.</summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The updated program counter register.</returns>
        /// <remarks>Only available in 65832 native mode.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterProgramCounter SetDWord(DWord value)
        {
            pc.addr = value;
            return pc;
        }

        /// <summary>
        /// Resets the program counter register to its default state.
        /// </summary>
        /// <returns>The updated program counter register with its value set to zero.</returns>
        /// <remarks>
        /// This method is intended for use in scenarios where the program counter needs to be reinitialized.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RegisterProgramCounter Reset()
        {
            pc.addr = 0;
            return pc;
        }
    }

    #endregion

    #region Registers Helpers

    /// <param name="registers">The CPU registers.</param>
    extension(ref Registers registers)
    {
        /// <summary>Gets the current architectural mode based on CP and E flags.</summary>
        /// <returns>The architectural mode.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchitecturalMode GetArchitecturalMode()
        {
            return registers switch
            {
                { CP: true, E: true } => ArchitecturalMode.Mode65C02,
                { CP: true, E: false } => ArchitecturalMode.Mode65816,
                _ => ArchitecturalMode.Mode65832,
            };
        }

        /// <summary>Checks if the CPU is in 6502 compatibility mode.</summary>
        /// <returns>True if in 6502 mode, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is65C02Mode() => registers is { CP: true, E: true };

        /// <summary>Checks if the CPU is in 65816 mode.</summary>
        /// <returns>True if in 65816 mode, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is65816Mode() => registers is { CP: true, E: false };

        /// <summary>Checks if the CPU is in 65832 native mode.</summary>
        /// <returns>True if in 65832 mode, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is65832Mode() => registers is { CP: false };

        /// <summary>Gets the effective accumulator size based on mode and M flag.</summary>
        /// <returns>The accumulator size in bits (8, 16, or 32).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetAccumulatorSize()
        {
            if (registers.Is65C02Mode())
            {
                return 8;
            }

            if (registers.Is65816Mode())
            {
                return registers.P.IsMemorySize8Bit() ? (byte)8 : (byte)16;
            }

            return 32; // 65832 mode
        }

        /// <summary>Gets the effective index register size based on mode and X flag.</summary>
        /// <returns>The index register size in bits (8, 16, or 32).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetIndexSize()
        {
            if (registers.Is65C02Mode())
            {
                return 8;
            }

            if (registers.Is65816Mode())
            {
                return registers.P.IsIndexSize8Bit() ? (byte)8 : (byte)16;
            }

            return 32; // 65832 mode
        }

        /// <summary>Resets all registers to their default power-on state.</summary>
        /// <param name="compat">
        /// A boolean value indicating whether to reset the registers in compatibility mode.
        /// When set to <c>true</c>, compatibility mode is enabled.
        /// </param>
        /// <remarks>
        /// When compatibility mode is enabled, the CPU starts in 6502 emulation mode first.
        /// </remarks>
        /// <returns>
        /// The <see cref="Registers"/> instance with all registers reset to their default state.
        /// </returns>
        public Registers Reset(bool compat = false)
        {
            registers.A = registers.A.Clear();
            registers.X = registers.X.Clear();
            registers.Y = registers.Y.Clear();
            registers.D = registers.D.Clear();
            registers.SP.stack = 0xFF; // Stack at $01FF in 6502 mode
            registers.PC = registers.PC.Reset();
            registers.P = ProcessorStatusFlags.Reset; // I, M, X set; others cleared
            registers.E = compat;  // Start in 6502 emulation mode
            registers.CP = compat; // Compatibility mode
            registers.DBR = 0;
            registers.PBR = 0;
            registers.R0 = 0;
            registers.R1 = 0;
            registers.R2 = 0;
            registers.R3 = 0;
            registers.R4 = 0;
            registers.R5 = 0;
            registers.R6 = 0;
            registers.R7 = 0;
            registers.System = new();
            return registers;
        }
    }

    #endregion
}