// <copyright file="PathValidationWarning.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Settings;

/// <summary>
/// Warning types that can occur during path validation.
/// </summary>
public enum PathValidationWarning
{
    /// <summary>
    /// The directory does not exist.
    /// </summary>
    DirectoryDoesNotExist,

    /// <summary>
    /// Insufficient permissions to access the path.
    /// </summary>
    InsufficientPermissions,

    /// <summary>
    /// Low disk space at the path location.
    /// </summary>
    LowDiskSpace,

    /// <summary>
    /// The path is a network path.
    /// </summary>
    NetworkPath,

    /// <summary>
    /// The path is relative rather than absolute.
    /// </summary>
    RelativePath,
}