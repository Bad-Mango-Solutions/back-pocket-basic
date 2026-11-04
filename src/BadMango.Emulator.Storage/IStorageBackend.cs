// <copyright file="IStorageBackend.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage;

/// <summary>
/// Byte-level random-access storage abstraction underlying every disk image backing.
/// </summary>
/// <remarks>
/// Implementations are mockable (no sealed types on the seam, no static singletons) per
/// PRD §7. All offsets are in bytes from the start of the backing store. Reads and writes
/// are required to be in-range; out-of-range access throws
/// <see cref="ArgumentOutOfRangeException"/>.
/// </remarks>
public interface IStorageBackend : IDisposable
{
    /// <summary>
    /// Gets the total length of the backing store in bytes.
    /// </summary>
    /// <value>The non-negative byte length of the backing store.</value>
    long Length { get; }

    /// <summary>
    /// Gets a value indicating whether this backend accepts writes.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="Write"/> may be called; otherwise <see langword="false"/>.</value>
    bool CanWrite { get; }

    /// <summary>
    /// Reads bytes from the backing store at the specified offset.
    /// </summary>
    /// <param name="offset">Byte offset from the start of the backing store.</param>
    /// <param name="destination">Destination buffer; exactly <c>destination.Length</c> bytes are read.</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> is negative or the requested range falls outside the backing store.</exception>
    void Read(long offset, Span<byte> destination);

    /// <summary>
    /// Writes bytes to the backing store at the specified offset.
    /// </summary>
    /// <param name="offset">Byte offset from the start of the backing store.</param>
    /// <param name="source">Source buffer; exactly <c>source.Length</c> bytes are written.</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> is negative or the requested range falls outside the backing store.</exception>
    /// <exception cref="InvalidOperationException">If <see cref="CanWrite"/> is <see langword="false"/>.</exception>
    void Write(long offset, ReadOnlySpan<byte> source);

    /// <summary>
    /// Flushes any buffered or cached writes to the underlying durable medium.
    /// </summary>
    /// <remarks>
    /// For purely in-memory implementations this is a no-op. For file-backed and
    /// write-back cached implementations this commits pending data.
    /// </remarks>
    void Flush();
}