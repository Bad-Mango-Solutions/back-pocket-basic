// <copyright file="PhysicalMemory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// A concrete implementation of <see cref="IPhysicalMemory"/> that owns a contiguous byte array.
/// </summary>
/// <remarks>
/// <para>
/// This implementation allocates a single contiguous <see cref="byte"/> array and provides
/// slicing operations that return views into that array without copying.
/// </para>
/// <para>
/// Use this class to provision physical memory pools for machines. Targets like
/// <see cref="RamTarget"/> and <see cref="RomTarget"/> receive slices from this pool.
/// </para>
/// </remarks>
public sealed class PhysicalMemory : IPhysicalMemory
{
    private readonly byte[] data;
    private readonly Memory<byte> mem;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalMemory"/> class with the specified size.
    /// </summary>
    /// <param name="size">The size of the memory in bytes.</param>
    /// <param name="name">A descriptive name for this memory pool.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when size is zero.</exception>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    /// <exception cref="ArgumentException">Thrown when name is empty or whitespace.</exception>
    /// <remarks>The memory is zero-initialized.</remarks>
    public PhysicalMemory(uint size, string name)
    {
        ArgumentOutOfRangeException.ThrowIfZero(size);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        data = new byte[size];
        mem = data.AsMemory();
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalMemory"/> class with existing data.
    /// </summary>
    /// <param name="initialData">The initial data to copy into the memory.</param>
    /// <param name="name">A descriptive name for this memory pool.</param>
    /// <exception cref="ArgumentNullException">Thrown when initialData or name is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when initialData has zero length.</exception>
    /// <exception cref="ArgumentException">Thrown when name is empty or whitespace.</exception>
    /// <remarks>The data is copied to ensure the memory owns its storage.</remarks>
    public PhysicalMemory(ReadOnlySpan<byte> initialData, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfZero(initialData.Length);

        data = initialData.ToArray();
        mem = data.AsMemory();
        Name = name;
    }

    /// <inheritdoc />
    public uint Size => (uint)data.Length;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Memory => mem;

    /// <inheritdoc />
    public Memory<byte> Slice(uint offset, uint length)
    {
        ValidateSliceParameters(offset, length);
        return mem.Slice((int)offset, (int)length);
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> ReadOnlySlice(uint offset, uint length)
    {
        ValidateSliceParameters(offset, length);
        return mem.Slice((int)offset, (int)length);
    }

    /// <inheritdoc />
    public Memory<byte> SlicePage(uint pageIndex, uint pageSize = 4096)
    {
        ValidatePageParameters(pageIndex, pageSize);
        uint offset = pageIndex * pageSize;
        return mem.Slice((int)offset, (int)pageSize);
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> ReadOnlySlicePage(uint pageIndex, uint pageSize = 4096)
    {
        ValidatePageParameters(pageIndex, pageSize);
        uint offset = pageIndex * pageSize;
        return mem.Slice((int)offset, (int)pageSize);
    }

    /// <inheritdoc />
    public Span<byte> AsSpan() => mem.Span;

    /// <inheritdoc />
    public ReadOnlySpan<byte> AsReadOnlySpan() => mem.Span;

    /// <inheritdoc />
    public void Fill(byte value) => mem.Span.Fill(value);

    /// <inheritdoc />
    public void Clear() => mem.Span.Clear();

    /// <inheritdoc />
    public uint PageCount(uint pageSize = 4096)
    {
        ArgumentOutOfRangeException.ThrowIfZero(pageSize);
        return (uint)data.Length / pageSize;
    }

    /// <summary>
    /// Writes data to this physical memory at the specified offset.
    /// </summary>
    /// <param name="offset">The offset within this memory where writing starts.</param>
    /// <param name="dataToWrite">The data to write.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the write would exceed memory bounds.
    /// </exception>
    /// <remarks>
    /// This method is used during initialization to load ROM images into physical memory.
    /// </remarks>
    public void WriteSpan(uint offset, ReadOnlySpan<byte> dataToWrite)
    {
        if (offset + (uint)dataToWrite.Length > data.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                $"Write at offset {offset} with length {dataToWrite.Length} exceeds memory size ({data.Length}).");
        }

        dataToWrite.CopyTo(mem.Span.Slice((int)offset));
    }

    /// <inheritdoc />
    public void WritePhysical(DebugPrivilege privilege, Addr address, ReadOnlySpan<byte> dataToWrite)
    {
        if (address + (uint)dataToWrite.Length > data.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(address),
                $"Write at address {address} with length {dataToWrite.Length} exceeds memory size ({data.Length}).");
        }

        dataToWrite.CopyTo(mem.Span.Slice((int)address));
    }

    /// <inheritdoc />
    public void WriteBytePhysical(DebugPrivilege privilege, Addr address, byte value)
    {
        if (address >= data.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(address),
                $"Address {address} exceeds memory size ({data.Length}).");
        }

        mem.Span[(int)address] = value;
    }

    private void ValidateSliceParameters(uint offset, uint length)
    {
        if (offset + length > data.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                $"Slice (offset={offset}, length={length}) exceeds memory size ({data.Length}).");
        }
    }

    private void ValidatePageParameters(uint pageIndex, uint pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfZero(pageSize);

        uint offset = pageIndex * pageSize;
        if (offset + pageSize > data.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageIndex),
                $"Page {pageIndex} (size={pageSize}) exceeds memory size ({data.Length}).");
        }
    }
}