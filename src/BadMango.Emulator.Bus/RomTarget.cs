// <copyright file="RomTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// A bus target implementation for ROM (Read-Only Memory).
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides read-only memory with support for Peek operations
/// and atomic wide access. ROM supports no side effects and ignores write operations.
/// </para>
/// <para>
/// The address parameter passed to read methods is the physical address within
/// the ROM's address space, not the CPU's address space. The bus is responsible for
/// translating CPU addresses to physical addresses via the page table.
/// </para>
/// <para>
/// Write operations are silently ignored to match real ROM behavior where writes
/// have no effect on the stored data.
/// </para>
/// <para>
/// <see cref="RomTarget"/> is a view into physical memory, not an owner. Create targets
/// by slicing an <see cref="IPhysicalMemory"/> instance. Multiple targets can share
/// overlapping or non-overlapping views of the same physical storage.
/// </para>
/// </remarks>
public sealed class RomTarget : IBusTarget
{
    private readonly ReadOnlyMemory<byte> memory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RomTarget"/> class with a memory slice.
    /// </summary>
    /// <param name="memorySlice">
    /// The read-only memory slice to use for this ROM target. This is a view into
    /// physical memory, not owned storage.
    /// </param>
    public RomTarget(ReadOnlyMemory<byte> memorySlice)
    {
        memory = memorySlice;
    }

    /// <inheritdoc />
    /// <remarks>
    /// ROM supports Peek and wide atomic access. It does not support Poke
    /// since writes are ignored. ROM has no side effects.
    /// </remarks>
    public TargetCaps Capabilities => TargetCaps.SupportsPeek | TargetCaps.SupportsWide;

    /// <summary>
    /// Gets the size of the ROM in bytes.
    /// </summary>
    public uint Size => (uint)memory.Length;

    /// <inheritdoc />
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        return memory.Span[(int)physicalAddress];
    }

    /// <inheritdoc />
    /// <remarks>
    /// Writes to ROM are silently ignored, matching real ROM behavior.
    /// </remarks>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        // Writes to ROM are silently ignored
    }

    /// <summary>
    /// Reads a 16-bit word from the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to read from.</param>
    /// <param name="access">The access context.</param>
    /// <returns>The 16-bit value at the address (little-endian).</returns>
    public Word Read16(Addr physicalAddress, in BusAccess access)
    {
        var span = memory.Span;
        int index = (int)physicalAddress;
        return (Word)(span[index] | (span[index + 1] << 8));
    }

    /// <summary>
    /// Writes a 16-bit word to ROM (silently ignored).
    /// </summary>
    /// <param name="physicalAddress">The physical address (ignored).</param>
    /// <param name="value">The value (ignored).</param>
    /// <param name="access">The access context.</param>
    public void Write16(Addr physicalAddress, Word value, in BusAccess access)
    {
        // Writes to ROM are silently ignored
    }

    /// <summary>
    /// Reads a 32-bit double word from the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to read from.</param>
    /// <param name="access">The access context.</param>
    /// <returns>The 32-bit value at the address (little-endian).</returns>
    public DWord Read32(Addr physicalAddress, in BusAccess access)
    {
        var span = memory.Span;
        int index = (int)physicalAddress;
        return (DWord)(
            span[index] |
            (span[index + 1] << 8) |
            (span[index + 2] << 16) |
            (span[index + 3] << 24));
    }

    /// <summary>
    /// Writes a 32-bit double word to ROM (silently ignored).
    /// </summary>
    /// <param name="physicalAddress">The physical address (ignored).</param>
    /// <param name="value">The value (ignored).</param>
    /// <param name="access">The access context.</param>
    public void Write32(Addr physicalAddress, DWord value, in BusAccess access)
    {
        // Writes to ROM are silently ignored
    }
}