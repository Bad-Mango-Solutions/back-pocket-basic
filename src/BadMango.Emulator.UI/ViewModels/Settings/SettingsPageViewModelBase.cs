// <copyright file="SettingsPageViewModelBase.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels.Settings;

using BadMango.Emulator.UI.Abstractions.Settings;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Base class for settings page ViewModels.
/// </summary>
public abstract partial class SettingsPageViewModelBase : ViewModelBase, ISettingsPage
{
    [ObservableProperty]
    private bool hasChanges;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageViewModelBase"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    /// <param name="displayName">The display name of the page.</param>
    /// <param name="iconKey">The icon key for the page.</param>
    /// <param name="sortOrder">The sort order within the category.</param>
    /// <param name="parentCategory">The parent category name, if any.</param>
    protected SettingsPageViewModelBase(
        ISettingsService settingsService,
        string displayName,
        string iconKey,
        int sortOrder = 0,
        string? parentCategory = null)
    {
        this.SettingsService = settingsService;
        this.DisplayName = displayName;
        this.IconKey = iconKey;
        this.SortOrder = sortOrder;
        this.ParentCategory = parentCategory;
    }

    /// <inheritdoc/>
    public string DisplayName { get; }

    /// <inheritdoc/>
    public string IconKey { get; }

    /// <inheritdoc/>
    public string? ParentCategory { get; }

    /// <inheritdoc/>
    public int SortOrder { get; }

    /// <inheritdoc/>
    bool ISettingsPage.HasChanges => HasChanges;

    /// <summary>
    /// Gets the settings service.
    /// </summary>
    protected ISettingsService SettingsService { get; }

    /// <inheritdoc/>
    public abstract Task LoadAsync();

    /// <inheritdoc/>
    public abstract Task SaveAsync();

    /// <inheritdoc/>
    public abstract Task ResetToDefaultsAsync();

    /// <summary>
    /// Marks the page as having unsaved changes.
    /// </summary>
    protected void MarkAsChanged()
    {
        HasChanges = true;
    }
}