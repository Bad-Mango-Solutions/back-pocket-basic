// <copyright file="Instructions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

/// <summary>
/// Provides reusable instruction implementations for 6502-family CPUs.
/// </summary>
/// <remarks>
/// This class contains static methods that implement common instructions
/// that can be composed with different addressing modes. Each method is
/// aggressively inlined for optimal performance in hot-path execution.
/// </remarks>
public static class Instructions
{
    /// <summary>
    /// LDA - Load Accumulator instruction logic.
    /// </summary>
    /// <param name="value">The value to load into the accumulator.</param>
    /// <param name="a">Reference to the accumulator register (will be updated).</param>
    /// <param name="p">Reference to the processor status register (will be updated for Z and N flags).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDA(byte value, ref byte a, ref byte p)
    {
        a = value;
        SetZN(value, ref p);
    }

    /// <summary>
    /// LDX - Load X Register instruction logic.
    /// </summary>
    /// <param name="value">The value to load into the X register.</param>
    /// <param name="x">Reference to the X register (will be updated).</param>
    /// <param name="p">Reference to the processor status register (will be updated for Z and N flags).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDX(byte value, ref byte x, ref byte p)
    {
        x = value;
        SetZN(value, ref p);
    }

    /// <summary>
    /// LDY - Load Y Register instruction logic.
    /// </summary>
    /// <param name="value">The value to load into the Y register.</param>
    /// <param name="y">Reference to the Y register (will be updated).</param>
    /// <param name="p">Reference to the processor status register (will be updated for Z and N flags).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDY(byte value, ref byte y, ref byte p)
    {
        y = value;
        SetZN(value, ref p);
    }

    /// <summary>
    /// Sets the Zero (Z) and Negative (N) flags based on a value.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="p">Reference to the processor status register (will be updated).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetZN(byte value, ref byte p)
    {
        const byte FlagZ = 0x02;
        const byte FlagN = 0x80;

        if (value == 0)
        {
            p |= FlagZ;
        }
        else
        {
            p &= unchecked((byte)~FlagZ);
        }

        if ((value & 0x80) != 0)
        {
            p |= FlagN;
        }
        else
        {
            p &= unchecked((byte)~FlagN);
        }
    }
}