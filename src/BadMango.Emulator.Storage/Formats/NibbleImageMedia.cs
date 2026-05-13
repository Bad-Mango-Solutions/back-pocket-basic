// <copyright file="NibbleImageMedia.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// 5.25" nibble-image adapter (e.g. <c>.nib</c>): one 6656-byte nibble stream per whole track.
/// </summary>
/// <remarks>
/// Nibble images do not carry a logical sector order — the nibble stream is consumed
/// directly. The <see cref="DiskGeometry.SectorOrder"/> field is informational only and
/// is reported as <see cref="SectorOrder.Dos33"/> by default.
/// </remarks>
public sealed class NibbleImageMedia : I525Media
{
    private readonly IStorageBackend backing;
    private readonly long backingOffset;
    private readonly bool readOnlyMount;

    /// <summary>
    /// Initializes a new instance of the <see cref="NibbleImageMedia"/> class.
    /// </summary>
    /// <param name="backing">Backing storage; payload is <c>trackCount × 6656</c> bytes starting at <paramref name="backingOffset"/>.</param>
    /// <param name="trackCount">Number of whole tracks in the image (typically 35).</param>
    /// <param name="backingOffset">Byte offset within <paramref name="backing"/> where the payload begins.</param>
    /// <param name="writeProtected">If <see langword="true"/>, writes are rejected regardless of <paramref name="backing"/>.<see cref="IStorageBackend.CanWrite"/>.</param>
    public NibbleImageMedia(IStorageBackend backing, int trackCount = 35, long backingOffset = 0, bool writeProtected = false)
    {
        ArgumentNullException.ThrowIfNull(backing);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(trackCount);
        ArgumentOutOfRangeException.ThrowIfNegative(backingOffset);
        var required = (long)trackCount * GcrEncoder.StandardTrackLength;
        if (backing.Length - backingOffset < required)
        {
            throw new ArgumentException("Backing store too small for the requested track count.", nameof(backing));
        }

        this.backing = backing;
        this.backingOffset = backingOffset;
        this.readOnlyMount = writeProtected;
        this.Geometry = new DiskGeometry(trackCount, SectorSkew.SectorsPerTrack, GcrEncoder.BytesPerSector, SectorOrder.Dos33);
    }

    /// <inheritdoc />
    public DiskGeometry Geometry { get; }

    /// <inheritdoc />
    public int OptimalTrackLength => GcrEncoder.StandardTrackLength;

    /// <inheritdoc />
    public bool IsReadOnly => this.readOnlyMount || !this.backing.CanWrite;

    /// <inheritdoc />
    public void ReadTrack(int quarterTrack, Span<byte> destination)
    {
        this.ValidateQuarterTrack(quarterTrack);
        if (destination.Length != GcrEncoder.StandardTrackLength)
        {
            throw new ArgumentOutOfRangeException(nameof(destination), $"Destination must be {GcrEncoder.StandardTrackLength} bytes.");
        }

        if ((quarterTrack & 0x03) != 0)
        {
            destination.Fill(GcrEncoder.GapByte);
            return;
        }

        var track = quarterTrack >> 2;
        this.backing.Read(this.backingOffset + ((long)track * GcrEncoder.StandardTrackLength), destination);
    }

    /// <inheritdoc />
    public void WriteTrack(int quarterTrack, ReadOnlySpan<byte> source)
    {
        if (this.IsReadOnly)
        {
            throw new InvalidOperationException("Media is read-only.");
        }

        this.ValidateQuarterTrack(quarterTrack);
        if (source.Length != GcrEncoder.StandardTrackLength)
        {
            throw new ArgumentOutOfRangeException(nameof(source), $"Source must be {GcrEncoder.StandardTrackLength} bytes.");
        }

        if ((quarterTrack & 0x03) != 0)
        {
            return;
        }

        var track = quarterTrack >> 2;
        this.backing.Write(this.backingOffset + ((long)track * GcrEncoder.StandardTrackLength), source);
    }

    /// <inheritdoc />
    public void Flush() => this.backing.Flush();

    private void ValidateQuarterTrack(int quarterTrack)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quarterTrack);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(quarterTrack, this.Geometry.QuarterTrackCount);
    }
}