// <copyright file="AppSettings.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.Models;

/// <summary>
/// Application settings model with versioning support.
/// Combines all settings categories into a single configuration object.
/// </summary>
public record AppSettings
{
    /// <summary>
    /// Gets the settings schema version for migration support.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Gets the general application settings.
    /// </summary>
    public GeneralSettings General { get; init; } = new();

    /// <summary>
    /// Gets the library path settings.
    /// </summary>
    public LibrarySettings Library { get; init; } = new();

    /// <summary>
    /// Gets the display settings.
    /// </summary>
    public DisplaySettings Display { get; init; } = new();

    /// <summary>
    /// Gets the input settings.
    /// </summary>
    public InputSettings Input { get; init; } = new();

    /// <summary>
    /// Gets the debug settings.
    /// </summary>
    public DebugSettings Debug { get; init; } = new();

    /// <summary>
    /// Gets the editor settings.
    /// </summary>
    public EditorSettings Editor { get; init; } = new();

    /// <summary>
    /// Gets the ID of the last active profile.
    /// </summary>
    public string? LastProfileId { get; init; }
}