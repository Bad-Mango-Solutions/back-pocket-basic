// <copyright file="IMemory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Interfaces;

using System.Runtime.CompilerServices;

/// <summary>
/// Base interface for memory management in emulated systems.
/// </summary>
/// <remarks>
/// This interface provides the foundation for various memory models including
/// standard 64KB configurations, banked memory, and extended memory systems.
/// </remarks>
public interface IMemory
{
    /// <summary>
    /// Gets the size of the emulated memory in bytes.
    /// </summary>
    uint Size { get; }

    /// <summary>
    /// Reads a byte from the specified memory address.
    /// </summary>
    /// <param name="address">The memory address to read from.</param>
    /// <returns>The byte value at the specified address.</returns>
    byte Read(Addr address);

    /// <summary>
    /// Reads a byte from the specified memory address.
    /// </summary>
    /// <param name="address">The memory address to read from.</param>
    /// <returns>The byte value at the specified address.</returns>
    /// <remarks>
    /// This method is functionally identical to <see cref="Read"/> and is provided
    /// for future-proofing to support additional read methods (e.g., Read16, Read32).
    /// </remarks>
    byte ReadByte(Addr address) => Read(address);

    /// <summary>
    /// Writes a byte to the specified memory address.
    /// </summary>
    /// <param name="address">The memory address to write to.</param>
    /// <param name="value">The byte value to write.</param>
    void Write(Addr address, byte value);

    /// <summary>
    /// Writes a byte to the specified memory address.
    /// </summary>
    /// <param name="address">The memory address to write to.</param>
    /// <param name="value">The byte value to write.</param>
    /// <remarks>
    /// This method is functionally identical to <see cref="Write"/> and is provided
    /// for future-proofing to support additional write methods (e.g., Write16, Write32).
    /// </remarks>
    void WriteByte(Addr address, byte value) => Write(address, value);

    /// <summary>
    /// Reads a 16-bit word from the specified memory address.
    /// </summary>
    /// <param name="address">The starting address to read from.</param>
    /// <returns>The 16-bit word value (low byte first, high byte second).</returns>
    Word ReadWord(Addr address);

    /// <summary>
    /// Writes a 16-bit word to the specified memory address.
    /// </summary>
    /// <param name="address">The starting address to write to.</param>
    /// <param name="value">The 16-bit word value to write (low byte first, high byte second).</param>
    void WriteWord(Addr address, Word value);

    /// <summary>
    /// Reads a 32-bit double-word from the specified memory address.
    /// </summary>
    /// <param name="address">The starting address to read from.</param>
    /// <returns>The 32-bit double-word value (little-endian byte order).</returns>
    /// <remarks>
    /// Reads four consecutive bytes in little-endian order:
    /// - Byte 0 (bits 0-7)
    /// - Byte 1 (bits 8-15)
    /// - Byte 2 (bits 16-23)
    /// - Byte 3 (bits 24-31).
    /// </remarks>
    DWord ReadDWord(Addr address);

    /// <summary>
    /// Writes a 32-bit double-word to the specified memory address.
    /// </summary>
    /// <param name="address">The starting address to write to.</param>
    /// <param name="value">The 32-bit double-word value to write.</param>
    /// <remarks>
    /// Writes four consecutive bytes in little-endian order:
    /// - Byte 0 (bits 0-7)
    /// - Byte 1 (bits 8-15)
    /// - Byte 2 (bits 16-23)
    /// - Byte 3 (bits 24-31).
    /// </remarks>
    void WriteDWord(Addr address, DWord value);

    /// <summary>
    /// Reads a value from memory based on the specified size.
    /// </summary>
    /// <param name="address">The memory address to read from.</param>
    /// <param name="sizeInBits">The size to read (8, 16, or 32 bits).</param>
    /// <returns>The value read from memory, zero-extended to 32 bits.</returns>
    /// <remarks>
    /// This method allows size-aware memory reads that adapt to different register widths.
    /// For 8-bit reads, only one byte is fetched.
    /// For 16-bit reads, two bytes are fetched (little-endian).
    /// For 32-bit reads, four bytes are fetched (little-endian).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    DWord ReadValue(Addr address, byte sizeInBits)
    {
        return sizeInBits switch
        {
            8 => Read(address),
            16 => ReadWord(address),
            32 => ReadDWord(address),
            _ => throw new ArgumentException($"Invalid size: {sizeInBits}. Must be 8, 16, or 32.", nameof(sizeInBits)),
        };
    }

    /// <summary>
    /// Writes a value to memory based on the specified size.
    /// </summary>
    /// <param name="address">The memory address to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="sizeInBits">The size to write (8, 16, or 32 bits).</param>
    /// <remarks>
    /// This method allows size-aware memory writes that adapt to different register widths.
    /// For 8-bit writes, only the low byte is written.
    /// For 16-bit writes, two bytes are written (little-endian).
    /// For 32-bit writes, four bytes are written (little-endian).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteValue(Addr address, DWord value, byte sizeInBits)
    {
        switch (sizeInBits)
        {
            case 8:
                Write(address, (byte)(value & 0xFF));
                break;
            case 16:
                WriteWord(address, (Word)(value & 0xFFFF));
                break;
            case 32:
                WriteDWord(address, value);
                break;
            default:
                throw new ArgumentException($"Invalid size: {sizeInBits}. Must be 8, 16, or 32.", nameof(sizeInBits));
        }
    }

    /// <summary>
    /// Clears all memory, setting all bytes to zero.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets a read-only view of the entire memory as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    /// <returns>RegisterAccumulator read-only view over the current memory contents.</returns>
    /// <remarks>
    /// This method returns a non-copying, read-only view of the underlying memory buffer
    /// that can be used for debugging, serialization, or analysis without allowing direct
    /// modification through the returned instance. Changes made to the underlying memory
    /// (for example, via <see cref="Write(Addr, byte)"/> or <see cref="WriteWord(Addr, Word)"/>)
    /// will be reflected in any previously obtained <see cref="ReadOnlyMemory{T}"/> views.
    /// </remarks>
    ReadOnlyMemory<byte> AsReadOnlyMemory();

    /// <summary>
    /// Retrieves a segment of memory as a read-only block.
    /// </summary>
    /// <param name="start">The starting address of the memory segment to inspect.</param>
    /// <param name="length">The length of the memory segment to inspect.</param>
    /// <returns>A read-only memory block representing the specified memory segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified range (start and length) exceeds the bounds of the memory.
    /// </exception>
    /// <remarks>
    /// This method allows inspection of a specific portion of memory without modifying its contents.
    /// It is useful for debugging or analyzing memory state.
    /// </remarks>
    ReadOnlyMemory<byte> Inspect(int start, int length);

    /// <summary>
    /// Gets a mutable view of the entire memory as a <see cref="Memory{T}"/>.
    /// </summary>
    /// <returns>RegisterAccumulator mutable view over the current memory contents.</returns>
    /// <remarks>
    /// This method provides direct access to the underlying memory buffer via a
    /// <see cref="Memory{T}"/> view. The returned instance does not represent a copy; any
    /// modifications performed through it, or through other write operations on this
    /// <see cref="IMemory"/> instance, operate on the same underlying data and will affect
    /// the emulated system's state. Use with caution.
    /// </remarks>
    Memory<byte> AsMemory();
}