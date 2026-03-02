// <copyright file="IStorageMedia.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

using BadMango.Emulator.Devices.Storage;

/// <summary>
/// Host-side abstraction for disk media used by storage devices.
/// </summary>
/// <remarks>
/// <para>
/// This API follows the repository disk abstractions and is format-neutral for block access while
/// allowing optional track/nibble semantics for classic Apple II media encodings.
/// </para>
/// <para>
/// Reference specifications:
/// <see href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/os/Disk%20Image%20Abstraction%20API%20Spec.md">Disk Image Abstraction API Spec</see>,
/// <see href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/os/Unified%20Block%20Device%20Backing%20API%20for%20Apple%20II%20Emulator.md">Unified Block Device Backing API for Apple II Emulator</see>.
/// </para>
/// <para>
/// Related open-source references include AppleWin, MAME, OpenEmulator, KEGS, CiderPress2, and DiskM8.
/// </para>
/// </remarks>
public interface IStorageMedia
{
    /// <summary>
    /// Raised when media content or host-observable media state changes.
    /// </summary>
    event EventHandler? MediaChanged;

    /// <summary>
    /// Gets the media block count.
    /// </summary>
    int BlockCount { get; }

    /// <summary>
    /// Gets the block size in bytes.
    /// </summary>
    int BlockSize { get; }

    /// <summary>
    /// Gets a value indicating whether writes are disallowed.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether optional track-level access is supported.
    /// </summary>
    bool SupportsTrackAccess { get; }

    /// <summary>
    /// Gets a value indicating whether optional nibble-level access is supported.
    /// </summary>
    bool SupportsNibbleAccess { get; }

    /// <summary>
    /// Gets the media format identifier.
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Gets format-specific metadata for host inspection and tooling.
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets strongly-typed metrics for host observability dashboards.
    /// </summary>
    MediaMetrics Metrics { get; }

    /// <summary>
    /// Gets a serializable metrics representation.
    /// </summary>
    /// <returns>A dictionary representation of <see cref="Metrics"/>.</returns>
    Dictionary<string, object> GetMetricsDictionary();

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