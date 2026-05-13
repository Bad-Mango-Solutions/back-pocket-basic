// <copyright file="GcrEncoder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Gcr;

/// <summary>
/// GCR 6-and-2 encoder: emits the raw nibble stream for a 16-sector 5.25" track.
/// </summary>
/// <remarks>
/// <para>
/// Implements PRD §6.1 FR-S6 / row-3 acceptance: full address-field generation with
/// volume / track / sector / checksum and standard prologue (<c>$D5 $AA $96</c> for
/// address fields, <c>$D5 $AA $AD</c> for data fields) and epilogue (<c>$DE $AA $EB</c>).
/// </para>
/// <para>
/// Sectors are emitted in physical order 0..15. Each call writes exactly
/// <see cref="StandardTrackLength"/> bytes (the conventional 6656-byte track size used
/// by <c>.nib</c> images and most emulators).
/// </para>
/// </remarks>
public static class GcrEncoder
{
    /// <summary>
    /// Standard nibble-stream length per track for 16-sector 5.25" media.
    /// </summary>
    public const int StandardTrackLength = 6656;

    /// <summary>
    /// Number of decoded user-data bytes per sector.
    /// </summary>
    public const int BytesPerSector = 256;

    /// <summary>Address-field prologue byte 1.</summary>
    public const byte AddressPrologue1 = 0xD5;

    /// <summary>Address-field prologue byte 2.</summary>
    public const byte AddressPrologue2 = 0xAA;

    /// <summary>Address-field prologue byte 3.</summary>
    public const byte AddressPrologue3 = 0x96;

    /// <summary>Data-field prologue byte 3.</summary>
    public const byte DataPrologue3 = 0xAD;

    /// <summary>Field epilogue byte 1.</summary>
    public const byte EpiloguePrologue1 = 0xDE;

    /// <summary>Field epilogue byte 2.</summary>
    public const byte EpiloguePrologue2 = 0xAA;

    /// <summary>Field epilogue byte 3.</summary>
    public const byte EpiloguePrologue3 = 0xEB;

    /// <summary>
    /// Self-sync gap byte.
    /// </summary>
    public const byte GapByte = 0xFF;

    // 6-bit value -> nibble (the canonical Apple II 6-and-2 write-translate table).
    private static readonly byte[] WriteTable =
    {
        0x96, 0x97, 0x9A, 0x9B, 0x9D, 0x9E, 0x9F, 0xA6,
        0xA7, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF, 0xB2, 0xB3,
        0xB4, 0xB5, 0xB6, 0xB7, 0xB9, 0xBA, 0xBB, 0xBC,
        0xBD, 0xBE, 0xBF, 0xCB, 0xCD, 0xCE, 0xCF, 0xD3,
        0xD6, 0xD7, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE,
        0xDF, 0xE5, 0xE6, 0xE7, 0xE9, 0xEA, 0xEB, 0xEC,
        0xED, 0xEE, 0xEF, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6,
        0xF7, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
    };

    // Inverse of WriteTable: nibble -> 6-bit value, or 0xFF for invalid.
    private static readonly byte[] ReadTable = BuildReadTable();

    /// <summary>
    /// Gets a copy of the 6-and-2 write-translate table (6-bit value → nibble).
    /// </summary>
    /// <returns>A 64-byte table mapping each 6-bit value to its on-disk nibble.</returns>
    public static byte[] GetWriteTable() => (byte[])WriteTable.Clone();

    /// <summary>
    /// Gets a copy of the 6-and-2 read-translate table (nibble → 6-bit value, or <c>0xFF</c> if invalid).
    /// </summary>
    /// <returns>A 256-byte table mapping each nibble byte to its 6-bit value.</returns>
    public static byte[] GetReadTable() => (byte[])ReadTable.Clone();

    /// <summary>
    /// Encodes a single 5.25" track to its nibble stream.
    /// </summary>
    /// <param name="volume">Volume number written into address fields (DOS 3.3 default 254); must be in <c>[0, 256)</c>.</param>
    /// <param name="track">Track number written into address fields; must be in <c>[0, 256)</c>.</param>
    /// <param name="sectorData">A buffer of <c>16 × <see cref="BytesPerSector"/> = 4096</c> bytes; sectors must already be in physical order.</param>
    /// <param name="destination">Destination nibble buffer; must be exactly <see cref="StandardTrackLength"/> bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="volume"/> or <paramref name="track"/> is outside <c>[0, 256)</c>.</exception>
    /// <exception cref="ArgumentException">If buffer lengths are incorrect.</exception>
    public static void EncodeTrack(int volume, int track, ReadOnlySpan<byte> sectorData, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(volume);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(volume, 255);
        ArgumentOutOfRangeException.ThrowIfNegative(track);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(track, 255);

        if (sectorData.Length != SectorSkew.SectorsPerTrack * BytesPerSector)
        {
            throw new ArgumentException($"sectorData must be {SectorSkew.SectorsPerTrack * BytesPerSector} bytes.", nameof(sectorData));
        }

        if (destination.Length != StandardTrackLength)
        {
            throw new ArgumentException($"destination must be {StandardTrackLength} bytes.", nameof(destination));
        }

        // Fill with self-sync gap; per-field writes overwrite where appropriate.
        destination.Fill(GapByte);

        // Layout per sector: 48 gap bytes, then address field (14 bytes), 5 gap bytes,
        // then data field (349 bytes). 16 * (48 + 14 + 5 + 349) = 16 * 416 = 6656.
        const int sectorStride = 416;
        const int gap1Length = 48;
        const int gap2Length = 5;

        for (var physical = 0; physical < SectorSkew.SectorsPerTrack; physical++)
        {
            var sectorStart = physical * sectorStride;
            var addressFieldStart = sectorStart + gap1Length;
            WriteAddressField(volume, track, physical, destination[addressFieldStart..(addressFieldStart + 14)]);

            var dataFieldStart = addressFieldStart + 14 + gap2Length;
            var sectorBytes = sectorData.Slice(physical * BytesPerSector, BytesPerSector);
            WriteDataField(sectorBytes, destination[dataFieldStart..(dataFieldStart + 349)]);
        }
    }

    /// <summary>
    /// Decodes a single 5.25" track from its nibble stream into 16 sectors of physical-order data.
    /// </summary>
    /// <param name="nibbles">Source nibble buffer.</param>
    /// <param name="sectorData">Destination buffer of <c>16 × <see cref="BytesPerSector"/> = 4096</c> bytes.</param>
    /// <returns>Bitmask of physical sectors that were successfully decoded (bit <c>n</c> set ⇒ sector <c>n</c> decoded).</returns>
    /// <exception cref="ArgumentException">If <paramref name="sectorData"/> is the wrong length.</exception>
    public static int DecodeTrack(ReadOnlySpan<byte> nibbles, Span<byte> sectorData)
    {
        if (sectorData.Length != SectorSkew.SectorsPerTrack * BytesPerSector)
        {
            throw new ArgumentException($"sectorData must be {SectorSkew.SectorsPerTrack * BytesPerSector} bytes.", nameof(sectorData));
        }

        sectorData.Clear();
        var found = 0;

        // Walk the nibble stream; treat it as cylindrical so an address field that
        // wraps the buffer end is still recognised.
        var n = nibbles.Length;
        if (n == 0)
        {
            return 0;
        }

        Span<byte> sectorBuffer = stackalloc byte[BytesPerSector];
        for (var i = 0; i < n; i++)
        {
            // Look for an address-field prologue: D5 AA 96.
            if (nibbles[i] != AddressPrologue1)
            {
                continue;
            }

            if (nibbles[Wrap(i + 1, n)] != AddressPrologue2 || nibbles[Wrap(i + 2, n)] != AddressPrologue3)
            {
                continue;
            }

            // Address-field body: vol (4-and-4), track (4-and-4), sector (4-and-4), checksum (4-and-4).
            var volH = nibbles[Wrap(i + 3, n)];
            var volL = nibbles[Wrap(i + 4, n)];
            var trkH = nibbles[Wrap(i + 5, n)];
            var trkL = nibbles[Wrap(i + 6, n)];
            var secH = nibbles[Wrap(i + 7, n)];
            var secL = nibbles[Wrap(i + 8, n)];
            var chkH = nibbles[Wrap(i + 9, n)];
            var chkL = nibbles[Wrap(i + 10, n)];

            var vol = Decode44(volH, volL);
            var trk = Decode44(trkH, trkL);
            var sec = Decode44(secH, secL);
            var chk = Decode44(chkH, chkL);
            if ((vol ^ trk ^ sec) != chk)
            {
                continue;
            }

            if (sec >= SectorSkew.SectorsPerTrack)
            {
                continue;
            }

            // Find the data-field prologue (D5 AA AD) within a reasonable window.
            var search = i + 14;
            var found2 = -1;
            for (var k = 0; k < 64; k++)
            {
                var idx = Wrap(search + k, n);
                if (nibbles[idx] == AddressPrologue1
                    && nibbles[Wrap(idx + 1, n)] == AddressPrologue2
                    && nibbles[Wrap(idx + 2, n)] == DataPrologue3)
                {
                    found2 = Wrap(idx + 3, n);
                    break;
                }
            }

            if (found2 < 0)
            {
                continue;
            }

            if (TryDecodeDataField(nibbles, found2, n, sectorBuffer))
            {
                sectorBuffer.CopyTo(sectorData.Slice(sec * BytesPerSector, BytesPerSector));
                found |= 1 << sec;
            }
        }

        return found;
    }

    private static byte[] BuildReadTable()
    {
        var table = new byte[256];
        Array.Fill(table, (byte)0xFF);
        for (var i = 0; i < WriteTable.Length; i++)
        {
            table[WriteTable[i]] = (byte)i;
        }

        return table;
    }

    private static int Wrap(int i, int n) => ((i % n) + n) % n;

    private static byte Decode44(byte high, byte low) => (byte)(((high << 1) | 1) & low);

    private static void Write44(byte value, Span<byte> dest)
    {
        dest[0] = (byte)((value >> 1) | 0xAA);
        dest[1] = (byte)(value | 0xAA);
    }

    private static void WriteAddressField(int volume, int track, int sector, Span<byte> dest)
    {
        // 14 bytes: D5 AA 96 V V T T S S C C DE AA EB
        dest[0] = AddressPrologue1;
        dest[1] = AddressPrologue2;
        dest[2] = AddressPrologue3;
        Write44((byte)volume, dest[3..5]);
        Write44((byte)track, dest[5..7]);
        Write44((byte)sector, dest[7..9]);
        var checksum = (byte)(volume ^ track ^ sector);
        Write44(checksum, dest[9..11]);
        dest[11] = EpiloguePrologue1;
        dest[12] = EpiloguePrologue2;
        dest[13] = EpiloguePrologue3;
    }

    private static void WriteDataField(ReadOnlySpan<byte> sector, Span<byte> dest)
    {
        // 349 bytes: D5 AA AD + 342 nibblised bytes + 1 checksum nibble + DE AA EB.
        dest[0] = AddressPrologue1;
        dest[1] = AddressPrologue2;
        dest[2] = DataPrologue3;

        // Step 1: split each byte into a 6-bit upper part and a 2-bit lower part.
        // The 86-byte "low two bits" array packs three sectors-of-2 bits per byte
        // (in reverse order) per the standard Apple II algorithm.
        Span<byte> sixBit = stackalloc byte[256];
        Span<byte> twoBit = stackalloc byte[86];
        twoBit.Clear();

        for (var i = 0; i < 256; i++)
        {
            sixBit[i] = (byte)(sector[i] >> 2);
            var idx = i % 86;
            var shift = (i / 86) * 2;

            // Reverse the 2 bits before packing (low bit -> high position) per the
            // standard Apple II convention.
            var twoBits = sector[i] & 0x03;
            var reversed = ((twoBits & 0x01) << 1) | ((twoBits & 0x02) >> 1);
            twoBit[idx] |= (byte)(reversed << shift);
        }

        // Step 2: XOR-chain encode (sixBit then twoBit, but written little-low first
        // i.e. twoBit reversed then sixBit). Then translate via WriteTable.
        Span<byte> xorChain = stackalloc byte[343];
        byte last = 0;

        // Write the 86 two-bit bytes first, in reverse order.
        for (var i = 0; i < 86; i++)
        {
            var b = twoBit[85 - i];
            xorChain[i] = (byte)(b ^ last);
            last = b;
        }

        // Then the 256 six-bit bytes in order.
        for (var i = 0; i < 256; i++)
        {
            var b = sixBit[i];
            xorChain[86 + i] = (byte)(b ^ last);
            last = b;
        }

        // Final checksum nibble = last value in the chain.
        xorChain[342] = last;

        for (var i = 0; i < 343; i++)
        {
            dest[3 + i] = WriteTable[xorChain[i] & 0x3F];
        }

        dest[346] = EpiloguePrologue1;
        dest[347] = EpiloguePrologue2;
        dest[348] = EpiloguePrologue3;
    }

    private static bool TryDecodeDataField(ReadOnlySpan<byte> nibbles, int start, int n, Span<byte> sector)
    {
        Span<byte> xorChain = stackalloc byte[343];
        for (var i = 0; i < 343; i++)
        {
            var v = ReadTable[nibbles[Wrap(start + i, n)]];
            if (v == 0xFF)
            {
                return false;
            }

            xorChain[i] = v;
        }

        // Inverse XOR chain.
        Span<byte> twoBit = stackalloc byte[86];
        Span<byte> sixBit = stackalloc byte[256];
        byte last = 0;
        for (var i = 0; i < 86; i++)
        {
            last ^= xorChain[i];
            twoBit[85 - i] = last;
        }

        for (var i = 0; i < 256; i++)
        {
            last ^= xorChain[86 + i];
            sixBit[i] = last;
        }

        // Verify checksum nibble.
        last ^= xorChain[342];
        if (last != 0)
        {
            return false;
        }

        for (var i = 0; i < 256; i++)
        {
            var idx = i % 86;
            var shift = (i / 86) * 2;
            var packed = (twoBit[idx] >> shift) & 0x03;

            // Reverse the two bits back to their original order.
            var twoBits = ((packed & 0x01) << 1) | ((packed & 0x02) >> 1);
            sector[i] = (byte)((sixBit[i] << 2) | twoBits);
        }

        return true;
    }
}