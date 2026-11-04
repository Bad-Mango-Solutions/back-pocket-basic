// <copyright file="RamStorageBackend.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Backends;

/// <summary>
/// In-memory <see cref="IStorageBackend"/> backed by a managed byte array.
/// </summary>
/// <remarks>
/// Useful for unit tests and for staging an image entirely in RAM. The buffer is owned
/// by this instance; callers may obtain a snapshot via <see cref="ToArray"/>.
/// </remarks>
public class RamStorageBackend : IStorageBackend
{
    private readonly byte[] buffer;
    private readonly bool canWrite;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RamStorageBackend"/> class with a zero-filled buffer.
    /// </summary>
    /// <param name="length">Length of the backing buffer in bytes; must be non-negative.</param>
    /// <param name="canWrite">Whether the backend accepts writes. Defaults to <see langword="true"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="length"/> is negative.</exception>
    public RamStorageBackend(long length, bool canWrite = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, int.MaxValue);
        this.buffer = new byte[length];
        this.canWrite = canWrite;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RamStorageBackend"/> class wrapping an existing buffer.
    /// </summary>
    /// <param name="initialContents">Initial contents; copied into a private buffer.</param>
    /// <param name="canWrite">Whether the backend accepts writes. Defaults to <see langword="true"/>.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="initialContents"/> is <see langword="null"/>.</exception>
    public RamStorageBackend(byte[] initialContents, bool canWrite = true)
    {
        ArgumentNullException.ThrowIfNull(initialContents);
        this.buffer = (byte[])initialContents.Clone();
        this.canWrite = canWrite;
    }

    /// <inheritdoc />
    public long Length => this.buffer.Length;

    /// <inheritdoc />
    public bool CanWrite => this.canWrite;

    /// <inheritdoc />
    public void Read(long offset, Span<byte> destination)
    {
        this.ThrowIfDisposed();
        ValidateRange(offset, destination.Length, this.buffer.Length);
        this.buffer.AsSpan((int)offset, destination.Length).CopyTo(destination);
    }

    /// <inheritdoc />
    public void Write(long offset, ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();
        if (!this.canWrite)
        {
            throw new InvalidOperationException("Backend is read-only.");
        }

        ValidateRange(offset, source.Length, this.buffer.Length);
        source.CopyTo(this.buffer.AsSpan((int)offset, source.Length));
    }

    /// <inheritdoc />
    public void Flush()
    {
        this.ThrowIfDisposed();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns a snapshot of the current contents.
    /// </summary>
    /// <returns>A newly allocated copy of the underlying buffer.</returns>
    public byte[] ToArray() => (byte[])this.buffer.Clone();

    private static void ValidateRange(long offset, int length, long bufferLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (offset + length > bufferLength)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Range extends past end of backing store.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
    }
}