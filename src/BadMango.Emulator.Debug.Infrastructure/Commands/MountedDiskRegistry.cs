// <copyright file="MountedDiskRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Storage.Formats;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Tracks the <see cref="DiskImageOpenResult"/> handles owned by the debug-console
/// <c>disk insert</c> path so that the underlying file backends are released on eject,
/// re-mount, or context teardown instead of leaking until process exit.
/// </summary>
/// <remarks>
/// <para>
/// The runtime <c>disk insert</c> subcommand opens an image through
/// <see cref="DiskImageFactory.Open"/> and mounts the resulting media into a live
/// <see cref="IDiskController"/>. The <see cref="DiskImageOpenResult"/>
/// owns the underlying storage backend (and therefore the open <see cref="FileStream"/>);
/// disposing it closes the file. Because the controller keeps the media reference for
/// the lifetime of the mount, the result cannot simply be wrapped in <c>using</c>: the
/// file must stay open until the medium is ejected or the debug context is torn down.
/// </para>
/// <para>
/// This registry stores those open results keyed by <c>(slot, driveIndex)</c>. It is
/// owned by <see cref="DebugContext"/>; disposing the context disposes the registry,
/// which disposes every retained open result (and therefore every retained file handle).
/// </para>
/// </remarks>
public sealed class MountedDiskRegistry : IDisposable
{
    private readonly Dictionary<(int Slot, int DriveIndex), DiskImageOpenResult> entries = [];
    private bool disposed;

    /// <summary>
    /// Gets the number of currently tracked mounts.
    /// </summary>
    /// <value>The count of <c>(slot, drive)</c> entries with a retained open result.</value>
    public int Count => this.entries.Count;

    /// <summary>
    /// Gets a value indicating whether <see cref="Dispose"/> has been called.
    /// </summary>
    /// <remarks>
    /// Callers (notably the <c>disk eject</c> path) should consult this before invoking
    /// <see cref="Track"/> or <see cref="Release"/> when they would otherwise translate
    /// a thrown <see cref="ObjectDisposedException"/> into an emulator crash. The Disk II
    /// hardware itself has no latch interlock — opening the door during a read is always
    /// permitted (just unsafe for the data) — so eject paths must remain crash-free even
    /// when the surrounding registry has already been torn down.
    /// </remarks>
    /// <value><see langword="true"/> once <see cref="Dispose"/> has run.</value>
    public bool IsDisposed => this.disposed;

    /// <summary>
    /// Records the <see cref="DiskImageOpenResult"/> that backs the medium just mounted at
    /// <paramref name="slot"/> / <paramref name="driveIndex"/>. If a prior open result was
    /// already tracked at the same key, it is disposed first to release its file handle.
    /// </summary>
    /// <param name="slot">1-based slot number (1..7).</param>
    /// <param name="driveIndex">0-based drive index within the controller.</param>
    /// <param name="open">The open result whose lifetime should follow this mount.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="open"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the registry has already been disposed.</exception>
    public void Track(int slot, int driveIndex, DiskImageOpenResult open)
    {
        ArgumentNullException.ThrowIfNull(open);
        ObjectDisposedException.ThrowIf(this.disposed, this);

        var key = (slot, driveIndex);
        if (this.entries.Remove(key, out var prior))
        {
            prior.Dispose();
        }

        this.entries[key] = open;
    }

    /// <summary>
    /// Disposes and removes the open result tracked for the given drive, releasing its
    /// underlying file handle. Safe to call when no entry exists.
    /// </summary>
    /// <param name="slot">1-based slot number (1..7).</param>
    /// <param name="driveIndex">0-based drive index within the controller.</param>
    /// <returns><see langword="true"/> when an entry was disposed and removed; otherwise <see langword="false"/>.</returns>
    public bool Release(int slot, int driveIndex)
    {
        if (this.disposed)
        {
            return false;
        }

        if (!this.entries.Remove((slot, driveIndex), out var prior))
        {
            return false;
        }

        prior.Dispose();
        return true;
    }

    /// <summary>
    /// Disposes every retained open result and clears the registry, but keeps the
    /// registry usable for subsequent <see cref="Track"/> calls. Intended for use when
    /// the underlying machine is being detached and re-attached without tearing down
    /// the surrounding <see cref="DebugContext"/>.
    /// </summary>
    public void Clear()
    {
        if (this.disposed)
        {
            return;
        }

        foreach (var entry in this.entries.Values)
        {
            entry.Dispose();
        }

        this.entries.Clear();
    }

    /// <summary>
    /// Disposes every retained open result, releasing every file handle the registry owns.
    /// Subsequent <see cref="Track"/> calls throw <see cref="ObjectDisposedException"/>.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        foreach (var entry in this.entries.Values)
        {
            entry.Dispose();
        }

        this.entries.Clear();
    }
}