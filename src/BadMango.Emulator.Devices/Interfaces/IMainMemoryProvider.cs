// <copyright file="IMainMemoryProvider.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Interface for providing direct access to the main RAM physical memory.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides direct read access to the physical main RAM backing store,
/// bypassing all soft switch mapping (80STORE, PAGE2, RAMRD). This is critical for
/// video rendering, where the display hardware accesses VRAM directly without going
/// through the MMU soft switch logic.
/// </para>
/// <para>
/// Without this interface, reading from the text page address range ($0400-$07FF)
/// through the memory bus may be redirected to auxiliary RAM when 80STORE is enabled
/// and PAGE2 is toggled, causing the display to show incorrect data.
/// </para>
/// </remarks>
public interface IMainMemoryProvider
{
    /// <summary>
    /// Gets the main RAM as a read-only memory block for direct physical access.
    /// </summary>
    /// <value>A <see cref="ReadOnlyMemory{T}"/> covering the main RAM.</value>
    /// <remarks>
    /// This property provides direct access to the physical main RAM, bypassing
    /// all bus routing and soft switch mapping. It is intended for use by the
    /// video display subsystem to capture consistent frame snapshots.
    /// </remarks>
    ReadOnlyMemory<byte> MainRam { get; }

    /// <summary>
    /// Reads a byte from main RAM at the specified address.
    /// </summary>
    /// <param name="address">The address within main RAM.</param>
    /// <returns>The byte value at the specified address.</returns>
    /// <remarks>
    /// This method provides direct read access to main RAM for video rendering.
    /// It does not go through the memory bus and does not respect soft switch
    /// routing (80STORE, PAGE2, RAMRD).
    /// </remarks>
    byte ReadMainRam(ushort address);
}