// <copyright file="SectorOrder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Media;

/// <summary>
/// Logical-to-physical sector ordering used by 16-sector 5.25" disk images.
/// </summary>
public enum SectorOrder
{
    /// <summary>
    /// DOS 3.3 logical order (used by <c>.dsk</c> sniffed-as-DOS and <c>.do</c> images).
    /// </summary>
    Dos33 = 0,

    /// <summary>
    /// ProDOS logical order (used by <c>.dsk</c> sniffed-as-ProDOS and <c>.po</c> images).
    /// </summary>
    ProDos = 1,
}