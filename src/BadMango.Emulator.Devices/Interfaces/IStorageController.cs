// <copyright file="IStorageController.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

using BadMango.Emulator.Devices.Storage;

/// <summary>
/// Host-side abstraction for a storage controller managing one or more drives.
/// </summary>
/// <remarks>
/// <para>
/// This interface omits slot-card concerns and focuses on host-observable drive composition and lifecycle.
/// </para>
/// <para>
/// Reference specifications:
/// <see href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/os/SmartPort%20Specification.md">SmartPort Specification</see>,
/// <see href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/os/Disk%20II%20Controller%20Device%20Specification.md">Disk II Controller Device Specification</see>.
/// </para>
/// </remarks>
public interface IStorageController
{
    /// <summary>
    /// Raised when a drive is added, removed, or has a major status transition.
    /// </summary>
    event EventHandler<ControllerEventArgs>? DriveChanged;

    /// <summary>
    /// Gets the drives currently attached to the controller.
    /// </summary>
    IReadOnlyList<IStorageDrive> Drives { get; }

    /// <summary>
    /// Gets strongly-typed metrics for host observability dashboards.
    /// </summary>
    ControllerMetrics Metrics { get; }

    /// <summary>
    /// Gets a serializable metrics representation.
    /// </summary>
    /// <returns>A dictionary representation of <see cref="Metrics"/>.</returns>
    Dictionary<string, object> GetMetricsDictionary();

    /// <summary>
    /// Attaches a drive to the controller.
    /// </summary>
    /// <param name="drive">The drive to attach.</param>
    void AttachDrive(IStorageDrive drive);

    /// <summary>
    /// Removes a drive from the controller.
    /// </summary>
    /// <param name="drive">The drive to remove.</param>
    void RemoveDrive(IStorageDrive drive);
}