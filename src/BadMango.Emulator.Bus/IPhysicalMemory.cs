// <copyright file="IPhysicalMemory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents a contiguous block of physical memory storage.
/// </summary>
/// <remarks>
/// <para>
/// Physical memory is a "dumb" byte container with no access semantics or privilege awareness.
/// It provides storage that can be sliced into views for use by bus targets like
/// <see cref="RamTarget"/> and <see cref="RomTarget"/>.
/// </para>
/// <para>
/// The ROM vs RAM distinction is not relevant at this level; that's the target's job.
/// Physical memory simply owns and manages the underlying byte storage.
/// </para>
/// <para>
/// Multiple targets can reference overlapping or non-overlapping slices of the same
/// physical memory, enabling bank switching and unified memory views for debugging,
/// save states, and DMA operations.
/// </para>
/// </remarks>
public interface IPhysicalMemory
{
    /// <summary>
    /// Gets the total size of the physical memory in bytes.
    /// </summary>
    /// <value>The size in bytes.</value>
    int Size { get; }

    /// <summary>
    /// Gets the descriptive name for this memory pool.
    /// </summary>
    /// <value>A human-readable identifier for diagnostics and debugging.</value>
    string Name { get; }

    /// <summary>
    /// Gets a writable slice of the memory.
    /// </summary>
    /// <param name="offset">The starting offset within the memory.</param>
    /// <param name="length">The length of the slice in bytes.</param>
    /// <returns>A <see cref="Memory{T}"/> representing the requested slice.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset"/> is negative,
    /// <paramref name="length"/> is negative,
    /// or <paramref name="offset"/> + <paramref name="length"/> exceeds <see cref="Size"/>.
    /// </exception>
    Memory<byte> Slice(int offset, int length);

    /// <summary>
    /// Gets a read-only slice of the memory.
    /// </summary>
    /// <param name="offset">The starting offset within the memory.</param>
    /// <param name="length">The length of the slice in bytes.</param>
    /// <returns>A <see cref="ReadOnlyMemory{T}"/> representing the requested slice.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset"/> is negative,
    /// <paramref name="length"/> is negative,
    /// or <paramref name="offset"/> + <paramref name="length"/> exceeds <see cref="Size"/>.
    /// </exception>
    ReadOnlyMemory<byte> ReadOnlySlice(int offset, int length);

    /// <summary>
    /// Gets a writable slice for a specific page.
    /// </summary>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <param name="pageSize">The size of each page in bytes. Defaults to 4096.</param>
    /// <returns>A <see cref="Memory{T}"/> representing the requested page.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageIndex"/> is negative,
    /// <paramref name="pageSize"/> is not positive,
    /// or the requested page extends beyond <see cref="Size"/>.
    /// </exception>
    Memory<byte> SlicePage(int pageIndex, int pageSize = 4096);

    /// <summary>
    /// Gets a read-only slice for a specific page.
    /// </summary>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <param name="pageSize">The size of each page in bytes. Defaults to 4096.</param>
    /// <returns>A <see cref="ReadOnlyMemory{T}"/> representing the requested page.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageIndex"/> is negative,
    /// <paramref name="pageSize"/> is not positive,
    /// or the requested page extends beyond <see cref="Size"/>.
    /// </exception>
    ReadOnlyMemory<byte> ReadOnlySlicePage(int pageIndex, int pageSize = 4096);

    /// <summary>
    /// Gets a writable span over the entire memory.
    /// </summary>
    /// <returns>A <see cref="Span{T}"/> containing the entire memory.</returns>
    Span<byte> AsSpan();

    /// <summary>
    /// Gets a read-only span over the entire memory.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}"/> containing the entire memory.</returns>
    ReadOnlySpan<byte> AsReadOnlySpan();

    /// <summary>
    /// Fills the entire memory with the specified value.
    /// </summary>
    /// <param name="value">The byte value to fill with.</param>
    void Fill(byte value);

    /// <summary>
    /// Clears the entire memory to zero.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the number of pages that fit in this memory.
    /// </summary>
    /// <param name="pageSize">The size of each page in bytes. Defaults to 4096.</param>
    /// <returns>The number of complete pages.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageSize"/> is not positive.
    /// </exception>
    int PageCount(int pageSize = 4096);
}