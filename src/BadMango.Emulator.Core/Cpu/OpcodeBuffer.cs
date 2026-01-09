// <copyright file="OpcodeBuffer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

using System.Runtime.InteropServices;

/// <summary>Represents a buffer for storing opcode data in the CPU emulator.</summary>
/// <remarks>
/// This structure is designed to hold two bytes of opcode data, providing access to
/// the primary opcode and sub-opcode. It supports indexed access to the bytes and
/// ensures memory layout compatibility through sequential packing.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct OpcodeBuffer
{
    private byte byte0;
    private byte byte1;

    /// <summary>Gets the primary opcode stored in the <see cref="OpcodeBuffer"/>.</summary>
    /// <value>A <see cref="byte"/> representing the primary opcode.</value>
    public readonly byte OpcodeByte => byte0;

    /// <summary>Gets the sub-opcode byte from the opcode buffer.</summary>
    /// <remarks>
    /// The sub-opcode represents the second byte of the opcode data stored in the buffer.
    /// It is primarily used in scenarios where multibyte opcodes are required.
    /// </remarks>
    public readonly byte SubOpcodeByte => byte1;

    /// <summary>
    /// Gets the 16-bit word representation of the opcode, combining the primary opcode byte
    /// and the sub-opcode byte.
    /// </summary>
    /// <value>
    /// A <see cref="Word"/> representing the combined opcode, where the primary opcode byte
    /// is the most significant byte, and the sub-opcode byte is the least significant byte.
    /// </value>
    /// <remarks>
    /// This property provides a convenient way to access the full 16-bit opcode as a single
    /// value, which is useful for operations requiring the complete opcode representation.
    /// </remarks>
    public readonly Word Opcode => (Word)(byte1 | (byte0 << 8));

    /// <summary>Gets or sets the byte at the specified index in the <see cref="OpcodeBuffer"/>.</summary>
    /// <param name="index">The zero-based index of the byte to access. Valid values are 0 or 1.</param>
    /// <returns>The byte at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when the specified <paramref name="index"/> is not 0 or 1.
    /// </exception>
    public byte this[int index]
    {
        readonly get => index switch
        {
            0 => byte0,
            1 => byte1,
            _ => throw new IndexOutOfRangeException("Index must be 0 or 1."),
        };
        set
        {
            switch (index)
            {
                case 0: byte0 = value; break;
                case 1: byte1 = value; break;
                default: throw new IndexOutOfRangeException("Index must be 0 or 1.");
            }
        }
    }

    /// <summary>Converts the opcode buffer to a byte array.</summary>
    /// <returns>A new byte array containing the opcode bytes.</returns>
    public readonly byte[] ToArray() => [byte0, byte1];
}