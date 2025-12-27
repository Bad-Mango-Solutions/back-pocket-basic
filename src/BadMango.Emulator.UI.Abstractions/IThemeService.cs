// <copyright file="IThemeService.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions;

/// <summary>
/// Service interface for managing application themes.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<bool>? ThemeChanged;

    /// <summary>
    /// Gets a value indicating whether the current theme is dark.
    /// </summary>
    bool IsDarkTheme { get; }

    /// <summary>
    /// Sets the application theme.
    /// </summary>
    /// <param name="isDark">True for dark theme, false for light theme.</param>
    void SetTheme(bool isDark);
}