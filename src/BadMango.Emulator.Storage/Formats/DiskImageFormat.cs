// <copyright file="DiskImageFormat.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

/// <summary>
/// Container / encoding identity of a disk image as detected by <see cref="DiskImageFactory"/>.
/// </summary>
public enum DiskImageFormat
{
    /// <summary>Unknown or undetected.</summary>
    Unknown = 0,

    /// <summary>5.25" raw sector image, DOS 3.3 logical order (<c>.dsk</c> sniffed-as-DOS, <c>.do</c>).</summary>
    Dos33SectorImage,

    /// <summary>5.25" raw sector image, ProDOS logical order (<c>.dsk</c> sniffed-as-ProDOS, <c>.po</c>).</summary>
    ProDosSectorImage,

    /// <summary>5.25" raw nibble image (<c>.nib</c>).</summary>
    NibbleImage,

    /// <summary>2MG-wrapped sector image, DOS payload (<c>.2mg</c>/<c>.2img</c>).</summary>
    TwoImgDos,

    /// <summary>2MG-wrapped sector image, ProDOS payload (<c>.2mg</c>/<c>.2img</c>).</summary>
    TwoImgProDos,

    /// <summary>2MG-wrapped nibble payload (<c>.2mg</c>/<c>.2img</c>).</summary>
    TwoImgNibble,

    /// <summary>Raw 512-byte-block hard-disk image (<c>.hdv</c>).</summary>
    HdvBlockImage,

    /// <summary>13-sector 5.25" image (<c>.d13</c>) — recognised but not supported.</summary>
    D13Unsupported,
}