// <copyright file="PhysicalMemory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalMemory"/> class with the specified size.
    /// </summary>
    /// <param name="size">The size of the memory in bytes.</param>
    /// <param name="name">A descriptive name for this memory pool.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when size is less than or equal to zero.</exception>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    /// <exception cref="ArgumentException">Thrown when name is empty or whitespace.</exception>
    /// <remarks>The memory is zero-initialized.</remarks>
    public PhysicalMemory(int size, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        data = new byte[size];
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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialData.Length);

        data = initialData.ToArray();
        Name = name;
    }

    /// <inheritdoc />
    public int Size => data.Length;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Memory<byte> Slice(int offset, int length)
    {
        ValidateSliceParameters(offset, length);
        return data.AsMemory(offset, length);
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> ReadOnlySlice(int offset, int length)
    {
        ValidateSliceParameters(offset, length);
        return data.AsMemory(offset, length);
    }

    /// <inheritdoc />
    public Memory<byte> SlicePage(int pageIndex, int pageSize = 4096)
    {
        ValidatePageParameters(pageIndex, pageSize);
        int offset = pageIndex * pageSize;
        return data.AsMemory(offset, pageSize);
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> ReadOnlySlicePage(int pageIndex, int pageSize = 4096)
    {
        ValidatePageParameters(pageIndex, pageSize);
        int offset = pageIndex * pageSize;
        return data.AsMemory(offset, pageSize);
    }

    /// <inheritdoc />
    public Span<byte> AsSpan() => data.AsSpan();

    /// <inheritdoc />
    public ReadOnlySpan<byte> AsReadOnlySpan() => data.AsSpan();

    /// <inheritdoc />
    public void Fill(byte value) => Array.Fill(data, value);

    /// <inheritdoc />
    public void Clear() => Array.Clear(data);

    /// <inheritdoc />
    public int PageCount(int pageSize = 4096)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        return data.Length / pageSize;
    }

    private void ValidateSliceParameters(int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (offset + length > data.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                $"Slice (offset={offset}, length={length}) exceeds memory size ({data.Length}).");
        }
    }

    private void ValidatePageParameters(int pageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        int offset = pageIndex * pageSize;
        if (offset + pageSize > data.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageIndex),
                $"Page {pageIndex} (size={pageSize}) exceeds memory size ({data.Length}).");
        }
    }
}