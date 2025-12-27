// <copyright file="ISettingsPage.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Settings;

/// <summary>
/// Marker interface for settings page ViewModels.
/// Defines the contract for settings pages in the settings panel.
/// </summary>
public interface ISettingsPage
{
    /// <summary>
    /// Gets the display name of this settings page.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the icon key for this settings page.
    /// </summary>
    string IconKey { get; }

    /// <summary>
    /// Gets the parent category name, or null for root categories.
    /// </summary>
    string? ParentCategory { get; }

    /// <summary>
    /// Gets the sort order within the parent category.
    /// </summary>
    int SortOrder { get; }

    /// <summary>
    /// Gets a value indicating whether the page has unsaved changes.
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// Loads settings into the page.
    /// </summary>
    /// <returns>A task representing the asynchronous load operation.</returns>
    Task LoadAsync();

    /// <summary>
    /// Saves settings from the page.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveAsync();

    /// <summary>
    /// Resets settings to defaults for this page.
    /// </summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    Task ResetToDefaultsAsync();
}