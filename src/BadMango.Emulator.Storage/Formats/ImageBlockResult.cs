// <copyright file="ImageBlockResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using BadMango.Emulator.Storage.Media;

/// <summary>
/// Open result for an image that exposes only an <see cref="IBlockMedia"/> view (e.g. <c>.hdv</c>).
/// </summary>
/// <param name="Media">The block-addressed view.</param>
/// <param name="FormatX">Detected format.</param>
/// <param name="PathX">Source path.</param>
/// <param name="ReadOnly">Whether the image was opened read-only.</param>
public sealed record ImageBlockResult(IBlockMedia Media, DiskImageFormat FormatX, string PathX, bool ReadOnly)
    : DiskImageOpenResult(FormatX, PathX, ReadOnly);