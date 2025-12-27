// <copyright file="RamTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

/// <summary>
/// A bus target implementation for RAM (Random Access Memory).
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides read-write memory with support for Peek/Poke operations
/// and atomic wide access. RAM is the most permissive memory type with no side effects.
/// </para>
/// <para>
/// The address parameter passed to read/write methods is the physical address within
/// the RAM's address space, not the CPU's address space. The bus is responsible for
/// translating CPU addresses to physical addresses via the page table.
/// </para>
/// <para>
/// <see cref="RamTarget"/> is a view into physical memory, not an owner. Create targets
/// by slicing an <see cref="IPhysicalMemory"/> instance. Multiple targets can share
/// overlapping or non-overlapping views of the same physical storage.
/// </para>
/// </remarks>
public sealed class RamTarget : IBusTarget
{
    private readonly Memory<byte> memory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RamTarget"/> class with a memory slice.
    /// </summary>
    /// <param name="memorySlice">
    /// The memory slice to use for this RAM target. This is a view into physical memory,
    /// not owned storage. Changes to this target are visible through the original
    /// <see cref="IPhysicalMemory"/> instance.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when memorySlice is empty.</exception>
    public RamTarget(Memory<byte> memorySlice)
    {
        if (memorySlice.IsEmpty)
        {
            throw new ArgumentException("Memory slice cannot be empty.", nameof(memorySlice));
        }

        memory = memorySlice;
    }

    /// <inheritdoc />
    /// <remarks>
    /// RAM supports Peek, Poke, and wide atomic access, with no side effects.
    /// </remarks>
    public TargetCaps Capabilities =>
        TargetCaps.SupportsPeek | TargetCaps.SupportsPoke | TargetCaps.SupportsWide;

    /// <summary>
    /// Gets the size of the RAM in bytes.
    /// </summary>
    public uint Size => (uint)memory.Length;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        return memory.Span[(int)physicalAddress];
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        memory.Span[(int)physicalAddress] = value;
    }

    /// <summary>
    /// Reads a 16-bit word from the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to read from.</param>
    /// <param name="access">The access context.</param>
    /// <returns>The 16-bit value at the address (little-endian).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Word Read16(Addr physicalAddress, in BusAccess access)
    {
        var span = memory.Span;
        int index = (int)physicalAddress;
        return (Word)(span[index] | (span[index + 1] << 8));
    }

    /// <summary>
    /// Writes a 16-bit word to the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to write to.</param>
    /// <param name="value">The 16-bit value to write (little-endian).</param>
    /// <param name="access">The access context.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write16(Addr physicalAddress, Word value, in BusAccess access)
    {
        var span = memory.Span;
        int index = (int)physicalAddress;
        span[index] = (byte)value;
        span[index + 1] = (byte)(value >> 8);
    }

    /// <summary>
    /// Reads a 32-bit double word from the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to read from.</param>
    /// <param name="access">The access context.</param>
    /// <returns>The 32-bit value at the address (little-endian).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    /// Writes a 32-bit double word to the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to write to.</param>
    /// <param name="value">The 32-bit value to write (little-endian).</param>
    /// <param name="access">The access context.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write32(Addr physicalAddress, DWord value, in BusAccess access)
    {
        var span = memory.Span;
        int index = (int)physicalAddress;
        span[index] = (byte)value;
        span[index + 1] = (byte)(value >> 8);
        span[index + 2] = (byte)(value >> 16);
        span[index + 3] = (byte)(value >> 24);
    }

    /// <summary>
    /// Clears all RAM to zero.
    /// </summary>
    /// <remarks>
    /// This method efficiently clears the underlying memory using <see cref="Span{T}.Clear"/>,
    /// which is significantly faster than writing byte-by-byte through the bus.
    /// </remarks>
    public void Clear()
    {
        memory.Span.Clear();
    }
}