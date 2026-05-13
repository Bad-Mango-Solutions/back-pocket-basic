// <copyright file="Image525AndBlockResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using BadMango.Emulator.Storage.Media;

/// <summary>
/// Open result for a 5.25" sector image that exposes both a track view and a block view
/// (every sector-ordered <c>.dsk</c>/<c>.do</c>/<c>.po</c>/2MG-DOS/2MG-ProDOS image).
/// </summary>
/// <param name="TrackMedia">Track-addressed view (via the GCR nibblizer).</param>
/// <param name="BlockMedia">Block-addressed view (via the inverse skew).</param>
/// <param name="SectorOrder">The sector order of the underlying backing image (sniffed for <c>.dsk</c>).</param>
/// <param name="WasOrderSniffed">Whether <see cref="SectorOrder"/> was determined by sniffing rather than by extension.</param>
/// <param name="FormatX">Detected format.</param>
/// <param name="PathX">Source path.</param>
/// <param name="ReadOnly">Whether the image was opened read-only.</param>
public sealed record Image525AndBlockResult(
    I525Media TrackMedia,
    IBlockMedia BlockMedia,
    SectorOrder SectorOrder,
    bool WasOrderSniffed,
    DiskImageFormat FormatX,
    string PathX,
    bool ReadOnly)
    : DiskImageOpenResult(FormatX, PathX, ReadOnly);