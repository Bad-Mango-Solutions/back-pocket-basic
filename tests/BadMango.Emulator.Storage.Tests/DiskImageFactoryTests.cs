// <copyright file="DiskImageFactoryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Tests;

using System.Buffers.Binary;

using Moq;

/// <summary>
/// Tests for <see cref="DiskImageFactory.Open"/> across every supported sector / block
/// format, including the <c>.dsk</c> ordering sniffer (PRD §6.1 FR-S4 / FR-S5 / §10.5).
/// </summary>
[TestFixture]
public class DiskImageFactoryTests
{
    private readonly List<string> tempPaths = [];

    /// <summary>
    /// Cleans up temp files created by the test fixture.
    /// </summary>
    [TearDown]
    public void Cleanup()
    {
        foreach (var p in this.tempPaths)
        {
            try
            {
                File.Delete(p);
            }
            catch
            {
                // best-effort cleanup
            }
        }

        this.tempPaths.Clear();
    }

    /// <summary>
    /// Round-trips a <c>.do</c> image through the factory and the block view.
    /// </summary>
    [Test]
    public void Open_DoFile_ProducesDualViewWithDosOrder()
    {
        var payload = ImageFixtures.Random525Payload(1);
        var path = this.Temp(payload, ".do");

        var factory = new DiskImageFactory();
        var result = factory.Open(path);

        var dual = (Image525AndBlockResult)result;
        Assert.That(dual.Format, Is.EqualTo(DiskImageFormat.Dos33SectorImage));
        Assert.That(dual.SectorOrder, Is.EqualTo(SectorOrder.Dos33));
        Assert.That(dual.WasOrderSniffed, Is.False);
        Assert.That(dual.IsReadOnly, Is.False);

        var block = new byte[512];
        dual.BlockMedia.ReadBlock(0, block);
    }

    /// <summary>
    /// Round-trips a <c>.po</c> image through the factory.
    /// </summary>
    [Test]
    public void Open_PoFile_ProducesDualViewWithProDosOrder()
    {
        var payload = ImageFixtures.Random525Payload(2);
        var path = this.Temp(payload, ".po");

        var factory = new DiskImageFactory();
        var result = (Image525AndBlockResult)factory.Open(path);

        Assert.That(result.Format, Is.EqualTo(DiskImageFormat.ProDosSectorImage));
        Assert.That(result.SectorOrder, Is.EqualTo(SectorOrder.ProDos));
        Assert.That(result.WasOrderSniffed, Is.False);
    }

    /// <summary>
    /// A <c>.dsk</c> with a DOS 3.3 VTOC sniffs as <see cref="SectorOrder.Dos33"/>.
    /// </summary>
    [Test]
    public void Open_DskWithDosVtoc_SniffsAsDos()
    {
        var payload = ImageFixtures.Random525Payload(3);
        ImageFixtures.WriteDosVtoc(payload);
        var path = this.Temp(payload, ".dsk");

        var factory = new DiskImageFactory();
        var result = (Image525AndBlockResult)factory.Open(path);
        Assert.That(result.SectorOrder, Is.EqualTo(SectorOrder.Dos33));
        Assert.That(result.WasOrderSniffed, Is.True);
        Assert.That(result.Format, Is.EqualTo(DiskImageFormat.Dos33SectorImage));
    }

    /// <summary>
    /// A <c>.dsk</c> whose ProDOS-ordered block 2 holds a volume-directory key block
    /// sniffs as <see cref="SectorOrder.ProDos"/>.
    /// </summary>
    [Test]
    public void Open_DskWithProDosRoot_SniffsAsProDos()
    {
        var payload = new byte[ImageFixtures.FivePointTwoFiveBytes];
        ImageFixtures.WriteProDosRootDirectory(payload);
        var path = this.Temp(payload, ".dsk");

        var factory = new DiskImageFactory();
        var result = (Image525AndBlockResult)factory.Open(path);
        Assert.That(result.SectorOrder, Is.EqualTo(SectorOrder.ProDos));
        Assert.That(result.WasOrderSniffed, Is.True);
        Assert.That(result.Format, Is.EqualTo(DiskImageFormat.ProDosSectorImage));
    }

    /// <summary>
    /// An ambiguous, signature-free <c>.dsk</c> falls back to <see cref="SectorOrder.Dos33"/>.
    /// </summary>
    [Test]
    public void Open_DskWithNoSignature_FallsBackToDos()
    {
        var payload = new byte[ImageFixtures.FivePointTwoFiveBytes];
        var path = this.Temp(payload, ".dsk");

        var factory = new DiskImageFactory();
        var result = (Image525AndBlockResult)factory.Open(path);
        Assert.That(result.SectorOrder, Is.EqualTo(SectorOrder.Dos33));
        Assert.That(result.WasOrderSniffed, Is.False);
    }

    /// <summary>
    /// A <c>.nib</c> image opens as a 5.25"-only result.
    /// </summary>
    [Test]
    public void Open_NibFile_ProducesTrackOnlyResult()
    {
        var payload = new byte[35 * GcrEncoder.StandardTrackLength];
        var path = this.Temp(payload, ".nib");

        var factory = new DiskImageFactory();
        var result = factory.Open(path);
        var track = (Image525Result)result;
        Assert.That(track.Format, Is.EqualTo(DiskImageFormat.NibbleImage));
        Assert.That(track.Media.OptimalTrackLength, Is.EqualTo(GcrEncoder.StandardTrackLength));
    }

    /// <summary>
    /// A <c>.hdv</c> image opens as a block-only result.
    /// </summary>
    [Test]
    public void Open_HdvFile_ProducesBlockOnlyResult()
    {
        var payload = new byte[1024 * 512]; // 512 KB
        var path = this.Temp(payload, ".hdv");

        var factory = new DiskImageFactory();
        var result = factory.Open(path);
        var block = (ImageBlockResult)result;
        Assert.That(block.Format, Is.EqualTo(DiskImageFormat.HdvBlockImage));
        Assert.That(block.Media.BlockSize, Is.EqualTo(512));
        Assert.That(block.Media.BlockCount, Is.EqualTo(1024));
    }

    /// <summary>
    /// A <c>.d13</c> image is recognised and refused with a clear error.
    /// </summary>
    [Test]
    public void Open_D13File_Throws()
    {
        var payload = new byte[35 * 13 * 256];
        var path = this.Temp(payload, ".d13");
        var factory = new DiskImageFactory();
        var ex = Assert.Throws<NotSupportedException>(() => factory.Open(path));
        Assert.That(ex!.Message, Does.Contain(".d13").Or.Contain("13-sector"));
    }

    /// <summary>
    /// A <c>.woz</c> path is rejected as out-of-scope here.
    /// </summary>
    [Test]
    public void Open_WozFile_Throws()
    {
        var path = this.Temp(new byte[] { (byte)'W', (byte)'O', (byte)'Z', (byte)'1' }, ".woz");
        var factory = new DiskImageFactory();
        Assert.Throws<NotSupportedException>(() => factory.Open(path));
    }

    /// <summary>
    /// A 2MG image with a DOS payload opens with both views and honours the
    /// write-protect flag bit.
    /// </summary>
    [Test]
    public void Open_TwoImgDosWithWriteProtect_OpensReadOnly()
    {
        var payload = ImageFixtures.Random525Payload(8);
        var image = Build2Mg(payload, format: 0, flags: 0x80000000u);
        var path = this.Temp(image, ".2mg");

        var factory = new DiskImageFactory();
        var result = (Image525AndBlockResult)factory.Open(path);
        Assert.That(result.Format, Is.EqualTo(DiskImageFormat.TwoImgDos));
        Assert.That(result.IsReadOnly, Is.True, "2MG header bit 31 must produce a read-only mount.");
        Assert.Throws<InvalidOperationException>(() => result.BlockMedia.WriteBlock(0, new byte[512]));
    }

    /// <summary>
    /// A 2MG image with a ProDOS payload opens with the ProDOS sector order.
    /// </summary>
    [Test]
    public void Open_TwoImgProDos_OpensInProDosOrder()
    {
        var payload = ImageFixtures.Random525Payload(9);
        var image = Build2Mg(payload, format: 1, flags: 0u);
        var path = this.Temp(image, ".2img");

        var factory = new DiskImageFactory();
        var result = (Image525AndBlockResult)factory.Open(path);
        Assert.That(result.Format, Is.EqualTo(DiskImageFormat.TwoImgProDos));
        Assert.That(result.SectorOrder, Is.EqualTo(SectorOrder.ProDos));
    }

    /// <summary>
    /// A 2MG image with a nibble payload opens as a track-only result.
    /// </summary>
    [Test]
    public void Open_TwoImgNibble_OpensAsTrackOnly()
    {
        var payload = new byte[35 * GcrEncoder.StandardTrackLength];
        var image = Build2Mg(payload, format: 2, flags: 0u);
        var path = this.Temp(image, ".2mg");

        var factory = new DiskImageFactory();
        var result = (Image525Result)factory.Open(path);
        Assert.That(result.Format, Is.EqualTo(DiskImageFormat.TwoImgNibble));
    }

    /// <summary>
    /// A 2MG image with a ProDOS payload larger than the 5.25" standard (e.g. an 800K
    /// 3.5" disk) must open as a pure block image, not as a multi-hundred-track 5.25"
    /// sector image. Regression test for the bug where 'disk info' on a 3.5" ProDOS
    /// 2MG reported "5.25" sector image (track + block views)".
    /// </summary>
    [Test]
    public void Open_TwoImgProDos3Point5_OpensAsBlockImage()
    {
        var payload = new byte[1600 * 512]; // 800K (3.5" ProDOS volume)
        var image = Build2Mg(payload, format: 1, flags: 0u);
        var path = this.Temp(image, ".2mg");

        var factory = new DiskImageFactory();
        var result = factory.Open(path);
        Assert.That(result, Is.InstanceOf<ImageBlockResult>(), "3.5\"-sized ProDOS 2MG must not expose a 5.25\" track view.");
        var block = (ImageBlockResult)result;
        Assert.Multiple(() =>
        {
            Assert.That(block.Format, Is.EqualTo(DiskImageFormat.TwoImgProDos));
            Assert.That(block.Media.BlockCount, Is.EqualTo(1600));
            Assert.That(block.Media.BlockSize, Is.EqualTo(512));
        });
    }

    /// <summary>
    /// 2MG format code 0 (DOS 3.3) is only valid at the 5.25" 35-track payload size.
    /// Any larger payload is rejected with a clear error.
    /// </summary>
    [Test]
    public void Open_TwoImgDos_NonStandardSize_ThrowsInvalidData()
    {
        var payload = new byte[1600 * 512];
        var image = Build2Mg(payload, format: 0, flags: 0u);
        var path = this.Temp(image, ".2mg");

        var factory = new DiskImageFactory();
        Assert.Throws<InvalidDataException>(() => factory.Open(path));
    }

    /// <summary>
    /// Mock-friendly seam: an <see cref="I525Media"/> Moq can be wrapped in an
    /// <see cref="Image525Result"/> and consumed via the factory's result type per PRD §7.
    /// </summary>
    [Test]
    public void Mock_I525Media_CanBeFedThroughFactoryResult()
    {
        var mockMedia = new Mock<I525Media>(MockBehavior.Strict);
        mockMedia.SetupGet(m => m.Geometry).Returns(DiskGeometry.Standard525Dos);
        mockMedia.SetupGet(m => m.OptimalTrackLength).Returns(GcrEncoder.StandardTrackLength);
        mockMedia.SetupGet(m => m.IsReadOnly).Returns(false);

        var result = new Image525Result(mockMedia.Object, DiskImageFormat.NibbleImage, "<mock>", false);

        // The pattern-match path is exactly what real callers (controllers, debug commands)
        // will use; this verifies the seam does not require sealed types or singletons.
        switch ((DiskImageOpenResult)result)
        {
            case Image525Result r:
                Assert.That(r.Media.Geometry.TrackCount, Is.EqualTo(35));
                Assert.That(r.Media.OptimalTrackLength, Is.EqualTo(GcrEncoder.StandardTrackLength));
                Assert.That(r.Media.IsReadOnly, Is.False);
                break;
            default:
                Assert.Fail("Pattern match did not select Image525Result.");
                break;
        }

        mockMedia.VerifyAll();
    }

    private static byte[] Build2Mg(byte[] payload, int format, uint flags)
    {
        var image = new byte[64 + payload.Length];
        var span = image.AsSpan();
        span[0] = (byte)'2';
        span[1] = (byte)'I';
        span[2] = (byte)'M';
        span[3] = (byte)'G';
        span[4] = (byte)'B';
        span[5] = (byte)'M';
        span[6] = (byte)'S';
        span[7] = (byte)'L';
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(8, 2), 64);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(0x0C, 4), format);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(0x10, 4), flags);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(0x18, 4), 64);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(0x1C, 4), payload.Length);
        payload.CopyTo(image.AsSpan(64));
        return image;
    }

    private string Temp(byte[] bytes, string extension)
    {
        var path = ImageFixtures.WriteTempFile(bytes, extension);
        this.tempPaths.Add(path);
        return path;
    }
}