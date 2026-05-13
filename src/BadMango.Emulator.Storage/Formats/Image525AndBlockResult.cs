// <copyright file="Image525AndBlockResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using BadMango.Emulator.Storage.Media;

/// <summary>
/// Open result for a 5.25" sector image that exposes both a track view and a block view
/// (every sector-ordered <c>.dsk</c>/<c>.do</c>/<c>.po</c>/2MG-DOS/2MG-ProDOS image).
/// </summary>
public sealed record Image525AndBlockResult : DiskImageOpenResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Image525AndBlockResult"/> class.
    /// </summary>
    /// <param name="trackMedia">Track-addressed view (via the GCR nibblizer).</param>
    /// <param name="blockMedia">Block-addressed view (via the inverse skew).</param>
    /// <param name="sectorOrder">Sector order of the underlying backing image (sniffed for <c>.dsk</c>).</param>
    /// <param name="wasOrderSniffed">Whether <paramref name="sectorOrder"/> was determined by sniffing rather than by extension.</param>
    /// <param name="format">Detected format.</param>
    /// <param name="path">Source path.</param>
    /// <param name="isReadOnly">Whether the image was opened read-only.</param>
    public Image525AndBlockResult(
        I525Media trackMedia,
        IBlockMedia blockMedia,
        SectorOrder sectorOrder,
        bool wasOrderSniffed,
        DiskImageFormat format,
        string path,
        bool isReadOnly)
        : base(format, path, isReadOnly)
    {
        this.TrackMedia = trackMedia;
        this.BlockMedia = blockMedia;
        this.SectorOrder = sectorOrder;
        this.WasOrderSniffed = wasOrderSniffed;
    }

    /// <summary>Gets the track-addressed view of the image.</summary>
    /// <value>The 5.25" view, produced via the GCR nibblizer.</value>
    public I525Media TrackMedia { get; }

    /// <summary>Gets the block-addressed view of the image.</summary>
    /// <value>The block view, produced via the inverse skew.</value>
    public IBlockMedia BlockMedia { get; }

    /// <summary>Gets the sector order of the underlying backing image.</summary>
    /// <value>The detected sector order.</value>
    public SectorOrder SectorOrder { get; }

    /// <summary>Gets a value indicating whether the sector order was determined by sniffing.</summary>
    /// <value><see langword="true"/> for ambiguous <c>.dsk</c> images that were sniffed; <see langword="false"/> when the order is fixed by the extension.</value>
    public bool WasOrderSniffed { get; }
}