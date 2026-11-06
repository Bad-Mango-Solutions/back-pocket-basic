// <copyright file="INibbleStreamMedia.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Defines nibble-stream operations for storage media.
/// </summary>
public interface INibbleStreamMedia : IStorageMedia
{
    /// <summary>
    /// Attempts to read raw nibble data for a track.
    /// </summary>
    /// <param name="trackIndex">The zero-based track index to read.</param>
    /// <param name="buffer">The destination buffer for nibble data.</param>
    /// <returns><see langword="true"/> if nibble data was read; otherwise, <see langword="false"/>.</returns>
    bool TryReadNibbles(int trackIndex, Span<byte> buffer);

    /// <summary>
    /// Attempts to write raw nibble data for a track.
    /// </summary>
    /// <param name="trackIndex">The zero-based track index to write.</param>
    /// <param name="buffer">The source buffer for nibble data.</param>
    /// <returns><see langword="true"/> if nibble data was written; otherwise, <see langword="false"/>.</returns>
    bool TryWriteNibbles(int trackIndex, ReadOnlySpan<byte> buffer);
}