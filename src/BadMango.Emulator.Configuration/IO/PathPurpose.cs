// <copyright file="PathPurpose.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.IO;

/// <summary>
/// The intended purpose of a path setting.
/// </summary>
public enum PathPurpose
{
    /// <summary>
    /// The library root directory.
    /// </summary>
    LibraryRoot,

    /// <summary>
    /// The disk images directory.
    /// </summary>
    DiskImages,

    /// <summary>
    /// The ROM images directory.
    /// </summary>
    RomImages,

    /// <summary>
    /// The log files directory.
    /// </summary>
    LogFiles,

    /// <summary>
    /// The save states directory.
    /// </summary>
    SaveStates,
}