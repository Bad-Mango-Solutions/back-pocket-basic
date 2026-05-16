// <copyright file="IDiskController.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Media;

/// <summary>
/// High-level controller seam for disk emulation, designed to be Moq-friendly.
/// </summary>
/// <remarks>
/// <para>
/// Implementations expose runtime mount / eject / flush operations and per-drive state
/// snapshots without coupling tests to the concrete controller implementation. This is
/// the seam called out by PRD §7 FR-T1 / FR-T2 — the unit-test surface for disk
/// controllers across both Disk II and SmartPort variants.
/// </para>
/// </remarks>
public interface IDiskController
{
    /// <summary>
    /// Gets the slot number this controller is installed in (1-7).
    /// </summary>
    /// <value>The slot number assigned by the slot manager.</value>
    int SlotNumber { get; }

    /// <summary>
    /// Gets the number of drives this controller exposes.
    /// </summary>
    /// <value>The total drive count (e.g. 2 for Disk II).</value>
    int DriveCount { get; }

    /// <summary>
    /// Mounts the specified medium into the indicated drive.
    /// </summary>
    /// <param name="driveIndex">Zero-based drive index (<c>0</c> = drive 1, <c>1</c> = drive 2, etc.).</param>
    /// <param name="media">The medium to mount.</param>
    /// <param name="imagePath">Optional path that produced this medium; surfaced via <see cref="GetDriveSnapshot"/>.</param>
    /// <remarks>
    /// Implementations defer the actual swap to a scheduler-safe boundary so the controller
    /// never observes a half-mounted drive mid-byte (PRD FR-R1).
    /// </remarks>
    void Mount(int driveIndex, I525Media media, string? imagePath = null);

    /// <summary>
    /// Ejects the medium from the indicated drive after flushing dirty state.
    /// </summary>
    /// <param name="driveIndex">Zero-based drive index.</param>
    /// <returns>
    /// <see langword="true"/> on success; <see langword="false"/> if the drive was empty
    /// or the underlying flush failed and the eject was rejected (PRD FR-R2).
    /// </returns>
    bool Eject(int driveIndex);

    /// <summary>
    /// Flushes any pending writes for the indicated drive without ejecting.
    /// </summary>
    /// <param name="driveIndex">Zero-based drive index.</param>
    void Flush(int driveIndex);

    /// <summary>
    /// Returns a debug snapshot of the requested drive's runtime state.
    /// </summary>
    /// <param name="driveIndex">Zero-based drive index.</param>
    /// <returns>The drive snapshot, mirroring PRD FR-D10's debug surface.</returns>
    DriveSnapshot GetDriveSnapshot(int driveIndex);

    /// <summary>
    /// Returns the medium currently mounted in the indicated drive, or <see langword="null"/>
    /// when the drive is empty.
    /// </summary>
    /// <param name="driveIndex">Zero-based drive index.</param>
    /// <returns>The mounted <see cref="I525Media"/>, or <see langword="null"/> if empty.</returns>
    /// <remarks>
    /// Intended for debug-console inspection commands (e.g. <c>disk dump-track</c>,
    /// <c>disk read-sector</c>) that need direct access to the live nibble stream without
    /// going through the controller's bus-side read path. Production runtime code should
    /// continue to use the soft-switch surface instead of reaching for the media directly.
    /// </remarks>
    I525Media? GetMedia(int driveIndex);
}