// <copyright file="DiskImageOpenResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

/// <summary>
/// Pattern-matchable result returned by <see cref="DiskImageFactory.Open"/>.
/// </summary>
/// <remarks>
/// <para>
/// Concrete subclasses expose either a 5.25" track view (<see cref="Image525Result"/>),
/// a block view (<see cref="ImageBlockResult"/>), or both (<see cref="Image525AndBlockResult"/>).
/// Callers select the view appropriate to the controller they are wiring up.
/// </para>
/// <para>
/// Implements <see cref="IDisposable"/>: disposing the result releases the underlying
/// file handle that <see cref="DiskImageFactory"/> opened. Callers that hold the result
/// only for the duration of a single operation (for example debug commands such as
/// <c>disk info</c> or <c>disk create --bootable</c>) must dispose it so the source
/// file is not locked for the lifetime of the process.
/// </para>
/// </remarks>
public abstract record DiskImageOpenResult : IDisposable
{
    private IDisposable? backend;
    private bool disposed;

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

    /// <summary>
    /// Releases the underlying storage backend (and therefore the open file handle)
    /// associated with this result. Safe to call multiple times.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.backend?.Dispose();
        this.backend = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Associates the underlying storage backend with this result so it is released when
    /// the result is disposed. Intended for use by <see cref="DiskImageFactory"/>.
    /// </summary>
    /// <param name="backendToOwn">The backend whose lifetime should follow this result.</param>
    /// <returns>This same result, to support fluent construction in the factory.</returns>
    internal DiskImageOpenResult AttachBackend(IDisposable backendToOwn)
    {
        ArgumentNullException.ThrowIfNull(backendToOwn);
        this.backend = backendToOwn;
        return this;
    }
}