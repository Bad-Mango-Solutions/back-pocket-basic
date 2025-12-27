// <copyright file="SettingsWindowViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels.Settings;

using System.Collections.ObjectModel;

using BadMango.Emulator.UI.Abstractions.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for the settings window with tree-view navigation.
/// </summary>
public partial class SettingsWindowViewModel : ViewModelBase
{
    private readonly ISettingsService settingsService;

    [ObservableProperty]
    private ISettingsPage? selectedPage;

    [ObservableProperty]
    private bool hasChanges;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindowViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    public SettingsWindowViewModel(ISettingsService settingsService)
    {
        this.settingsService = settingsService;

        // Initialize settings pages
        SettingsPages = new ObservableCollection<ISettingsPage>
        {
            new GeneralSettingsViewModel(settingsService),
            new LibrarySettingsViewModel(settingsService),
            new DisplaySettingsViewModel(settingsService),
            new InputSettingsViewModel(settingsService),
            new DebugSettingsViewModel(settingsService),
            new EditorSettingsViewModel(settingsService),
            new AboutViewModel(),
        };

        // Select the first page by default
        SelectedPage = SettingsPages.FirstOrDefault();
    }

    /// <summary>
    /// Gets the collection of settings pages.
    /// </summary>
    public ObservableCollection<ISettingsPage> SettingsPages { get; }

    /// <summary>
    /// Applies the current settings changes.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task ApplyAsync()
    {
        foreach (var page in SettingsPages)
        {
            if (page.HasChanges)
            {
                await page.SaveAsync().ConfigureAwait(false);
            }
        }

        HasChanges = false;
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        await settingsService.ResetToDefaultsAsync().ConfigureAwait(false);

        foreach (var page in SettingsPages)
        {
            await page.LoadAsync().ConfigureAwait(false);
        }

        HasChanges = false;
    }

    /// <summary>
    /// Navigates to the specified settings page.
    /// </summary>
    /// <param name="pageName">The display name of the page to navigate to.</param>
    [RelayCommand]
    private void NavigateToPage(string pageName)
    {
        var page = SettingsPages.FirstOrDefault(p => p.DisplayName == pageName);
        if (page is not null)
        {
            SelectedPage = page;
        }
    }

    partial void OnSelectedPageChanged(ISettingsPage? value)
    {
        // Load the page when selected
        value?.LoadAsync();
    }
}