// <copyright file="I525Media.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Media;

/// <summary>
/// Track-addressed 5.25" disk media (Disk II view).
/// </summary>
/// <remarks>
/// Quarter-track addressing covers 0…139 (4 positions per of 35 tracks). Implementations
/// over sector-image backings translate quarter-tracks to whole tracks and synthesize the
/// raw nibble stream via a GCR 6-and-2 nibblizer; nibble-image backings (<c>.nib</c>,
/// <c>.woz</c>) return cached nibbles directly.
/// </remarks>
public interface I525Media
{
    /// <summary>
    /// Gets the geometry of this medium.
    /// </summary>
    /// <value>The geometry record describing track / sector counts and ordering.</value>
    DiskGeometry Geometry { get; }

    /// <summary>
    /// Gets the optimal nibble-stream length per track for this medium.
    /// </summary>
    /// <value>For sector-backed images this is typically 6656 (the standard <c>.nib</c> track length).</value>
    int OptimalTrackLength { get; }

    /// <summary>
    /// Gets a value indicating whether writes are rejected.
    /// </summary>
    /// <value><see langword="true"/> if the underlying image (or runtime mount) is write-protected.</value>
    bool IsReadOnly { get; }

    /// <summary>
    /// Reads a track's raw nibble stream.
    /// </summary>
    /// <param name="quarterTrack">Quarter-track index in the range <c>[0, Geometry.QuarterTrackCount)</c>.</param>
    /// <param name="destination">Destination buffer; must be exactly <see cref="OptimalTrackLength"/> bytes long.</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="quarterTrack"/> is out of range or the buffer length is wrong.</exception>
    void ReadTrack(int quarterTrack, Span<byte> destination);

    /// <summary>
    /// Writes a track's raw nibble stream.
    /// </summary>
    /// <param name="quarterTrack">Quarter-track index in the range <c>[0, Geometry.QuarterTrackCount)</c>.</param>
    /// <param name="source">Source buffer; must be exactly <see cref="OptimalTrackLength"/> bytes long.</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="quarterTrack"/> is out of range or the buffer length is wrong.</exception>
    /// <exception cref="InvalidOperationException">If <see cref="IsReadOnly"/> is <see langword="true"/>.</exception>
    void WriteTrack(int quarterTrack, ReadOnlySpan<byte> source);

    /// <summary>
    /// Flushes any pending writes to the underlying storage.
    /// </summary>
    void Flush();
}