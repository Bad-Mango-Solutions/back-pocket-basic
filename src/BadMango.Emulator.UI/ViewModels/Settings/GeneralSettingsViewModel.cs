// <copyright file="GeneralSettingsViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels.Settings;

using BadMango.Emulator.Configuration.Models;
using BadMango.Emulator.Configuration.Services;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel for general application settings.
/// </summary>
public partial class GeneralSettingsViewModel : SettingsPageViewModelBase
{
    [ObservableProperty]
    private bool loadLastProfile = true;

    [ObservableProperty]
    private bool startPaused;

    [ObservableProperty]
    private bool restoreWindowLayout = true;

    [ObservableProperty]
    private string language = "en-US";

    [ObservableProperty]
    private string theme = "Dark";

    [ObservableProperty]
    private bool checkForUpdates = true;

    [ObservableProperty]
    private bool enableTelemetry;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralSettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    public GeneralSettingsViewModel(ISettingsService settingsService)
        : base(settingsService, "General", "SettingsIcon", 0)
    {
    }

    /// <summary>
    /// Gets the available languages.
    /// </summary>
    public IReadOnlyList<string> AvailableLanguages { get; } =
    [
        "en-US",
        "en-GB",
        "es-ES",
        "de-DE",
        "fr-FR",
    ];

    /// <summary>
    /// Gets the available themes.
    /// </summary>
    public IReadOnlyList<string> AvailableThemes { get; } =
    [
        "Light",
        "Dark",
        "System",
    ];

    /// <inheritdoc/>
    public override Task LoadAsync()
    {
        var settings = SettingsService.Current.General;
        LoadLastProfile = settings.LoadLastProfile;
        StartPaused = settings.StartPaused;
        RestoreWindowLayout = settings.RestoreWindowLayout;
        Language = settings.Language;
        Theme = settings.Theme;
        CheckForUpdates = settings.CheckForUpdates;
        EnableTelemetry = settings.EnableTelemetry;
        HasChanges = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override async Task SaveAsync()
    {
        var current = SettingsService.Current;
        var newSettings = current with
        {
            General = new GeneralSettings
            {
                LoadLastProfile = LoadLastProfile,
                StartPaused = StartPaused,
                RestoreWindowLayout = RestoreWindowLayout,
                Language = Language,
                Theme = Theme,
                CheckForUpdates = CheckForUpdates,
                EnableTelemetry = EnableTelemetry,
            },
        };
        await SettingsService.SaveAsync(newSettings).ConfigureAwait(false);
        HasChanges = false;
    }

    /// <inheritdoc/>
    public override Task ResetToDefaultsAsync()
    {
        var defaults = new GeneralSettings();
        LoadLastProfile = defaults.LoadLastProfile;
        StartPaused = defaults.StartPaused;
        RestoreWindowLayout = defaults.RestoreWindowLayout;
        Language = defaults.Language;
        Theme = defaults.Theme;
        CheckForUpdates = defaults.CheckForUpdates;
        EnableTelemetry = defaults.EnableTelemetry;
        MarkAsChanged();
        return Task.CompletedTask;
    }

    partial void OnLoadLastProfileChanged(bool value) => MarkAsChanged();

    partial void OnStartPausedChanged(bool value) => MarkAsChanged();

    partial void OnRestoreWindowLayoutChanged(bool value) => MarkAsChanged();

    partial void OnLanguageChanged(string value) => MarkAsChanged();

    partial void OnThemeChanged(string value) => MarkAsChanged();

    partial void OnCheckForUpdatesChanged(bool value) => MarkAsChanged();

    partial void OnEnableTelemetryChanged(bool value) => MarkAsChanged();
}