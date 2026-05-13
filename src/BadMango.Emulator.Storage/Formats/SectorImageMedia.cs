// <copyright file="SectorImageMedia.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// 5.25" sector-image adapter exposing both an <see cref="I525Media"/> view (via the GCR
/// 6-and-2 nibblizer + the appropriate sector skew) and an <see cref="IBlockMedia"/> view
/// (via the inverse skew over 512-byte ProDOS blocks).
/// </summary>
/// <remarks>
/// <para>
/// The backing store holds <c>TrackCount × SectorsPerTrack × BytesPerSector</c> bytes in
/// the logical sector order indicated by <see cref="DiskGeometry.SectorOrder"/>:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="SectorOrder.Dos33"/>: <c>(track, dosLogicalSector)</c> ordering.</description></item>
/// <item><description><see cref="SectorOrder.ProDos"/>: <c>(track, proDosLogicalSector)</c> ordering.</description></item>
/// </list>
/// <para>
/// The block view always presents 512-byte ProDOS blocks regardless of which order the
/// backing image uses, by computing the appropriate logical-sector positions and pairing
/// them. Track and data-field nibbles are produced on-demand by the GCR encoder.
/// </para>
/// </remarks>
public sealed class SectorImageMedia
{
    private const int BlockSize = 512;

    private readonly IStorageBackend backing;
    private readonly long backingOffset;
    private readonly bool readOnlyMount;
    private readonly DiskGeometry geometry;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectorImageMedia"/> class.
    /// </summary>
    /// <param name="backing">Underlying storage holding the sector-ordered bytes. The instance is not disposed by this adapter.</param>
    /// <param name="geometry">Geometry / sector-order of the image.</param>
    /// <param name="backingOffset">Byte offset within <paramref name="backing"/> where the payload begins (e.g. 64 for 2MG).</param>
    /// <param name="writeProtected">If <see langword="true"/>, both views report read-only regardless of <paramref name="backing"/>.<see cref="IStorageBackend.CanWrite"/>.</param>
    /// <param name="volume">Volume number to write into GCR address fields (default 254 — the DOS 3.3 default).</param>
    public SectorImageMedia(IStorageBackend backing, DiskGeometry geometry, long backingOffset = 0, bool writeProtected = false, int volume = 254)
    {
        ArgumentNullException.ThrowIfNull(backing);
        ArgumentOutOfRangeException.ThrowIfNegative(backingOffset);
        geometry.ValidatePositive();

        // SectorImageMedia bakes 16-sector / 256-byte assumptions into the GCR
        // nibblizer and the IBlockMedia view (which pairs ProDOS-logical sectors
        // 2N and 2N+1 within a track). Reject geometries that would silently
        // miscompute offsets.
        if (geometry.SectorsPerTrack != SectorSkew.SectorsPerTrack)
        {
            throw new ArgumentException(
                $"SectorImageMedia requires {SectorSkew.SectorsPerTrack} sectors per track; got {geometry.SectorsPerTrack}.",
                nameof(geometry));
        }

        if (geometry.BytesPerSector != GcrEncoder.BytesPerSector)
        {
            throw new ArgumentException(
                $"SectorImageMedia requires {GcrEncoder.BytesPerSector} bytes per sector; got {geometry.BytesPerSector}.",
                nameof(geometry));
        }

        if (backing.Length - backingOffset < geometry.TotalBytes)
        {
            throw new ArgumentException("Backing store too small for the requested geometry.", nameof(backing));
        }

        this.backing = backing;
        this.backingOffset = backingOffset;
        this.geometry = geometry;
        this.readOnlyMount = writeProtected;
        this.Volume = volume;
    }

    /// <summary>
    /// Gets the volume number written into GCR address fields by the track view.
    /// </summary>
    /// <value>The volume number, defaulting to 254.</value>
    public int Volume { get; }

    /// <summary>
    /// Gets the geometry of the underlying sector image.
    /// </summary>
    /// <value>The geometry record.</value>
    public DiskGeometry Geometry => this.geometry;

    /// <summary>
    /// Gets a value indicating whether this image is currently read-only (mount flag or backend).
    /// </summary>
    /// <value><see langword="true"/> if writes are rejected.</value>
    public bool IsReadOnly => this.readOnlyMount || !this.backing.CanWrite;

    /// <summary>
    /// Returns an <see cref="I525Media"/> view of this image.
    /// </summary>
    /// <returns>A track-addressed view that nibblises sectors on read and parses nibbles back on write.</returns>
    public I525Media As525Media() => new TrackView(this);

    /// <summary>
    /// Returns an <see cref="IBlockMedia"/> view of this image as 512-byte ProDOS blocks.
    /// </summary>
    /// <returns>A block-addressed view that reorders sectors on read/write.</returns>
    public IBlockMedia AsBlockMedia() => new BlockView(this);

    /// <summary>
    /// Flushes the underlying backend.
    /// </summary>
    public void Flush() => this.backing.Flush();

    private void ReadSectorPhysical(int track, int physicalSector, Span<byte> destination)
    {
        var logical = SectorSkew.PhysicalToLogical(this.geometry.SectorOrder, physicalSector);
        var offset = this.backingOffset + ((((long)track * this.geometry.SectorsPerTrack) + logical) * this.geometry.BytesPerSector);
        this.backing.Read(offset, destination[..this.geometry.BytesPerSector]);
    }

    private void WriteSectorPhysical(int track, int physicalSector, ReadOnlySpan<byte> source)
    {
        var logical = SectorSkew.PhysicalToLogical(this.geometry.SectorOrder, physicalSector);
        var offset = this.backingOffset + ((((long)track * this.geometry.SectorsPerTrack) + logical) * this.geometry.BytesPerSector);
        this.backing.Write(offset, source[..this.geometry.BytesPerSector]);
    }

    private void ReadSectorLogicalProDos(int track, int proDosLogical, Span<byte> destination)
    {
        // Convert to the physical sector, then to the backing-image logical sector.
        var physical = SectorSkew.LogicalToPhysical(SectorOrder.ProDos, proDosLogical);
        this.ReadSectorPhysical(track, physical, destination);
    }

    private void WriteSectorLogicalProDos(int track, int proDosLogical, ReadOnlySpan<byte> source)
    {
        var physical = SectorSkew.LogicalToPhysical(SectorOrder.ProDos, proDosLogical);
        this.WriteSectorPhysical(track, physical, source);
    }

    /// <summary>
    /// I525Media view over the parent image.
    /// </summary>
    private sealed class TrackView : I525Media
    {
        private readonly SectorImageMedia parent;

        internal TrackView(SectorImageMedia parent)
        {
            this.parent = parent;
        }

        /// <inheritdoc />
        public DiskGeometry Geometry => this.parent.geometry;

        /// <inheritdoc />
        public int OptimalTrackLength => GcrEncoder.StandardTrackLength;

        /// <inheritdoc />
        public bool IsReadOnly => this.parent.IsReadOnly;

        /// <inheritdoc />
        public void ReadTrack(int quarterTrack, Span<byte> destination)
        {
            ValidateQuarterTrack(quarterTrack, this.parent.geometry);
            if (destination.Length != GcrEncoder.StandardTrackLength)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), $"Destination must be {GcrEncoder.StandardTrackLength} bytes.");
            }

            // Quarter-tracks that aren't on a whole-track boundary return self-sync gap (no data).
            if ((quarterTrack & 0x03) != 0)
            {
                destination.Fill(GcrEncoder.GapByte);
                return;
            }

            var track = quarterTrack >> 2;
            Span<byte> trackSectors = stackalloc byte[SectorSkew.SectorsPerTrack * GcrEncoder.BytesPerSector];
            for (var phys = 0; phys < SectorSkew.SectorsPerTrack; phys++)
            {
                this.parent.ReadSectorPhysical(track, phys, trackSectors.Slice(phys * GcrEncoder.BytesPerSector, GcrEncoder.BytesPerSector));
            }

            GcrEncoder.EncodeTrack(this.parent.Volume, track, trackSectors, destination);
        }

        /// <inheritdoc />
        public void WriteTrack(int quarterTrack, ReadOnlySpan<byte> source)
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException("Media is read-only.");
            }

            ValidateQuarterTrack(quarterTrack, this.parent.geometry);
            if (source.Length != GcrEncoder.StandardTrackLength)
            {
                throw new ArgumentOutOfRangeException(nameof(source), $"Source must be {GcrEncoder.StandardTrackLength} bytes.");
            }

            // Writes to an off-axis quarter-track are a no-op — the head only writes
            // recoverable data on a whole-track boundary.
            if ((quarterTrack & 0x03) != 0)
            {
                return;
            }

            var track = quarterTrack >> 2;
            Span<byte> trackSectors = stackalloc byte[SectorSkew.SectorsPerTrack * GcrEncoder.BytesPerSector];

            // Pre-load the existing sectors so that any sectors we fail to decode are
            // not corrupted on disk.
            for (var phys = 0; phys < SectorSkew.SectorsPerTrack; phys++)
            {
                this.parent.ReadSectorPhysical(track, phys, trackSectors.Slice(phys * GcrEncoder.BytesPerSector, GcrEncoder.BytesPerSector));
            }

            var decoded = GcrEncoder.DecodeTrack(source, trackSectors);
            for (var phys = 0; phys < SectorSkew.SectorsPerTrack; phys++)
            {
                if ((decoded & (1 << phys)) != 0)
                {
                    this.parent.WriteSectorPhysical(track, phys, trackSectors.Slice(phys * GcrEncoder.BytesPerSector, GcrEncoder.BytesPerSector));
                }
            }
        }

        /// <inheritdoc />
        public void Flush() => this.parent.Flush();

        private static void ValidateQuarterTrack(int quarterTrack, DiskGeometry geometry)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(quarterTrack);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(quarterTrack, geometry.QuarterTrackCount);
        }
    }

    /// <summary>
    /// IBlockMedia view over the parent image (512-byte ProDOS blocks).
    /// </summary>
    private sealed class BlockView : IBlockMedia
    {
        private readonly SectorImageMedia parent;

        internal BlockView(SectorImageMedia parent)
        {
            this.parent = parent;
        }

        /// <inheritdoc />
        public int BlockSize => SectorImageMedia.BlockSize;

        /// <inheritdoc />
        public int BlockCount => checked((int)(this.parent.geometry.TotalBytes / SectorImageMedia.BlockSize));

        /// <inheritdoc />
        public bool IsReadOnly => this.parent.IsReadOnly;

        /// <inheritdoc />
        public void ReadBlock(int blockIndex, Span<byte> destination)
        {
            ValidateBlockArgs(blockIndex, destination.Length, this);
            this.SectorPair(blockIndex, out var track, out var lowerLogical, out var upperLogical);
            this.parent.ReadSectorLogicalProDos(track, lowerLogical, destination[..256]);
            this.parent.ReadSectorLogicalProDos(track, upperLogical, destination.Slice(256, 256));
        }

        /// <inheritdoc />
        public void WriteBlock(int blockIndex, ReadOnlySpan<byte> source)
        {
            if (this.IsReadOnly)
            {
                throw new InvalidOperationException("Media is read-only.");
            }

            ValidateBlockArgs(blockIndex, source.Length, this);
            this.SectorPair(blockIndex, out var track, out var lowerLogical, out var upperLogical);
            this.parent.WriteSectorLogicalProDos(track, lowerLogical, source[..256]);
            this.parent.WriteSectorLogicalProDos(track, upperLogical, source.Slice(256, 256));
        }

        /// <inheritdoc />
        public void Flush() => this.parent.Flush();

        private static void ValidateBlockArgs(int blockIndex, int length, BlockView self)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(blockIndex);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(blockIndex, self.BlockCount);
            if (length != self.BlockSize)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"Buffer must be {self.BlockSize} bytes.");
            }
        }

        private void SectorPair(int blockIndex, out int track, out int lowerLogical, out int upperLogical)
        {
            // ProDOS block N consists of ProDOS-logical sectors 2N (lower half) and
            // 2N+1 (upper half) within the same track. Each track holds 8 ProDOS blocks.
            track = blockIndex / 8;
            var blockInTrack = blockIndex % 8;
            lowerLogical = blockInTrack * 2;
            upperLogical = (blockInTrack * 2) + 1;
        }
    }
}