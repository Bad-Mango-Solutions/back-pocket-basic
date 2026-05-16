// <copyright file="GcrEncoderTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Tests;

/// <summary>
/// GCR 6-and-2 encoder/decoder tests (PRD §6.1 row 3 acceptance).
/// </summary>
[TestFixture]
public class GcrEncoderTests
{
    /// <summary>
    /// Verifies the WriteTable / ReadTable round-trip for every 6-bit value.
    /// </summary>
    [Test]
    public void WriteTable_RoundTripsThroughReadTable()
    {
        var write = GcrEncoder.GetWriteTable();
        var read = GcrEncoder.GetReadTable();
        Assert.That(write.Length, Is.EqualTo(64));
        for (var v = 0; v < 64; v++)
        {
            var nibble = write[v];
            Assert.That(read[nibble], Is.EqualTo((byte)v), $"6-bit value {v} did not round-trip via nibble 0x{nibble:X2}.");
        }
    }

    /// <summary>
    /// Verifies that an encoded track decodes back to the original sector data for every
    /// (volume, track) tuple in the standard 5.25" range.
    /// </summary>
    /// <param name="volume">DOS volume number written into address fields.</param>
    /// <param name="track">Track number written into address fields.</param>
    [TestCase(254, 0)]
    [TestCase(254, 17)]
    [TestCase(254, 34)]
    [TestCase(1, 0)]
    [TestCase(255, 34)]
    [TestCase(0, 0)]
    public void EncodeDecode_RoundTripsAllSectors(int volume, int track)
    {
        var sectors = new byte[16 * 256];
        var rng = new Random(volume + (track * 4096));
        rng.NextBytes(sectors);

        var nibbles = new byte[GcrEncoder.StandardTrackLength];
        GcrEncoder.EncodeTrack(volume, track, sectors, nibbles);

        var decoded = new byte[16 * 256];
        var mask = GcrEncoder.DecodeTrack(nibbles, decoded);

        Assert.That(mask, Is.EqualTo(0xFFFF), $"Not every physical sector decoded for vol={volume}, track={track}.");
        Assert.That(decoded, Is.EqualTo(sectors));
    }

    /// <summary>
    /// Verifies that the encoded track contains a valid address-field prologue at the
    /// expected offsets and that the embedded sector numbers go 0…15.
    /// </summary>
    [Test]
    public void EncodeTrack_AddressFields_HavePhysicalSectorOrder()
    {
        var sectors = new byte[16 * 256];
        var nibbles = new byte[GcrEncoder.StandardTrackLength];
        GcrEncoder.EncodeTrack(254, 17, sectors, nibbles);

        // Address field starts 48 bytes into each 416-byte sector slot. Sector
        // number is 4-and-4 encoded at bytes 7..8 of the address field.
        for (var phys = 0; phys < 16; phys++)
        {
            var addrStart = (phys * 416) + 48;
            Assert.That(nibbles[addrStart], Is.EqualTo(GcrEncoder.AddressPrologue1), $"prologue byte 1 of phys {phys}");
            Assert.That(nibbles[addrStart + 1], Is.EqualTo(GcrEncoder.AddressPrologue2), $"prologue byte 2 of phys {phys}");
            Assert.That(nibbles[addrStart + 2], Is.EqualTo(GcrEncoder.AddressPrologue3), $"prologue byte 3 of phys {phys}");

            var secH = nibbles[addrStart + 7];
            var secL = nibbles[addrStart + 8];
            var sec = (byte)(((secH << 1) | 1) & secL);
            Assert.That(sec, Is.EqualTo((byte)phys));
        }
    }

    /// <summary>
    /// Verifies that decoding all-zeros nibbles yields no sectors but does not throw.
    /// </summary>
    [Test]
    public void DecodeTrack_NoAddressFields_ReturnsZeroMask()
    {
        var nibbles = new byte[GcrEncoder.StandardTrackLength];
        nibbles.AsSpan().Fill(0xFF);
        var decoded = new byte[16 * 256];
        var mask = GcrEncoder.DecodeTrack(nibbles, decoded);
        Assert.That(mask, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that the 6-and-2 auxiliary ("low two bits") byte at the start
    /// of the data field carries the low two bits of <em>sector byte 0</em>,
    /// matching the real Apple II RWTS POSTNIB16 layout. A previous bug
    /// reversed this ordering (aux byte 0 carried bits for sector byte 85),
    /// which produced sectors whose top 6 bits decoded correctly but whose
    /// bottom 2 bits were permuted in groups of 86.
    /// </summary>
    [Test]
    public void EncodeTrack_DataFieldAuxBytes_AreInRwtsOrder()
    {
        // Sector 0, byte 0 = 0x02 (low2 = 10), byte 86 = 0x01 (low2 = 01),
        // byte 172 = 0x03 (low2 = 11). All other bytes zero. Each pair is
        // bit-reversed (low-bit-first) per the Apple II convention before
        // packing into twoBit[0] at shifts 0, 2, and 4 respectively:
        //   reversed(10) = 01 at shift 0 -> 0x01
        //   reversed(01) = 10 at shift 2 -> 0x08
        //   reversed(11) = 11 at shift 4 -> 0x30
        // twoBit[0] = 0x39, and xorChain[0] = twoBit[0] ^ 0 = 0x39.
        var sectors = new byte[16 * 256];
        sectors[0] = 0x02;
        sectors[86] = 0x01;
        sectors[172] = 0x03;

        var nibbles = new byte[GcrEncoder.StandardTrackLength];
        GcrEncoder.EncodeTrack(254, 0, sectors, nibbles);

        // Find sector-0's data field: 48 gap + 14 addr + 5 gap = 67 bytes in,
        // then 3 bytes of data prologue.
        const int sector0DataStart = 48 + 14 + 5 + 3;
        var firstAuxNibble = nibbles[sector0DataStart];
        var read = GcrEncoder.GetReadTable();
        var firstAux6Bit = read[firstAuxNibble];

        Assert.That(firstAux6Bit, Is.Not.EqualTo((byte)0xFF), "First aux nibble must translate via the read table.");
        Assert.That(firstAux6Bit, Is.EqualTo((byte)0x39), "First aux 6-bit value must encode the low two bits of sector bytes 0, 86, 172 (not byte 85's bits).");

        // And a full round-trip must still recover the original sector.
        var decoded = new byte[16 * 256];
        var mask = GcrEncoder.DecodeTrack(nibbles, decoded);
        Assert.That(mask & 1, Is.EqualTo(1), "Sector 0 must decode.");
        Assert.That(decoded[0], Is.EqualTo((byte)0x02));
        Assert.That(decoded[86], Is.EqualTo((byte)0x01));
        Assert.That(decoded[172], Is.EqualTo((byte)0x03));
    }

    /// <summary>
    /// Verifies that out-of-range <paramref name="volume"/> or <paramref name="track"/>
    /// throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <param name="volume">Volume to attempt.</param>
    /// <param name="track">Track to attempt.</param>
    [TestCase(-1, 0)]
    [TestCase(256, 0)]
    [TestCase(0, -1)]
    [TestCase(0, 256)]
    public void EncodeTrack_OutOfRangeVolumeOrTrack_Throws(int volume, int track)
    {
        var sectors = new byte[16 * 256];
        var nibbles = new byte[GcrEncoder.StandardTrackLength];
        Assert.Throws<ArgumentOutOfRangeException>(() => GcrEncoder.EncodeTrack(volume, track, sectors, nibbles));
    }
}