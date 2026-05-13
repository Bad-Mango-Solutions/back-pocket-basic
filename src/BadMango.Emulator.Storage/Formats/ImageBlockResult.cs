// <copyright file="ImageBlockResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using BadMango.Emulator.Storage.Media;

/// <summary>
/// Open result for an image that exposes only an <see cref="IBlockMedia"/> view (e.g. <c>.hdv</c>).
/// </summary>
public sealed record ImageBlockResult : DiskImageOpenResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageBlockResult"/> class.
    /// </summary>
    /// <param name="media">The block-addressed view.</param>
    /// <param name="format">Detected format.</param>
    /// <param name="path">Source path.</param>
    /// <param name="isReadOnly">Whether the image was opened read-only.</param>
    public ImageBlockResult(IBlockMedia media, DiskImageFormat format, string path, bool isReadOnly)
        : base(format, path, isReadOnly)
    {
        this.Media = media;
    }

    /// <summary>
    /// Gets the block-addressed view.
    /// </summary>
    /// <value>The block view of the underlying image.</value>
    public IBlockMedia Media { get; }
}