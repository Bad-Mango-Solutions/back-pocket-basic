// <copyright file="IBusTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Represents a target that can be accessed through the memory bus.
/// </summary>
/// <remarks>
/// <para>
/// Bus targets are the endpoints for memory operations. They can represent
/// RAM, ROM, memory-mapped I/O, or any other addressable resource.
/// </para>
/// <para>
/// The interface provides required 8-bit primitives (<see cref="Read8"/> and <see cref="Write8"/>)
/// that must be implemented, plus optional 16-bit and 32-bit operations with default
/// implementations that decompose into byte operations.
/// </para>
/// <para>
/// Targets advertise their capabilities via <see cref="Capabilities"/>, allowing the
/// bus to make informed decisions about atomic vs decomposed access.
/// </para>
/// </remarks>
public interface IBusTarget
{
    /// <summary>
    /// Gets the name of this bus target.
    /// </summary>
    /// <value>A human-readable name for diagnostics, debugging, and display purposes.</value>
    /// <remarks>
    /// The default implementation returns the type name. Implementations should override
    /// this to provide a more descriptive name, such as "main-ram-48k" or "system-rom-16k".
    /// </remarks>
    string Name => GetType().Name;

    /// <summary>
    /// Gets the capabilities of this bus target.
    /// </summary>
    /// <value>A combination of <see cref="TargetCaps"/> flags describing what this target supports.</value>
    TargetCaps Capabilities { get; }

    /// <summary>
    /// Reads a single byte from the specified physical address.
    /// </summary>
    /// <param name="physicalAddress">The physical address within the target's address space.</param>
    /// <param name="access">The access context describing the operation.</param>
    /// <returns>The byte value at the specified address.</returns>
    byte Read8(Addr physicalAddress, in BusAccess access);

    /// <summary>
    /// Writes a single byte to the specified physical address.
    /// </summary>
    /// <param name="physicalAddress">The physical address within the target's address space.</param>
    /// <param name="value">The byte value to write.</param>
    /// <param name="access">The access context describing the operation.</param>
    void Write8(Addr physicalAddress, byte value, in BusAccess access);

    /// <summary>
    /// Reads a 16-bit word from the specified physical address.
    /// </summary>
    /// <param name="physicalAddress">The physical address within the target's address space.</param>
    /// <param name="access">The access context describing the operation.</param>
    /// <returns>The 16-bit value at the specified address (little-endian).</returns>
    /// <remarks>
    /// The default implementation decomposes the read into two byte operations.
    /// Targets that support atomic wide access can override this for better performance.
    /// </remarks>
    Word Read16(Addr physicalAddress, in BusAccess access)
    {
        byte low = Read8(physicalAddress, access);
        byte high = Read8(physicalAddress + 1, access.WithAddressOffset(1));
        return (Word)(low | (high << 8));
    }

    /// <summary>
    /// Writes a 16-bit word to the specified physical address.
    /// </summary>
    /// <param name="physicalAddress">The physical address within the target's address space.</param>
    /// <param name="value">The 16-bit value to write (little-endian).</param>
    /// <param name="access">The access context describing the operation.</param>
    /// <remarks>
    /// The default implementation decomposes the write into two byte operations.
    /// Targets that support atomic wide access can override this for better performance.
    /// </remarks>
    void Write16(Addr physicalAddress, Word value, in BusAccess access)
    {
        Write8(physicalAddress, (byte)value, access);
        Write8(physicalAddress + 1, (byte)(value >> 8), access.WithAddressOffset(1));
    }

    /// <summary>
    /// Reads a 32-bit double word from the specified physical address.
    /// </summary>
    /// <param name="physicalAddress">The physical address within the target's address space.</param>
    /// <param name="access">The access context describing the operation.</param>
    /// <returns>The 32-bit value at the specified address (little-endian).</returns>
    /// <remarks>
    /// The default implementation decomposes the read into four byte operations.
    /// Targets that support atomic wide access can override this for better performance.
    /// </remarks>
    DWord Read32(Addr physicalAddress, in BusAccess access)
    {
        byte b0 = Read8(physicalAddress, access);
        byte b1 = Read8(physicalAddress + 1, access.WithAddressOffset(1));
        byte b2 = Read8(physicalAddress + 2, access.WithAddressOffset(2));
        byte b3 = Read8(physicalAddress + 3, access.WithAddressOffset(3));
        return (DWord)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24));
    }

    /// <summary>
    /// Writes a 32-bit double word to the specified physical address.
    /// </summary>
    /// <param name="physicalAddress">The physical address within the target's address space.</param>
    /// <param name="value">The 32-bit value to write (little-endian).</param>
    /// <param name="access">The access context describing the operation.</param>
    /// <remarks>
    /// The default implementation decomposes the write into four byte operations.
    /// Targets that support atomic wide access can override this for better performance.
    /// </remarks>
    void Write32(Addr physicalAddress, DWord value, in BusAccess access)
    {
        Write8(physicalAddress, (byte)value, access);
        Write8(physicalAddress + 1, (byte)(value >> 8), access.WithAddressOffset(1));
        Write8(physicalAddress + 2, (byte)(value >> 16), access.WithAddressOffset(2));
        Write8(physicalAddress + 3, (byte)(value >> 24), access.WithAddressOffset(3));
    }

    /// <summary>
    /// Clears all data in this target to zero.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is used for bulk memory clearing without causing side effects.
    /// Each target type is responsible for its own clearing behavior, allowing
    /// for efficient implementations (e.g., <c>Array.Clear</c> for RAM).
    /// </para>
    /// <para>
    /// The default implementation does nothing. Read-only targets like ROM
    /// should not override this method. RAM targets should override to clear
    /// their underlying storage efficiently.
    /// </para>
    /// <para>
    /// This operation should NEVER cause side effects (no I/O triggers, no
    /// timing-sensitive behavior).
    /// </para>
    /// </remarks>
    void Clear()
    {
        // Default: no-op. ROM and other read-only targets don't need clearing.
    }
}