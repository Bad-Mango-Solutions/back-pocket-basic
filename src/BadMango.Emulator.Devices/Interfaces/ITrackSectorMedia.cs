// <copyright file="ITrackSectorMedia.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Defines track-stream operations for storage media.
/// </summary>
public interface ITrackSectorMedia : IStorageMedia
{
    /// <summary>
    /// Attempts to read a track image into the provided buffer.
    /// </summary>
    /// <param name="trackIndex">The zero-based track index to read.</param>
    /// <param name="buffer">The destination buffer for track data.</param>
    /// <returns><see langword="true"/> if track data was read; otherwise, <see langword="false"/>.</returns>
    bool TryReadTrack(int trackIndex, Span<byte> buffer);

    /// <summary>
    /// Attempts to write a track image from the provided buffer.
    /// </summary>
    /// <param name="trackIndex">The zero-based track index to write.</param>
    /// <param name="buffer">The source buffer for track data.</param>
    /// <returns><see langword="true"/> if track data was written; otherwise, <see langword="false"/>.</returns>
    bool TryWriteTrack(int trackIndex, ReadOnlySpan<byte> buffer);
}