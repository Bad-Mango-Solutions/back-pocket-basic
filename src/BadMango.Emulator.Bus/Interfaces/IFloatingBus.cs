// <copyright file="IFloatingBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Exposes the Apple II floating-bus latch maintained by the main memory bus.
/// </summary>
/// <remarks>
/// <para>
/// Soft-switch reads and other non-driving accesses return the last value that
/// appeared on the data bus during a prior instruction fetch or RAM/ROM read.
/// </para>
/// </remarks>
public interface IFloatingBus
{
    /// <summary>
    /// Gets the current floating-bus value.
    /// </summary>
    /// <value>The latched data-bus byte.</value>
    byte FloatingBusValue { get; }

    /// <summary>
    /// Updates the floating-bus latch.
    /// </summary>
    /// <param name="value">The byte to latch.</param>
    void LatchFloatingBus(byte value);
}