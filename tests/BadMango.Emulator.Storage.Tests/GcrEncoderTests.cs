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
}