// <copyright file="SettingsMigrator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.Services;

using System.Text.Json;

using BadMango.Emulator.Configuration.Models;

using Microsoft.Extensions.Logging;

/// <summary>
/// Handles settings schema migrations between versions.
/// </summary>
public class SettingsMigrator : ISettingsMigrator
{
    private readonly ILogger<SettingsMigrator>? logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsMigrator"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for migration operations.</param>
    public SettingsMigrator(ILogger<SettingsMigrator>? logger = null)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public int CurrentVersion => 1;

    /// <inheritdoc/>
    public bool NeedsMigration(int settingsVersion)
    {
        return settingsVersion < CurrentVersion;
    }

    /// <inheritdoc/>
    public AppSettings Migrate(JsonElement oldSettings, int fromVersion)
    {
        logger?.LogInformation("Starting settings migration from version {FromVersion} to {ToVersion}", fromVersion, CurrentVersion);

        var currentSettings = oldSettings;
        var currentVersion = fromVersion;

        // Apply migrations sequentially
        // Note: No migrations are currently defined. This loop structure is kept
        // so that future versioned migrations can be added here without
        // changing the surrounding logic.
        while (currentVersion < CurrentVersion)
        {
            currentSettings = currentVersion switch
            {
                // Add migration steps here as schema evolves
                // 0 => MigrateV0ToV1(currentSettings),
                // 1 => MigrateV1ToV2(currentSettings),
                _ => currentSettings,
            };
            currentVersion++;
        }

        // Deserialize the migrated settings
        var json = currentSettings.GetRawText();
        var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        logger?.LogInformation("Settings migration completed successfully");
        return settings ?? new AppSettings();
    }
}