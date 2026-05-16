// <copyright file="ControllerDriveChangeKind.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Storage;

using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Indicates the type of drive transition reported by <see cref="IStorageController.DriveChanged"/>.
/// </summary>
public enum ControllerDriveChangeKind
{
    /// <summary>
    /// A drive was attached to the controller.
    /// </summary>
    Added,

    /// <summary>
    /// A drive was removed from the controller.
    /// </summary>
    Removed,

    /// <summary>
    /// A drive experienced a major status transition (for example media insertion, ejection, or write-protect changes).
    /// </summary>
    StatusChanged,
}