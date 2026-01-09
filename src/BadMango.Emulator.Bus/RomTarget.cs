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
/// and atomic wide access. ROM supports no side effects and ignores normal write
/// operations, but allows debug writes (Poke) for debugging purposes.
/// </para>
/// <para>
/// The address parameter passed to read methods is the physical address within
/// the ROM's address space, not the CPU's address space. The bus is responsible for
/// translating CPU addresses to physical addresses via the page table.
/// </para>
/// <para>
/// Normal write operations are silently ignored to match real ROM behavior where
/// writes have no effect on the stored data. Debug writes (with
/// <see cref="AccessIntent.DebugWrite"/>) are allowed for debugging and patching.
/// </para>
/// <para>
/// <see cref="RomTarget"/> is a view into physical memory, not an owner. Create targets
/// by slicing an <see cref="IPhysicalMemory"/> instance. Multiple targets can share
/// overlapping or non-overlapping views of the same physical storage.
/// </para>
/// </remarks>
public sealed class RomTarget : IBusTarget
{
    private readonly Memory<byte> memory;
    private readonly ReadOnlyMemory<byte> readOnlyMemory;
    private readonly bool isWritable;

    /// <summary>
    /// Initializes a new instance of the <see cref="RomTarget"/> class with a memory slice.
    /// </summary>
    /// <param name="memorySlice">
    /// The read-only memory slice to use for this ROM target. This is a view into
    /// physical memory, not owned storage.
    /// </param>
    /// <param name="name">
    /// Optional name for this ROM target. If not specified, defaults to "ROM".
    /// </param>
    public RomTarget(ReadOnlyMemory<byte> memorySlice, string? name = null)
    {
        // We need writable backing for debug writes, but ReadOnlyMemory doesn't allow that.
        // This constructor maintains API compatibility but debug writes won't work.
        // Use the Memory<byte> constructor for full functionality.
        memory = default;
        readOnlyMemory = memorySlice;
        isWritable = false;
        Name = name ?? "ROM";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RomTarget"/> class with a writable memory slice.
    /// </summary>
    /// <param name="memorySlice">
    /// The memory slice to use for this ROM target. This is a view into physical memory,
    /// not owned storage. Using this constructor enables debug writes.
    /// </param>
    /// <param name="name">
    /// Optional name for this ROM target. If not specified, defaults to "ROM".
    /// </param>
    /// <remarks>
    /// Use this constructor when you need to support debug writes (Poke) to ROM.
    /// Normal writes are still ignored; only debug writes are allowed.
    /// </remarks>
    public RomTarget(Memory<byte> memorySlice, string? name = null)
    {
        memory = memorySlice;
        readOnlyMemory = memorySlice;
        isWritable = true;
        Name = name ?? "ROM";
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    /// <remarks>
    /// ROM supports Peek, Poke (debug write), and wide atomic access.
    /// ROM has no side effects for normal operations.
    /// </remarks>
    public TargetCaps Capabilities => TargetCaps.SupportsPeek | TargetCaps.SupportsPoke | TargetCaps.SupportsWide;

    /// <summary>
    /// Gets the size of the ROM in bytes.
    /// </summary>
    public uint Size => (uint)readOnlyMemory.Length;

    /// <inheritdoc />
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        return readOnlyMemory.Span[(int)physicalAddress];
    }

    /// <inheritdoc />
    /// <remarks>
    /// Normal writes to ROM are silently ignored, matching real ROM behavior.
    /// Debug writes (Poke) are allowed for debugging and patching purposes.
    /// </remarks>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        // Only allow debug writes (Poke)
        if (isWritable && access.IsDebugAccess)
        {
            memory.Span[(int)physicalAddress] = value;
        }

        // Normal writes are silently ignored
    }

    /// <summary>
    /// Reads a 16-bit word from the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to read from.</param>
    /// <param name="access">The access context.</param>
    /// <returns>The 16-bit value at the address (little-endian).</returns>
    public Word Read16(Addr physicalAddress, in BusAccess access)
    {
        var span = readOnlyMemory.Span;
        int index = (int)physicalAddress;
        return (Word)(span[index] | (span[index + 1] << 8));
    }

    /// <summary>
    /// Writes a 16-bit word to ROM.
    /// </summary>
    /// <param name="physicalAddress">The physical address.</param>
    /// <param name="value">The value.</param>
    /// <param name="access">The access context.</param>
    /// <remarks>
    /// Normal writes are silently ignored. Debug writes are allowed.
    /// </remarks>
    public void Write16(Addr physicalAddress, Word value, in BusAccess access)
    {
        // Only allow debug writes (Poke)
        if (isWritable && access.IsDebugAccess)
        {
            var span = memory.Span;
            int index = (int)physicalAddress;
            span[index] = (byte)value;
            span[index + 1] = (byte)(value >> 8);
        }

        // Normal writes are silently ignored
    }

    /// <summary>
    /// Reads a 32-bit double word from the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to read from.</param>
    /// <param name="access">The access context.</param>
    /// <returns>The 32-bit value at the address (little-endian).</returns>
    public DWord Read32(Addr physicalAddress, in BusAccess access)
    {
        var span = readOnlyMemory.Span;
        int index = (int)physicalAddress;
        return (DWord)(
            span[index] |
            (span[index + 1] << 8) |
            (span[index + 2] << 16) |
            (span[index + 3] << 24));
    }

    /// <summary>
    /// Writes a 32-bit double word to ROM.
    /// </summary>
    /// <param name="physicalAddress">The physical address.</param>
    /// <param name="value">The value.</param>
    /// <param name="access">The access context.</param>
    /// <remarks>
    /// Normal writes are silently ignored. Debug writes are allowed.
    /// </remarks>
    public void Write32(Addr physicalAddress, DWord value, in BusAccess access)
    {
        // Only allow debug writes (Poke)
        if (isWritable && access.IsDebugAccess)
        {
            var span = memory.Span;
            int index = (int)physicalAddress;
            span[index] = (byte)value;
            span[index + 1] = (byte)(value >> 8);
            span[index + 2] = (byte)(value >> 16);
            span[index + 3] = (byte)(value >> 24);
        }

        // Normal writes are silently ignored
    }
}