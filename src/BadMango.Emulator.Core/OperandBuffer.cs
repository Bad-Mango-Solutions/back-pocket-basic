// <copyright file="OperandBuffer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.InteropServices;

/// <summary>
/// Fixed-size buffer for instruction operands (up to 4 bytes for 65816/65832 support).
/// </summary>
/// <remarks>
/// This is a value type that avoids heap allocation for the operand buffer.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OperandBuffer
{
    private byte byte0;
    private byte byte1;
    private byte byte2;
    private byte byte3;

    /// <summary>
    /// Gets or sets the operand byte at the specified index.
    /// </summary>
    /// <param name="index">The index of the operand byte (0-3).</param>
    /// <returns>The operand byte at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when index is out of range.</exception>
    public byte this[int index]
    {
        readonly get => index switch
        {
            0 => byte0,
            1 => byte1,
            2 => byte2,
            3 => byte3,
            _ => throw new IndexOutOfRangeException("Operand index must be 0-3"),
        };
        set
        {
            switch (index)
            {
                case 0: byte0 = value; break;
                case 1: byte1 = value; break;
                case 2: byte2 = value; break;
                case 3: byte3 = value; break;
                default: throw new IndexOutOfRangeException("Operand index must be 0-3");
            }
        }
    }

    /// <summary>
    /// Converts the operand buffer to a byte array.
    /// </summary>
    /// <returns>A new byte array containing the operand bytes.</returns>
    public readonly byte[] ToArray() => [byte0, byte1, byte2, byte3];
}