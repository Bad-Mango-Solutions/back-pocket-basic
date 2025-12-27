// <copyright file="ISettingsService.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Settings;

/// <summary>
/// Service interface for managing application settings.
/// Provides load, save, reset, export, and import functionality for settings persistence.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Event raised when settings change.
    /// </summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    AppSettings Current { get; }

    /// <summary>
    /// Loads settings from the default storage location.
    /// </summary>
    /// <returns>A task that returns the loaded settings.</returns>
    Task<AppSettings> LoadAsync();

    /// <summary>
    /// Saves the specified settings to the default storage location.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveAsync(AppSettings settings);

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    /// <returns>A task that returns the default settings.</returns>
    Task<AppSettings> ResetToDefaultsAsync();

    /// <summary>
    /// Exports settings to a file at the specified path.
    /// </summary>
    /// <param name="path">The file path to export settings to.</param>
    /// <returns>A task representing the asynchronous export operation.</returns>
    Task ExportAsync(string path);

    /// <summary>
    /// Imports settings from a file at the specified path.
    /// </summary>
    /// <param name="path">The file path to import settings from.</param>
    /// <returns>A task that returns the imported settings.</returns>
    Task<AppSettings> ImportAsync(string path);

    /// <summary>
    /// Gets a specific setting value by key path.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The dot-separated key path (e.g., "General.Theme").</param>
    /// <returns>The setting value, or the default value if not found.</returns>
    T GetValue<T>(string key);

    /// <summary>
    /// Sets a specific setting value by key path.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The dot-separated key path (e.g., "General.Theme").</param>
    /// <param name="value">The value to set.</param>
    void SetValue<T>(string key, T value);
}