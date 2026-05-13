// <copyright file="RamCachedStorageBackend.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Backends;

/// <summary>
/// Block-level RAM cache wrapping another <see cref="IStorageBackend"/>.
/// </summary>
/// <remarks>
/// <para>
/// Implements PRD §6.1 FR-S1: write-through and write-back modes with a dirty-block bitmap.
/// In <see cref="StorageCacheMode.WriteThrough"/> mode, writes update both the cache and
/// the underlying backend immediately. In <see cref="StorageCacheMode.WriteBack"/> mode,
/// writes update the cache and mark blocks dirty; <see cref="Flush"/> commits dirty blocks
/// to the underlying backend.
/// </para>
/// <para>
/// On construction the entire underlying backend is read into the in-RAM cache. The block
/// size determines the granularity of the dirty-tracking bitmap.
/// </para>
/// </remarks>
public class RamCachedStorageBackend : IStorageBackend
{
    private readonly IStorageBackend inner;
    private readonly byte[] cache;
    private readonly int blockSize;
    private readonly int blockCount;
    private readonly byte[] dirtyBitmap;
    private readonly bool ownsInner;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RamCachedStorageBackend"/> class.
    /// </summary>
    /// <param name="inner">The underlying backend to cache. Must not be <see langword="null"/>.</param>
    /// <param name="mode">Caching mode (write-through or write-back).</param>
    /// <param name="blockSize">Granularity of the dirty bitmap, in bytes; must be positive. Defaults to 512.</param>
    /// <param name="ownsInner">If <see langword="true"/>, the inner backend is disposed when this instance is disposed.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="inner"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="blockSize"/> is not positive.</exception>
    public RamCachedStorageBackend(IStorageBackend inner, StorageCacheMode mode = StorageCacheMode.WriteThrough, int blockSize = 512, bool ownsInner = false)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(blockSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(inner.Length, int.MaxValue);

        this.inner = inner;
        this.Mode = mode;
        this.blockSize = blockSize;
        this.ownsInner = ownsInner;

        var length = (int)inner.Length;
        this.cache = new byte[length];
        if (length > 0)
        {
            inner.Read(0, this.cache);
        }

        this.blockCount = (length + blockSize - 1) / blockSize;
        this.dirtyBitmap = new byte[(this.blockCount + 7) / 8];
    }

    /// <summary>
    /// Gets the caching mode in effect.
    /// </summary>
    /// <value>The caching mode set at construction time.</value>
    public StorageCacheMode Mode { get; }

    /// <inheritdoc />
    public long Length => this.cache.Length;

    /// <inheritdoc />
    public bool CanWrite => this.inner.CanWrite;

    /// <summary>
    /// Gets the dirty-block granularity in bytes.
    /// </summary>
    /// <value>The block size used for the dirty bitmap.</value>
    public int BlockSize => this.blockSize;

    /// <summary>
    /// Gets the number of blocks tracked by the dirty bitmap.
    /// </summary>
    /// <value>The block count, equal to <c>ceil(Length / BlockSize)</c>.</value>
    public int BlockCount => this.blockCount;

    /// <inheritdoc />
    public void Read(long offset, Span<byte> destination)
    {
        this.ThrowIfDisposed();
        ValidateRange(offset, destination.Length, this.cache.Length);
        this.cache.AsSpan((int)offset, destination.Length).CopyTo(destination);
    }

    /// <inheritdoc />
    public void Write(long offset, ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();
        if (!this.inner.CanWrite)
        {
            throw new InvalidOperationException("Backend is read-only.");
        }

        ValidateRange(offset, source.Length, this.cache.Length);
        source.CopyTo(this.cache.AsSpan((int)offset, source.Length));

        if (this.Mode == StorageCacheMode.WriteThrough)
        {
            this.inner.Write(offset, source);
        }
        else
        {
            this.MarkDirty(offset, source.Length);
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        this.ThrowIfDisposed();

        if (this.Mode == StorageCacheMode.WriteBack && this.inner.CanWrite)
        {
            for (var block = 0; block < this.blockCount; block++)
            {
                if (this.IsDirty(block))
                {
                    var offset = (long)block * this.blockSize;
                    var length = (int)Math.Min(this.blockSize, this.cache.Length - offset);
                    this.inner.Write(offset, this.cache.AsSpan((int)offset, length));
                    this.ClearDirty(block);
                }
            }
        }

        this.inner.Flush();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!this.disposed)
        {
            try
            {
                this.Flush();
            }
            catch
            {
                // Suppress flush errors during disposal; callers wanting durability
                // must call Flush() explicitly first.
            }

            if (this.ownsInner)
            {
                this.inner.Dispose();
            }

            this.disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Indicates whether the specified block is marked dirty.
    /// </summary>
    /// <param name="blockIndex">Zero-based block index.</param>
    /// <returns><see langword="true"/> if the block is dirty; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="blockIndex"/> is out of range.</exception>
    public bool IsDirty(int blockIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(blockIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(blockIndex, this.blockCount);
        return (this.dirtyBitmap[blockIndex >> 3] & (1 << (blockIndex & 7))) != 0;
    }

    /// <summary>
    /// Returns the number of dirty blocks currently tracked.
    /// </summary>
    /// <returns>The dirty block count.</returns>
    public int DirtyBlockCount()
    {
        var count = 0;
        for (var i = 0; i < this.blockCount; i++)
        {
            if (this.IsDirty(i))
            {
                count++;
            }
        }

        return count;
    }

    private static void ValidateRange(long offset, int length, long bufferLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (offset + length > bufferLength)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Range extends past end of backing store.");
        }
    }

    private void MarkDirty(long offset, int length)
    {
        if (length == 0)
        {
            return;
        }

        var firstBlock = (int)(offset / this.blockSize);
        var lastBlock = (int)((offset + length - 1) / this.blockSize);
        for (var b = firstBlock; b <= lastBlock; b++)
        {
            this.dirtyBitmap[b >> 3] |= (byte)(1 << (b & 7));
        }
    }

    private void ClearDirty(int blockIndex)
    {
        this.dirtyBitmap[blockIndex >> 3] &= (byte)~(1 << (blockIndex & 7));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
    }
}