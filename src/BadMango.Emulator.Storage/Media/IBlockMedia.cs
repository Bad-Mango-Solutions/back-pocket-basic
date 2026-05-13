// <copyright file="IBlockMedia.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Media;

/// <summary>
/// Block-addressed disk media (ProDOS / SmartPort view).
/// </summary>
/// <remarks>
/// Backs both 5.25" sector images presented as ProDOS blocks (via the inverse skew)
/// and headerless block images such as <c>.hdv</c> and <c>.po</c>.
/// </remarks>
public interface IBlockMedia
{
    /// <summary>
    /// Gets the size of one block in bytes (typically 512).
    /// </summary>
    /// <value>The block size in bytes.</value>
    int BlockSize { get; }

    /// <summary>
    /// Gets the number of blocks in the volume.
    /// </summary>
    /// <value>The total number of blocks.</value>
    int BlockCount { get; }

    /// <summary>
    /// Gets a value indicating whether writes are rejected.
    /// </summary>
    /// <value><see langword="true"/> if the underlying image (or runtime mount) is write-protected.</value>
    bool IsReadOnly { get; }

    /// <summary>
    /// Reads one block.
    /// </summary>
    /// <param name="blockIndex">Zero-based block index in the range <c>[0, BlockCount)</c>.</param>
    /// <param name="destination">Destination buffer; must be exactly <see cref="BlockSize"/> bytes long.</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="blockIndex"/> is out of range or the buffer length is wrong.</exception>
    void ReadBlock(int blockIndex, Span<byte> destination);

    /// <summary>
    /// Writes one block.
    /// </summary>
    /// <param name="blockIndex">Zero-based block index in the range <c>[0, BlockCount)</c>.</param>
    /// <param name="source">Source buffer; must be exactly <see cref="BlockSize"/> bytes long.</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="blockIndex"/> is out of range or the buffer length is wrong.</exception>
    /// <exception cref="InvalidOperationException">If <see cref="IsReadOnly"/> is <see langword="true"/>.</exception>
    void WriteBlock(int blockIndex, ReadOnlySpan<byte> source);

    /// <summary>
    /// Flushes any pending writes to the underlying storage.
    /// </summary>
    void Flush();
}