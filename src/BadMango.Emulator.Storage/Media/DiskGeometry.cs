// <copyright file="DiskGeometry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Media;

/// <summary>
/// Geometry of a 5.25" disk image: track / sector counts plus on-disk sector ordering.
/// </summary>
/// <param name="TrackCount">Number of whole tracks (typically 35).</param>
/// <param name="SectorsPerTrack">Sectors per track (16 for 6-and-2 GCR; 13 for 5-and-3 is out of scope).</param>
/// <param name="BytesPerSector">Bytes of decoded user data per sector (typically 256).</param>
/// <param name="SectorOrder">Logical-to-physical sector mapping used to interpret the backing image.</param>
public readonly record struct DiskGeometry(int TrackCount, int SectorsPerTrack, int BytesPerSector, SectorOrder SectorOrder)
{
    /// <summary>
    /// Standard 5.25" 35-track / 16-sector / 256-byte geometry in DOS 3.3 order.
    /// </summary>
    public static readonly DiskGeometry Standard525Dos = new(35, 16, 256, SectorOrder.Dos33);

    /// <summary>
    /// Standard 5.25" 35-track / 16-sector / 256-byte geometry in ProDOS order.
    /// </summary>
    public static readonly DiskGeometry Standard525ProDos = new(35, 16, 256, SectorOrder.ProDos);

    /// <summary>
    /// Gets the total decoded user-data length of the image in bytes
    /// (<see cref="TrackCount"/> × <see cref="SectorsPerTrack"/> × <see cref="BytesPerSector"/>).
    /// </summary>
    /// <value>The total user-data byte length for this geometry.</value>
    public int TotalBytes => this.TrackCount * this.SectorsPerTrack * this.BytesPerSector;

    /// <summary>
    /// Gets the number of quarter-track positions (<see cref="TrackCount"/> × 4).
    /// </summary>
    /// <value>The maximum quarter-track index plus one.</value>
    public int QuarterTrackCount => this.TrackCount * 4;
}