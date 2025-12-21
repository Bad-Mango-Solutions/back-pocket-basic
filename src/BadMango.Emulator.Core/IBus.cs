// <copyright file="IBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

/// <summary>
/// Represents a system bus interface for communication between CPU, memory, and peripherals.
/// </summary>
/// <remarks>
/// The bus interface provides extension points for future device communication,
/// expansion slots, memory-mapped I/O, and peripheral integration.
/// </remarks>
public interface IBus
{
    /// <summary>
    /// Reads a byte from the bus at the specified address.
    /// </summary>
    /// <param name="address">The address to read from.</param>
    /// <returns>The byte value at the specified address.</returns>
    byte Read(int address);

    /// <summary>
    /// Writes a byte to the bus at the specified address.
    /// </summary>
    /// <param name="address">The address to write to.</param>
    /// <param name="value">The byte value to write.</param>
    void Write(int address, byte value);
}