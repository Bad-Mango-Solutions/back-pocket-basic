// <copyright file="IPeripheral.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

/// <summary>
/// Represents a peripheral device that can be attached to the system.
/// </summary>
/// <remarks>
/// This interface provides extension points for device implementations such as
/// disk controllers, video cards, sound devices, expansion cards, and other peripherals.
/// Peripherals can respond to memory-mapped I/O operations and system events.
/// </remarks>
public interface IPeripheral
{
    /// <summary>
    /// Gets the name of the peripheral device.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Resets the peripheral to its initial state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Handles a read operation from a peripheral I/O address.
    /// </summary>
    /// <param name="address">The I/O address being read.</param>
    /// <returns>The byte value from the peripheral, or null if the address is not handled by this device.</returns>
    byte? ReadIO(int address);

    /// <summary>
    /// Handles a write operation to a peripheral I/O address.
    /// </summary>
    /// <param name="address">The I/O address being written.</param>
    /// <param name="value">The byte value being written.</param>
    /// <returns>True if the peripheral handled the write operation, false otherwise.</returns>
    bool WriteIO(int address, byte value);
}