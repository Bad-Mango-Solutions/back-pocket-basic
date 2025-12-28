// <copyright file="SettingsService.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.Services;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using BadMango.Emulator.Configuration.Models;

using Microsoft.Extensions.Logging;

/// <summary>
/// Service for managing application settings with persistence.
/// </summary>
public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly ILogger<SettingsService>? logger;
    private readonly ISettingsMigrator? migrator;
    private readonly string settingsFilePath;
    private AppSettings current;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for settings operations.</param>
    /// <param name="migrator">Optional settings migrator for version upgrades.</param>
    /// <param name="settingsDirectory">Optional directory for settings storage. Defaults to ~/.backpocket.</param>
    public SettingsService(
        ILogger<SettingsService>? logger = null,
        ISettingsMigrator? migrator = null,
        string? settingsDirectory = null)
    {
        this.logger = logger;
        this.migrator = migrator;
        this.current = new AppSettings();

        var directory = settingsDirectory ?? GetDefaultSettingsDirectory();
        this.settingsFilePath = Path.Combine(directory, "settings.json");
    }

    /// <inheritdoc/>
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <inheritdoc/>
    public AppSettings Current => current;

    /// <inheritdoc/>
    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(settingsFilePath))
            {
                logger?.LogInformation("Settings file not found at {Path}, using defaults", settingsFilePath);
                current = new AppSettings();
                return current;
            }

            var json = await File.ReadAllTextAsync(settingsFilePath).ConfigureAwait(false);

            // First deserialize to check version
            using var document = JsonDocument.Parse(json);
            var version = 1;
            if (document.RootElement.TryGetProperty("version", out var versionElement))
            {
                version = versionElement.GetInt32();
            }

            // Check if migration is needed
            if (migrator?.NeedsMigration(version) == true)
            {
                logger?.LogInformation("Migrating settings from version {FromVersion} to {ToVersion}", version, migrator.CurrentVersion);
                current = migrator.Migrate(document.RootElement, version);
            }
            else
            {
                current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }

            logger?.LogInformation("Settings loaded from {Path}", settingsFilePath);
            OnSettingsChanged(new SettingsChangedEventArgs { IsFullReload = true });
            return current;
        }
        catch (JsonException ex)
        {
            logger?.LogError(ex, "Failed to parse settings file, using defaults");
            current = new AppSettings();
            return current;
        }
        catch (IOException ex)
        {
            logger?.LogError(ex, "Failed to read settings file, using defaults");
            current = new AppSettings();
            return current;
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var directory = Path.GetDirectoryName(settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(settingsFilePath, json).ConfigureAwait(false);

            current = settings;
            logger?.LogInformation("Settings saved to {Path}", settingsFilePath);
            OnSettingsChanged(new SettingsChangedEventArgs { IsFullReload = true });
        }
        catch (IOException ex)
        {
            logger?.LogError(ex, "Failed to save settings file");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<AppSettings> ResetToDefaultsAsync()
    {
        logger?.LogInformation("Resetting settings to defaults");
        current = new AppSettings();
        OnSettingsChanged(new SettingsChangedEventArgs { IsFullReload = true });
        return Task.FromResult(current);
    }

    /// <inheritdoc/>
    public async Task ExportAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            var json = JsonSerializer.Serialize(current, JsonOptions);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
            logger?.LogInformation("Settings exported to {Path}", path);
        }
        catch (IOException ex)
        {
            logger?.LogError(ex, "Failed to export settings to {Path}", path);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<AppSettings> ImportAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize settings");

            current = settings;
            logger?.LogInformation("Settings imported from {Path}", path);
            OnSettingsChanged(new SettingsChangedEventArgs { IsFullReload = true });
            return current;
        }
        catch (JsonException ex)
        {
            logger?.LogError(ex, "Failed to parse settings file at {Path}", path);
            throw;
        }
        catch (IOException ex)
        {
            logger?.LogError(ex, "Failed to read settings file at {Path}", path);
            throw;
        }
    }

    /// <inheritdoc/>
    public T GetValue<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var parts = key.Split('.');
        object? currentObj = current;

        foreach (var part in parts)
        {
            if (currentObj is null)
            {
                return default!;
            }

            var property = currentObj.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
            {
                logger?.LogWarning("Setting key not found: {Key}", key);
                return default!;
            }

            currentObj = property.GetValue(currentObj);
        }

        if (currentObj is T typedValue)
        {
            return typedValue;
        }

        return default!;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This implementation supports two-level nesting only (e.g., "General.Theme").
    /// For settings with deeper nesting, use SaveAsync with a modified AppSettings record.
    /// </remarks>
    public void SetValue<T>(string key, T value)
    {
        ArgumentNullException.ThrowIfNull(key);

        // For setting values, we need to create a new record with the updated value
        // This is complex with nested records, so we'll serialize/deserialize with modifications
        var parts = key.Split('.');
        if (parts.Length != 2)
        {
            logger?.LogWarning("SetValue only supports two-level keys (Category.Property). Key: {Key}", key);
            return;
        }

        // Convert current settings to a mutable dictionary
        var json = JsonSerializer.Serialize(current, JsonOptions);

        var mutableDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (mutableDict is null)
        {
            logger?.LogWarning("Failed to deserialize settings for SetValue operation");
            return;
        }

        // Navigate and update the value
        var categoryName = char.ToLowerInvariant(parts[0][0]) + parts[0][1..];
        if (mutableDict.TryGetValue(categoryName, out var categoryElement))
        {
            var categoryDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(categoryElement.GetRawText());
            if (categoryDict is not null)
            {
                var propertyName = char.ToLowerInvariant(parts[1][0]) + parts[1][1..];
                categoryDict[propertyName] = value;
                var updatedJson = JsonSerializer.Serialize(categoryDict, JsonOptions);
                mutableDict[categoryName] = JsonDocument.Parse(updatedJson).RootElement.Clone();
            }
        }

        // Reconstruct the AppSettings
        var newJson = JsonSerializer.Serialize(mutableDict, JsonOptions);
        var newSettings = JsonSerializer.Deserialize<AppSettings>(newJson, JsonOptions);
        if (newSettings is not null)
        {
            current = newSettings;
            OnSettingsChanged(new SettingsChangedEventArgs { ChangedKeys = [key] });
        }
    }

    private static string GetDefaultSettingsDirectory()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".backpocket");
    }

    /// <summary>
    /// Raises the SettingsChanged event in a thread-safe manner.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    private void OnSettingsChanged(SettingsChangedEventArgs args)
    {
        var handler = SettingsChanged;
        handler?.Invoke(this, args);
    }
}