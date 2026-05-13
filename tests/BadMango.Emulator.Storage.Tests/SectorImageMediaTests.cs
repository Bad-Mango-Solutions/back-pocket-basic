// <copyright file="SectorImageMediaTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Tests;

/// <summary>
/// Tests for <see cref="SectorImageMedia"/>: dual <see cref="I525Media"/> /
/// <see cref="IBlockMedia"/> views, write-protect propagation, and round-trip writes
/// through the GCR nibblizer (PRD §6.1 FR-S6, FR-S7).
/// </summary>
[TestFixture]
public class SectorImageMediaTests
{
    /// <summary>
    /// Track read → decode round-trips every sector for both DOS and ProDOS orderings.
    /// </summary>
    /// <param name="order">Backing-image sector order.</param>
    [TestCase(SectorOrder.Dos33)]
    [TestCase(SectorOrder.ProDos)]
    public void TrackRead_RoundTripsThroughGcr(SectorOrder order)
    {
        var payload = ImageFixtures.Random525Payload(seed: 12345 + (int)order);
        using var backend = new RamStorageBackend(payload);
        var geometry = new DiskGeometry(35, 16, 256, order);
        var media = new SectorImageMedia(backend, geometry).As525Media();

        for (var track = 0; track < 35; track++)
        {
            var nibbles = new byte[GcrEncoder.StandardTrackLength];
            media.ReadTrack(track * 4, nibbles);

            var decoded = new byte[16 * 256];
            var mask = GcrEncoder.DecodeTrack(nibbles, decoded);
            Assert.That(mask, Is.EqualTo(0xFFFF), $"track {track} ({order})");

            // Compare each physical sector against the backing image (after applying
            // the order's skew).
            for (var phys = 0; phys < 16; phys++)
            {
                var logical = SectorSkew.PhysicalToLogical(order, phys);
                var srcOff = ((track * 16) + logical) * 256;
                var actual = decoded.AsSpan(phys * 256, 256).ToArray();
                var expected = payload.AsSpan(srcOff, 256).ToArray();
                Assert.That(actual, Is.EqualTo(expected), $"track {track} phys {phys} ({order})");
            }
        }
    }

    /// <summary>
    /// Off-axis quarter-tracks return all gap bytes.
    /// </summary>
    [Test]
    public void TrackRead_OffAxisQuarterTrack_ReturnsGap()
    {
        var payload = ImageFixtures.Random525Payload(7);
        using var backend = new RamStorageBackend(payload);
        var media = new SectorImageMedia(backend, DiskGeometry.Standard525Dos).As525Media();

        var nibbles = new byte[GcrEncoder.StandardTrackLength];
        media.ReadTrack(quarterTrack: 1, nibbles);
        Assert.That(nibbles.All(b => b == GcrEncoder.GapByte), Is.True);
    }

    /// <summary>
    /// Encoded-then-rewritten track preserves the underlying sector data.
    /// </summary>
    [Test]
    public void TrackWrite_RoundTripPreservesSectors()
    {
        var payload = ImageFixtures.Random525Payload(99);
        using var backend = new RamStorageBackend(payload);
        var media = new SectorImageMedia(backend, DiskGeometry.Standard525Dos).As525Media();

        var nibbles = new byte[GcrEncoder.StandardTrackLength];
        media.ReadTrack(quarterTrack: 17 * 4, nibbles);

        // Now overwrite the same track with the same nibbles and confirm the backing
        // image is unchanged.
        var before = backend.ToArray();
        media.WriteTrack(17 * 4, nibbles);
        var after = backend.ToArray();
        Assert.That(after, Is.EqualTo(before));
    }

    /// <summary>
    /// Write-protect at construction time blocks writes from both views.
    /// </summary>
    [Test]
    public void WriteProtected_BlocksWritesOnBothViews()
    {
        var payload = ImageFixtures.Random525Payload(3);
        using var backend = new RamStorageBackend(payload);
        var media = new SectorImageMedia(backend, DiskGeometry.Standard525Dos, writeProtected: true);

        var trackView = media.As525Media();
        var blockView = media.AsBlockMedia();
        Assert.That(trackView.IsReadOnly, Is.True);
        Assert.That(blockView.IsReadOnly, Is.True);
        Assert.Throws<InvalidOperationException>(() => trackView.WriteTrack(0, new byte[GcrEncoder.StandardTrackLength]));
        Assert.Throws<InvalidOperationException>(() => blockView.WriteBlock(0, new byte[512]));
    }

    /// <summary>
    /// IBlockMedia view round-trips block writes for ProDOS-ordered images.
    /// </summary>
    [Test]
    public void BlockView_RoundTripsWritesProDos()
    {
        using var backend = new RamStorageBackend(ImageFixtures.FivePointTwoFiveBytes);
        var media = new SectorImageMedia(backend, DiskGeometry.Standard525ProDos);
        var blocks = media.AsBlockMedia();
        Assert.That(blocks.BlockCount, Is.EqualTo(280));
        Assert.That(blocks.BlockSize, Is.EqualTo(512));

        var write = new byte[512];
        new Random(42).NextBytes(write);
        blocks.WriteBlock(7, write);

        var read = new byte[512];
        blocks.ReadBlock(7, read);
        Assert.That(read, Is.EqualTo(write));
    }

    /// <summary>
    /// Block view over a DOS-ordered backing image still presents 512-byte ProDOS blocks
    /// — the inverse skew is applied transparently.
    /// </summary>
    [Test]
    public void BlockView_OverDosBacking_RoundTripsWrites()
    {
        using var backend = new RamStorageBackend(ImageFixtures.FivePointTwoFiveBytes);
        var media = new SectorImageMedia(backend, DiskGeometry.Standard525Dos);
        var blocks = media.AsBlockMedia();

        var write = new byte[512];
        new Random(123).NextBytes(write);
        blocks.WriteBlock(123, write);

        var read = new byte[512];
        blocks.ReadBlock(123, read);
        Assert.That(read, Is.EqualTo(write));
    }
}