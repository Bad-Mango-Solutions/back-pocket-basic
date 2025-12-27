// <copyright file="LibrarySettingsViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels.Settings;

using BadMango.Emulator.UI.Abstractions.Settings;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel for library path settings.
/// </summary>
public partial class LibrarySettingsViewModel : SettingsPageViewModelBase
{
    [ObservableProperty]
    private string libraryRoot = "~/.backpocket";

    [ObservableProperty]
    private string diskImagesPath = "{Library}/disks";

    [ObservableProperty]
    private string romImagesPath = "{Library}/roms";

    [ObservableProperty]
    private string logFilesPath = "{Library}/logs";

    [ObservableProperty]
    private string saveStatesPath = "{Library}/saves";

    [ObservableProperty]
    private bool autoScanOnStartup = true;

    [ObservableProperty]
    private bool watchForChanges = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibrarySettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    public LibrarySettingsViewModel(ISettingsService settingsService)
        : base(settingsService, "Library", "FolderIcon", 1)
    {
    }

    /// <inheritdoc/>
    public override Task LoadAsync()
    {
        var settings = SettingsService.Current.Library;
        LibraryRoot = settings.LibraryRoot;
        DiskImagesPath = settings.DiskImagesPath;
        RomImagesPath = settings.RomImagesPath;
        LogFilesPath = settings.LogFilesPath;
        SaveStatesPath = settings.SaveStatesPath;
        AutoScanOnStartup = settings.AutoScanOnStartup;
        WatchForChanges = settings.WatchForChanges;
        HasChanges = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override async Task SaveAsync()
    {
        var current = SettingsService.Current;
        var newSettings = current with
        {
            Library = new LibrarySettings
            {
                LibraryRoot = LibraryRoot,
                DiskImagesPath = DiskImagesPath,
                RomImagesPath = RomImagesPath,
                LogFilesPath = LogFilesPath,
                SaveStatesPath = SaveStatesPath,
                AutoScanOnStartup = AutoScanOnStartup,
                WatchForChanges = WatchForChanges,
            },
        };
        await SettingsService.SaveAsync(newSettings).ConfigureAwait(false);
        HasChanges = false;
    }

    /// <inheritdoc/>
    public override Task ResetToDefaultsAsync()
    {
        var defaults = new LibrarySettings();
        LibraryRoot = defaults.LibraryRoot;
        DiskImagesPath = defaults.DiskImagesPath;
        RomImagesPath = defaults.RomImagesPath;
        LogFilesPath = defaults.LogFilesPath;
        SaveStatesPath = defaults.SaveStatesPath;
        AutoScanOnStartup = defaults.AutoScanOnStartup;
        WatchForChanges = defaults.WatchForChanges;
        MarkAsChanged();
        return Task.CompletedTask;
    }

    partial void OnLibraryRootChanged(string value) => MarkAsChanged();

    partial void OnDiskImagesPathChanged(string value) => MarkAsChanged();

    partial void OnRomImagesPathChanged(string value) => MarkAsChanged();

    partial void OnLogFilesPathChanged(string value) => MarkAsChanged();

    partial void OnSaveStatesPathChanged(string value) => MarkAsChanged();

    partial void OnAutoScanOnStartupChanged(bool value) => MarkAsChanged();

    partial void OnWatchForChangesChanged(bool value) => MarkAsChanged();
}