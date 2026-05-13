// <copyright file="FileStorageBackend.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Backends;

/// <summary>
/// File-backed <see cref="IStorageBackend"/> using a <see cref="FileStream"/>.
/// </summary>
/// <remarks>
/// Writes go directly to the file (write-through). Wrap in
/// <see cref="RamCachedStorageBackend"/> to obtain block-level write-back caching.
/// </remarks>
public class FileStorageBackend : IStorageBackend
{
    private readonly FileStream stream;
    private readonly bool canWrite;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStorageBackend"/> class.
    /// </summary>
    /// <param name="path">Path to the backing file. The file must already exist.</param>
    /// <param name="readOnly">If <see langword="true"/>, the file is opened for read access only.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
    public FileStorageBackend(string path, bool readOnly = false)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Backing file not found.", path);
        }

        // Honor the OS-level read-only attribute as an implicit read-only mount.
        var attrs = File.GetAttributes(path);
        if ((attrs & FileAttributes.ReadOnly) != 0)
        {
            readOnly = true;
        }

        var access = readOnly ? FileAccess.Read : FileAccess.ReadWrite;
        var share = readOnly ? FileShare.Read : FileShare.None;
        this.stream = new FileStream(path, FileMode.Open, access, share);
        this.canWrite = !readOnly;
    }

    /// <inheritdoc />
    public long Length => this.stream.Length;

    /// <inheritdoc />
    public bool CanWrite => this.canWrite;

    /// <inheritdoc />
    public void Read(long offset, Span<byte> destination)
    {
        this.ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        if (offset + destination.Length > this.stream.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Range extends past end of file.");
        }

        this.stream.Seek(offset, SeekOrigin.Begin);
        int total = 0;
        while (total < destination.Length)
        {
            int read = this.stream.Read(destination[total..]);
            if (read <= 0)
            {
                throw new EndOfStreamException("Unexpected end of file while reading.");
            }

            total += read;
        }
    }

    /// <inheritdoc />
    public void Write(long offset, ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();
        if (!this.canWrite)
        {
            throw new InvalidOperationException("Backend is read-only.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        if (offset + source.Length > this.stream.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Range extends past end of file.");
        }

        this.stream.Seek(offset, SeekOrigin.Begin);
        this.stream.Write(source);
    }

    /// <inheritdoc />
    public void Flush()
    {
        this.ThrowIfDisposed();
        this.stream.Flush(flushToDisk: true);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!this.disposed)
        {
            this.stream.Dispose();
            this.disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
    }
}