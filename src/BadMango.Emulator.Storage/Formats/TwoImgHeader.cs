// <copyright file="TwoImgHeader.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using System.Buffers.Binary;
using System.Text;

/// <summary>
/// Parsed 2MG / 2IMG container header (PRD §6.1, format support FR-S5).
/// </summary>
/// <param name="Creator">Four-character creator code (e.g. <c>"!nfc"</c>, <c>"BMSL"</c>).</param>
/// <param name="HeaderLength">Header length in bytes (always 64 for v1).</param>
/// <param name="Format">Payload encoding: 0 = DOS 3.3 sector order, 1 = ProDOS sector order, 2 = nibble.</param>
/// <param name="Flags">Image flag bits; bit 31 (mask <c>0x80000000</c>) signals write-protect; bits 0–7 hold the DOS volume number when bit 8 is set.</param>
/// <param name="DataOffset">Byte offset within the file at which the payload begins.</param>
/// <param name="DataLength">Length of the payload in bytes.</param>
public readonly record struct TwoImgHeader(
    string Creator,
    int HeaderLength,
    int Format,
    uint Flags,
    int DataOffset,
    int DataLength)
{
    /// <summary>
    /// Magic four-byte signature at file offset 0 (<c>"2IMG"</c>).
    /// </summary>
    public static readonly byte[] Magic = "2IMG"u8.ToArray();

    /// <summary>
    /// Gets a value indicating whether the write-protect flag bit (bit 31) is set.
    /// </summary>
    /// <value><see langword="true"/> if bit 31 of <see cref="Flags"/> is set.</value>
    public bool IsWriteProtected => (this.Flags & 0x80000000u) != 0;

    /// <summary>
    /// Gets a value indicating whether a DOS volume number is embedded in the flags.
    /// </summary>
    /// <value><see langword="true"/> if bit 8 of <see cref="Flags"/> is set.</value>
    public bool HasDosVolumeNumber => (this.Flags & 0x00000100u) != 0;

    /// <summary>
    /// Gets the DOS volume number embedded in the flags, or 254 if none is present.
    /// </summary>
    /// <value>The DOS volume number from flag bits 0–7, or 254 by default.</value>
    public int DosVolumeNumber => this.HasDosVolumeNumber ? (int)(this.Flags & 0xFFu) : 254;

    /// <summary>
    /// Parses a 2MG header from the start of the supplied buffer.
    /// </summary>
    /// <param name="buffer">Buffer containing at least the first 64 bytes of the file.</param>
    /// <returns>The parsed header.</returns>
    /// <exception cref="ArgumentException">If the buffer is too short or the magic bytes are wrong.</exception>
    public static TwoImgHeader Parse(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 64)
        {
            throw new ArgumentException("2MG header must be at least 64 bytes.", nameof(buffer));
        }

        if (!buffer[..4].SequenceEqual(Magic))
        {
            throw new ArgumentException("Buffer does not start with the 2MG magic bytes.", nameof(buffer));
        }

        var creator = Encoding.ASCII.GetString(buffer.Slice(4, 4));
        var headerLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(8, 2));
        var format = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(0x0C, 4));
        var flags = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(0x10, 4));
        var dataOffset = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(0x18, 4));
        var dataLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(0x1C, 4));

        return new TwoImgHeader(creator, headerLength, format, flags, dataOffset, dataLength);
    }
}