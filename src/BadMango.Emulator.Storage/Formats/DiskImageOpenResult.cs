// <copyright file="DiskImageOpenResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

/// <summary>
/// Pattern-matchable result returned by <see cref="DiskImageFactory.Open"/>.
/// </summary>
/// <remarks>
/// Concrete subclasses expose either a 5.25" track view (<see cref="Image525Result"/>),
/// a block view (<see cref="ImageBlockResult"/>), or both (<see cref="Image525AndBlockResult"/>).
/// Callers select the view appropriate to the controller they are wiring up.
/// </remarks>
public abstract record DiskImageOpenResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiskImageOpenResult"/> class.
    /// </summary>
    /// <param name="format">Detected format.</param>
    /// <param name="path">Source path that produced this result.</param>
    /// <param name="isReadOnly">Whether the image was opened read-only.</param>
    protected DiskImageOpenResult(DiskImageFormat format, string path, bool isReadOnly)
    {
        this.Format = format;
        this.Path = path;
        this.IsReadOnly = isReadOnly;
    }

    /// <summary>Gets the detected format identity.</summary>
    /// <value>The format chosen by <see cref="DiskImageFactory"/>.</value>
    public DiskImageFormat Format { get; }

    /// <summary>Gets the source path that produced this result.</summary>
    /// <value>The image file path.</value>
    public string Path { get; }

    /// <summary>Gets a value indicating whether the image was opened read-only.</summary>
    /// <value><see langword="true"/> if writes are rejected at the media level.</value>
    public bool IsReadOnly { get; }
}