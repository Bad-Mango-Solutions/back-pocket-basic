// <copyright file="AccessFlags.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Flags that modify the behavior of a bus access operation.
/// </summary>
/// <remarks>
/// Access flags allow fine-grained control over how the bus processes
/// memory operations, including side-effect policy, atomic vs decomposed
/// handling, and endianness for wide operations.
/// </remarks>
[Flags]
public enum AccessFlags : uint
{
    /// <summary>
    /// No special flags applied; use default behavior.
    /// </summary>
    None = 0,

    /// <summary>
    /// Suppresses side effects during the access operation.
    /// </summary>
    /// <remarks>
    /// When set, the access should not toggle soft switches, clear interrupt
    /// flags, advance disk state, or cause any other observable state change
    /// beyond the raw read/write of data. Used for Peek/Poke semantics.
    /// </remarks>
    NoSideEffects = 1 << 0,

    /// <summary>
    /// Indicates little-endian byte order for wide operations.
    /// </summary>
    /// <remarks>
    /// When set, multi-byte operations use little-endian byte order
    /// (low byte first, high byte second). This is the standard byte
    /// order for 6502-family processors.
    /// </remarks>
    LittleEndian = 1 << 1,

    /// <summary>
    /// Requests atomic execution of a wide access operation.
    /// </summary>
    /// <remarks>
    /// When set, the bus attempts to perform 16-bit or 32-bit operations
    /// as a single transaction if the target device supports it. If the
    /// target does not support atomic wide access, the bus falls back to
    /// decomposed byte-wise operations.
    /// </remarks>
    Atomic = 1 << 2,

    /// <summary>
    /// Forces decomposition of wide accesses into individual byte cycles.
    /// </summary>
    /// <remarks>
    /// When set, even if the target supports atomic wide access, the bus
    /// will decompose the operation into individual byte reads/writes.
    /// This is useful for Apple II compatibility where software and devices
    /// may depend on observing individual bus cycles.
    /// </remarks>
    Decompose = 1 << 3,
}