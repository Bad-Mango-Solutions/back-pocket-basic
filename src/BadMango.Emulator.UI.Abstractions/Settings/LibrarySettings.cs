// <copyright file="LibrarySettings.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Settings;

/// <summary>
/// Library path settings for disk images, ROMs, logs, and save states.
/// </summary>
public record LibrarySettings
{
    /// <summary>
    /// Gets the base directory for all storage.
    /// </summary>
    public string LibraryRoot { get; init; } = "~/.backpocket";

    /// <summary>
    /// Gets the location of disk images. Uses {Library} as a placeholder for LibraryRoot.
    /// </summary>
    public string DiskImagesPath { get; init; } = "{Library}/disks";

    /// <summary>
    /// Gets the location of ROM images. Uses {Library} as a placeholder for LibraryRoot.
    /// </summary>
    public string RomImagesPath { get; init; } = "{Library}/roms";

    /// <summary>
    /// Gets the location of log files. Uses {Library} as a placeholder for LibraryRoot.
    /// </summary>
    public string LogFilesPath { get; init; } = "{Library}/logs";

    /// <summary>
    /// Gets the location of save states. Uses {Library} as a placeholder for LibraryRoot.
    /// </summary>
    public string SaveStatesPath { get; init; } = "{Library}/saves";

    /// <summary>
    /// Gets a value indicating whether to scan the library for changes at startup.
    /// </summary>
    public bool AutoScanOnStartup { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to monitor the library for external changes.
    /// </summary>
    public bool WatchForChanges { get; init; } = true;
}