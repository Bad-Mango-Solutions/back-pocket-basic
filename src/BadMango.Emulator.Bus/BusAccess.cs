// <copyright file="BusAccess.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents a fully described context for a bus access operation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="BusAccess"/> structure captures all information needed by the bus
/// and connected devices to correctly handle a memory operation. Every subsystem
/// speaks the same "access language" through this structure.
/// </para>
/// <para>
/// The CPU computes the effective width based on mode, E/M/X flags, and instruction
/// semantics, then populates <see cref="WidthBits"/>. The bus enforces consequences
/// (atomic vs decomposed, cross-page rules, tracing) without needing to understand
/// CPU flag logic.
/// </para>
/// <para>
/// The <see cref="PrivilegeLevel"/> field indicates the requestor's privilege ring.
/// The bus may check this against the page entry's minimum privilege requirements.
/// In compat mode, this defaults to Ring 0 (full access).
/// </para>
/// </remarks>
/// <param name="Address">The memory address for the operation.</param>
/// <param name="Value">The value for write operations; ignored for reads.</param>
/// <param name="WidthBits">The effective width of the operation (8, 16, or 32 bits).</param>
/// <param name="Mode">The CPU execution mode (Native or Compat).</param>
/// <param name="EmulationFlag">The 65816 E flag state; only meaningful in Compat mode.</param>
/// <param name="Intent">The purpose of the access (data, fetch, DMA, debug).</param>
/// <param name="SourceId">Structural identifier of the access initiator (CPU, DMA channel, debugger).</param>
/// <param name="Cycle">The current machine cycle for timing correlation.</param>
/// <param name="Flags">Flags modifying access behavior (side effects, atomicity, endianness).</param>
/// <param name="PrivilegeLevel">The requestor's privilege level. Defaults to Ring 0 for compat mode.</param>
public readonly record struct BusAccess(
    Addr Address,
    DWord Value,
    byte WidthBits,
    CpuMode Mode,
    bool EmulationFlag,
    AccessIntent Intent,
    int SourceId,
    ulong Cycle,
    AccessFlags Flags,
    PrivilegeLevel PrivilegeLevel = PrivilegeLevel.Ring0)
{
    /// <summary>
    /// Gets a value indicating whether this access should suppress side effects.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the access should not trigger soft switches,
    /// clear flags, or cause other state changes; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsSideEffectFree => (Flags & AccessFlags.NoSideEffects) != 0;

    /// <summary>
    /// Gets a value indicating whether atomic wide access is requested.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the access prefers atomic wide operations
    /// when supported; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsAtomicRequested => (Flags & AccessFlags.Atomic) != 0;

    /// <summary>
    /// Gets a value indicating whether the access should be decomposed into byte cycles.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the access should be forcibly decomposed into
    /// individual byte operations; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsDecomposeForced => (Flags & AccessFlags.Decompose) != 0;

    /// <summary>
    /// Gets a value indicating whether this is a debug (Peek/Poke) access.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the intent indicates a debug operation;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsDebugAccess => Intent == AccessIntent.DebugRead || Intent == AccessIntent.DebugWrite;

    /// <summary>
    /// Gets a value indicating whether this is a DMA access.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the intent indicates a DMA operation;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsDmaAccess => Intent == AccessIntent.DmaRead || Intent == AccessIntent.DmaWrite;

    /// <summary>
    /// Gets a value indicating whether this is a read operation.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the intent is a read operation;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsRead => Intent is AccessIntent.DataRead
        or AccessIntent.InstructionFetch
        or AccessIntent.DebugRead
        or AccessIntent.DmaRead;

    /// <summary>
    /// Gets a value indicating whether this is a write operation.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the intent is a write operation;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsWrite => Intent is AccessIntent.DataWrite
        or AccessIntent.DebugWrite
        or AccessIntent.DmaWrite;

    /// <summary>
    /// Creates a new <see cref="BusAccess"/> with the address incremented by the specified offset.
    /// </summary>
    /// <param name="offset">The offset to add to the address.</param>
    /// <returns>A new <see cref="BusAccess"/> with the updated address.</returns>
    public BusAccess WithAddressOffset(uint offset) => this with { Address = Address + offset };
}