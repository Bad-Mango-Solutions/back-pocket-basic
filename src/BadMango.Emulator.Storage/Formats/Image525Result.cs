// <copyright file="Image525Result.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using BadMango.Emulator.Storage.Media;

/// <summary>
/// Open result for an image that exposes only an <see cref="I525Media"/> view (e.g. <c>.nib</c>).
/// </summary>
/// <param name="Media">The track-addressed view.</param>
/// <param name="FormatX">Detected format.</param>
/// <param name="PathX">Source path.</param>
/// <param name="ReadOnly">Whether the image was opened read-only.</param>
public sealed record Image525Result(I525Media Media, DiskImageFormat FormatX, string PathX, bool ReadOnly)
    : DiskImageOpenResult(FormatX, PathX, ReadOnly);