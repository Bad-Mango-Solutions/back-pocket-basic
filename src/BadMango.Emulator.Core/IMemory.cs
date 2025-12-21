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
    int Size { get; }

    /// <summary>
    /// Reads a byte from the specified memory address.
    /// </summary>
    /// <param name="address">The memory address to read from.</param>
    /// <returns>The byte value at the specified address.</returns>
    byte Read(int address);

    /// <summary>
    /// Writes a byte to the specified memory address.
    /// </summary>
    /// <param name="address">The memory address to write to.</param>
    /// <param name="value">The byte value to write.</param>
    void Write(int address, byte value);

    /// <summary>
    /// Reads a 16-bit word from the specified memory address.
    /// </summary>
    /// <param name="address">The starting address to read from.</param>
    /// <returns>The 16-bit word value (low byte first, high byte second).</returns>
    ushort ReadWord(int address);

    /// <summary>
    /// Writes a 16-bit word to the specified memory address.
    /// </summary>
    /// <param name="address">The starting address to write to.</param>
    /// <param name="value">The 16-bit word value to write (low byte first, high byte second).</param>
    void WriteWord(int address, ushort value);

    /// <summary>
    /// Clears all memory, setting all bytes to zero.
    /// </summary>
    void Clear();
}