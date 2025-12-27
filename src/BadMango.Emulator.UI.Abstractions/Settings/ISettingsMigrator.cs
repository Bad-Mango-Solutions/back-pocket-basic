// <copyright file="ISettingsMigrator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Settings;

using System.Text.Json;

/// <summary>
/// Service interface for handling settings schema migrations between versions.
/// </summary>
public interface ISettingsMigrator
{
    /// <summary>
    /// Gets the current settings schema version.
    /// </summary>
    int CurrentVersion { get; }

    /// <summary>
    /// Migrates settings from an older version to the current version.
    /// </summary>
    /// <param name="oldSettings">The JSON element containing the old settings.</param>
    /// <param name="fromVersion">The version of the old settings.</param>
    /// <returns>The migrated settings.</returns>
    AppSettings Migrate(JsonElement oldSettings, int fromVersion);

    /// <summary>
    /// Checks if migration is needed for the given settings version.
    /// </summary>
    /// <param name="settingsVersion">The version of the settings to check.</param>
    /// <returns>True if migration is needed; otherwise, false.</returns>
    bool NeedsMigration(int settingsVersion);
}