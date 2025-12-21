// <copyright file="IMemory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

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
    /// Clears all memory, setting all bytes to zero.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets a read-only view of the entire memory as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    /// <returns>A read-only view over the current memory contents.</returns>
    /// <remarks>
    /// This method returns a non-copying, read-only view of the underlying memory buffer
    /// that can be used for debugging, serialization, or analysis without allowing direct
    /// modification through the returned instance. Changes made to the underlying memory
    /// (for example, via <see cref="Write(Addr, byte)"/> or <see cref="WriteWord(Addr, Word)"/>)
    /// will be reflected in any previously obtained <see cref="ReadOnlyMemory{T}"/> views.
    /// </remarks>
    ReadOnlyMemory<byte> AsReadOnlyMemory();

    /// <summary>
    /// Gets a mutable view of the entire memory as a <see cref="Memory{T}"/>.
    /// </summary>
    /// <returns>A mutable view over the current memory contents.</returns>
    /// <remarks>
    /// This method provides direct access to the underlying memory buffer via a
    /// <see cref="Memory{T}"/> view. The returned instance does not represent a copy; any
    /// modifications performed through it, or through other write operations on this
    /// <see cref="IMemory"/> instance, operate on the same underlying data and will affect
    /// the emulated system's state. Use with caution.
    /// </remarks>
    Memory<byte> AsMemory();
}