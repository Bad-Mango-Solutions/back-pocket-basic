// <copyright file="IBlockMedia.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Defines block-addressable operations for storage media.
/// </summary>
public interface IBlockMedia : IStorageMedia
{
    /// <summary>
    /// Gets the media block count.
    /// </summary>
    int BlockCount { get; }

    /// <summary>
    /// Gets the block size in bytes.
    /// </summary>
    int BlockSize { get; }

    /// <summary>
    /// Reads a logical block into the provided buffer.
    /// </summary>
    /// <param name="blockIndex">The zero-based block index to read.</param>
    /// <param name="buffer">The destination buffer that receives block data.</param>
    void ReadBlock(int blockIndex, Span<byte> buffer);

    /// <summary>
    /// Writes a logical block from the provided buffer.
    /// </summary>
    /// <param name="blockIndex">The zero-based block index to write.</param>
    /// <param name="buffer">The source buffer containing block data to write.</param>
    void WriteBlock(int blockIndex, ReadOnlySpan<byte> buffer);
}