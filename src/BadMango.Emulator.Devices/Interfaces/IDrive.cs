// <copyright file="IDrive.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Host-side abstraction of a disk drive mechanism.
/// </summary>
/// <remarks>
/// <para>
/// The drive abstraction tracks motor/head state and media insertion semantics independently from guest-slot wiring.
/// </para>
/// <para>
/// Reference specifications:
/// <see href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/os/Unified%20Block%20Device%20Backing%20API%20for%20Apple%20II%20Emulator.md">Unified Block Device Backing API for Apple II Emulator</see>,
/// <see href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/os/Disk%20II%20Controller%20Device%20Specification.md">Disk II Controller Device Specification</see>.
/// </para>
/// </remarks>
public interface IDrive
{
    /// <summary>
    /// Raised when media content or insertion/ejection state changes.
    /// </summary>
    event EventHandler? MediaChanged;

    /// <summary>
    /// Gets a value indicating whether this drive supports removable media.
    /// </summary>
    bool IsRemovable { get; }

    /// <summary>
    /// Gets a value indicating whether media is currently present.
    /// </summary>
    bool IsMediaPresent { get; }

    /// <summary>
    /// Gets a value indicating whether the drive motor is currently active.
    /// </summary>
    bool MotorOn { get; }

    /// <summary>
    /// Gets the current head position.
    /// </summary>
    int HeadPosition { get; }

    /// <summary>
    /// Gets the slot association for this drive.
    /// </summary>
    int SlotNumber { get; }

    /// <summary>
    /// Gets a value indicating whether the drive door is open.
    /// </summary>
    bool IsDoorOpen { get; }

    /// <summary>
    /// Gets a value indicating whether writes are currently blocked.
    /// </summary>
    bool IsWriteProtected { get; }

    /// <summary>
    /// Gets the currently attached media, when present.
    /// </summary>
    IMedia? CurrentMedia { get; }

    /// <summary>
    /// Gets strongly-typed metrics for host observability dashboards.
    /// </summary>
    DriveMetrics Metrics { get; }

    /// <summary>
    /// Gets a serializable metrics representation.
    /// </summary>
    /// <returns>A dictionary representation of <see cref="Metrics"/>.</returns>
    Dictionary<string, object> GetMetricsDictionary();

    /// <summary>
    /// Inserts removable media into the drive.
    /// </summary>
    /// <param name="media">The media to insert.</param>
    void InsertMedia(IMedia media);

    /// <summary>
    /// Ejects the currently inserted removable media.
    /// </summary>
    void EjectMedia();

    /// <summary>
    /// Attaches fixed media for non-removable drive configurations.
    /// </summary>
    /// <param name="media">The fixed media to attach.</param>
    void AttachFixedMedia(IMedia media);
}