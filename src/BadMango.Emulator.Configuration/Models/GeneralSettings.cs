// <copyright file="GeneralSettings.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.Models;

/// <summary>
/// General application settings.
/// </summary>
public record GeneralSettings
{
    /// <summary>
    /// Gets a value indicating whether to auto-load the last used profile on startup.
    /// </summary>
    public bool LoadLastProfile { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to start the emulator in a paused state.
    /// </summary>
    public bool StartPaused { get; init; }

    /// <summary>
    /// Gets a value indicating whether to restore pop-out windows on startup.
    /// </summary>
    public bool RestoreWindowLayout { get; init; } = true;

    /// <summary>
    /// Gets the UI language preference.
    /// </summary>
    public string Language { get; init; } = "en-US";

    /// <summary>
    /// Gets the theme preference (Light, Dark, or System).
    /// </summary>
    public string Theme { get; init; } = "Dark";

    /// <summary>
    /// Gets a value indicating whether to automatically check for updates.
    /// </summary>
    public bool CheckForUpdates { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to send anonymous usage telemetry.
    /// </summary>
    public bool EnableTelemetry { get; init; }
}