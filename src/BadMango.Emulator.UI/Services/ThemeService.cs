// <copyright file="ThemeService.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Services;

using Avalonia;
using Avalonia.Styling;

using Microsoft.Extensions.Logging;

/// <summary>
/// Service for managing application themes (dark/light mode).
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService>? logger;
    private bool isDarkTheme = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for theme operations.</param>
    public ThemeService(ILogger<ThemeService>? logger = null)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<bool>? ThemeChanged;

    /// <inheritdoc />
    public bool IsDarkTheme => isDarkTheme;

    /// <inheritdoc />
    public void SetTheme(bool isDark)
    {
        if (isDarkTheme == isDark)
        {
            return;
        }

        isDarkTheme = isDark;
        logger?.LogInformation("Theme changed to {Theme}", isDark ? "Dark" : "Light");

        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = isDark
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }

        ThemeChanged?.Invoke(this, isDark);
    }
}