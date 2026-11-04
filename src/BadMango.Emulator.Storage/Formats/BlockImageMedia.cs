// <copyright file="BlockImageMedia.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using BadMango.Emulator.Storage.Media;

/// <summary>
/// Headerless raw block-image adapter (e.g. <c>.hdv</c>): a contiguous run of equal-size
/// blocks at a fixed offset within the backing storage.
/// </summary>
public sealed class BlockImageMedia : IBlockMedia
{
    private readonly IStorageBackend backing;
    private readonly long backingOffset;
    private readonly bool readOnlyMount;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockImageMedia"/> class.
    /// </summary>
    /// <param name="backing">Backing storage holding <c>blockCount × blockSize</c> bytes at <paramref name="backingOffset"/>.</param>
    /// <param name="blockCount">Number of blocks in the image; must be positive.</param>
    /// <param name="blockSize">Block size in bytes; defaults to 512.</param>
    /// <param name="backingOffset">Byte offset within <paramref name="backing"/> where the payload begins.</param>
    /// <param name="writeProtected">If <see langword="true"/>, writes are rejected regardless of <paramref name="backing"/>.<see cref="IStorageBackend.CanWrite"/>.</param>
    public BlockImageMedia(IStorageBackend backing, int blockCount, int blockSize = 512, long backingOffset = 0, bool writeProtected = false)
    {
        ArgumentNullException.ThrowIfNull(backing);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(blockCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(blockSize);
        ArgumentOutOfRangeException.ThrowIfNegative(backingOffset);
        var required = (long)blockCount * blockSize;
        if (backing.Length - backingOffset < required)
        {
            throw new ArgumentException("Backing store too small for the requested block count.", nameof(backing));
        }

        this.backing = backing;
        this.BlockCount = blockCount;
        this.BlockSize = blockSize;
        this.backingOffset = backingOffset;
        this.readOnlyMount = writeProtected;
    }

    /// <inheritdoc />
    public int BlockSize { get; }

    /// <inheritdoc />
    public int BlockCount { get; }

    /// <inheritdoc />
    public bool IsReadOnly => this.readOnlyMount || !this.backing.CanWrite;

    /// <inheritdoc />
    public void ReadBlock(int blockIndex, Span<byte> destination)
    {
        this.ValidateArgs(blockIndex, destination.Length);
        this.backing.Read(this.backingOffset + ((long)blockIndex * this.BlockSize), destination);
    }

    /// <inheritdoc />
    public void WriteBlock(int blockIndex, ReadOnlySpan<byte> source)
    {
        if (this.IsReadOnly)
        {
            throw new InvalidOperationException("Media is read-only.");
        }

        this.ValidateArgs(blockIndex, source.Length);
        this.backing.Write(this.backingOffset + ((long)blockIndex * this.BlockSize), source);
    }

    /// <inheritdoc />
    public void Flush() => this.backing.Flush();

    private void ValidateArgs(int blockIndex, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(blockIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(blockIndex, this.BlockCount);
        if (length != this.BlockSize)
        {
            throw new ArgumentOutOfRangeException(nameof(length), $"Buffer must be {this.BlockSize} bytes.");
        }
    }
}