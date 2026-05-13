// <copyright file="Image525Result.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using BadMango.Emulator.Storage.Media;

/// <summary>
/// Open result for an image that exposes only an <see cref="I525Media"/> view (e.g. <c>.nib</c>).
/// </summary>
public sealed record Image525Result : DiskImageOpenResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Image525Result"/> class.
    /// </summary>
    /// <param name="media">The track-addressed view.</param>
    /// <param name="format">Detected format.</param>
    /// <param name="path">Source path.</param>
    /// <param name="isReadOnly">Whether the image was opened read-only.</param>
    public Image525Result(I525Media media, DiskImageFormat format, string path, bool isReadOnly)
        : base(format, path, isReadOnly)
    {
        this.Media = media;
    }

    /// <summary>
    /// Gets the track-addressed view.
    /// </summary>
    /// <value>The 5.25" track view of the underlying image.</value>
    public I525Media Media { get; }
}