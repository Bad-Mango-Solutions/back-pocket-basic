// <copyright file="SectorSkew.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Gcr;

using BadMango.Emulator.Storage.Media;

/// <summary>
/// DOS 3.3 and ProDOS sector-skew tables for 16-sector 5.25" media.
/// </summary>
/// <remarks>
/// <para>
/// A sector image stored in <see cref="SectorOrder.Dos33"/> order (<c>.dsk</c> sniffed
/// as DOS, <c>.do</c>) holds sectors in DOS 3.3 logical order: file offset
/// <c>(track * 16 + L) * 256</c> contains DOS logical sector <c>L</c> of the given track.
/// A <see cref="SectorOrder.ProDos"/> image (<c>.po</c>) instead holds ProDOS logical
/// sector <c>L</c> at the same position, where each ProDOS block <c>B</c> spans
/// logical sectors <c>2B</c> and <c>2B + 1</c>.
/// </para>
/// <para>
/// The GCR nibblizer always emits sectors in physical order (the order they actually
/// appear on the spinning disk: 0, 1, 2, …, 15). These tables map each physical sector
/// number to the logical position to read from the backing image.
/// </para>
/// </remarks>
public static class SectorSkew
{
    /// <summary>
    /// Number of sectors per track for 6-and-2 GCR media.
    /// </summary>
    public const int SectorsPerTrack = 16;

    // Physical sector index -> DOS 3.3 logical sector index.
    // Standard interleave used by every Apple II tool that handles .dsk/.do.
    private static readonly byte[] PhysicalToDosLogical =
    {
        0x0, 0x7, 0xE, 0x6, 0xD, 0x5, 0xC, 0x4,
        0xB, 0x3, 0xA, 0x2, 0x9, 0x1, 0x8, 0xF,
    };

    // Physical sector index -> ProDOS logical sector index.
    // Mirrors the DOS table about the centre, except for sectors 0 and 15 which are fixed.
    private static readonly byte[] PhysicalToProDosLogical =
    {
        0x0, 0x8, 0x1, 0x9, 0x2, 0xA, 0x3, 0xB,
        0x4, 0xC, 0x5, 0xD, 0x6, 0xE, 0x7, 0xF,
    };

    private static readonly byte[] DosLogicalToPhysical = Invert(PhysicalToDosLogical);
    private static readonly byte[] ProDosLogicalToPhysical = Invert(PhysicalToProDosLogical);

    /// <summary>
    /// Maps a physical sector number to its logical position in a backing image of the given order.
    /// </summary>
    /// <param name="order">Sector ordering of the backing image.</param>
    /// <param name="physicalSector">Physical sector number in the range <c>[0, 16)</c>.</param>
    /// <returns>Logical sector index to read/write in the backing image.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="physicalSector"/> is out of range.</exception>
    public static int PhysicalToLogical(SectorOrder order, int physicalSector)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(physicalSector);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(physicalSector, SectorsPerTrack);
        return order switch
        {
            SectorOrder.Dos33 => PhysicalToDosLogical[physicalSector],
            SectorOrder.ProDos => PhysicalToProDosLogical[physicalSector],
            _ => throw new ArgumentOutOfRangeException(nameof(order)),
        };
    }

    /// <summary>
    /// Maps a logical sector number in a backing image of the given order to its physical position.
    /// </summary>
    /// <param name="order">Sector ordering of the backing image.</param>
    /// <param name="logicalSector">Logical sector number in the range <c>[0, 16)</c>.</param>
    /// <returns>Physical sector number on disk.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="logicalSector"/> is out of range.</exception>
    public static int LogicalToPhysical(SectorOrder order, int logicalSector)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(logicalSector);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(logicalSector, SectorsPerTrack);
        return order switch
        {
            SectorOrder.Dos33 => DosLogicalToPhysical[logicalSector],
            SectorOrder.ProDos => ProDosLogicalToPhysical[logicalSector],
            _ => throw new ArgumentOutOfRangeException(nameof(order)),
        };
    }

    private static byte[] Invert(byte[] forward)
    {
        var inverse = new byte[forward.Length];
        for (var i = 0; i < forward.Length; i++)
        {
            inverse[forward[i]] = (byte)i;
        }

        return inverse;
    }
}