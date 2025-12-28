// <copyright file="ThemeService.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Services;

using Avalonia;
using Avalonia.Styling;

using BadMango.Emulator.Configuration.Events;
using BadMango.Emulator.Configuration.Services;
using BadMango.Emulator.Infrastructure.Events;
using BadMango.Emulator.UI.Abstractions.Events;

using Microsoft.Extensions.Logging;

/// <summary>
/// Service for managing application themes (dark/light mode).
/// Subscribes to SettingsChangedEvent via EventAggregator to automatically
/// update the theme when settings change.
/// </summary>
public class ThemeService : IThemeService, IDisposable
{
    private readonly ILogger<ThemeService>? logger;
    private readonly IEventAggregator? eventAggregator;
    private readonly ISettingsService? settingsService;
    private readonly IDisposable? settingsSubscription;
    private bool isDarkTheme = true;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for theme operations.</param>
    /// <param name="eventAggregator">Optional event aggregator for pub/sub messaging.</param>
    /// <param name="settingsService">Optional settings service to read theme settings from.</param>
    public ThemeService(
        ILogger<ThemeService>? logger = null,
        IEventAggregator? eventAggregator = null,
        ISettingsService? settingsService = null)
    {
        this.logger = logger;
        this.eventAggregator = eventAggregator;
        this.settingsService = settingsService;

        // Subscribe to settings changes via event aggregator
        if (eventAggregator is not null && settingsService is not null)
        {
            settingsSubscription = eventAggregator.Subscribe<SettingsChangedEvent>(OnSettingsChanged);
        }
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

        // Raise the direct event
        ThemeChanged?.Invoke(this, isDark);

        // Publish to the event aggregator for loose coupling
        eventAggregator?.Publish(new ThemeChangedEvent(isDark));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ThemeService"/>
    /// and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // Unsubscribe from event aggregator
                settingsSubscription?.Dispose();
            }

            disposed = true;
        }
    }

    /// <summary>
    /// Handles settings changed events to update theme based on settings.
    /// </summary>
    /// <param name="eventData">The settings changed event data.</param>
    private void OnSettingsChanged(SettingsChangedEvent eventData)
    {
        if (settingsService is null)
        {
            return;
        }

        // Update theme based on the new settings
        var newTheme = settingsService.Current.General.Theme;
        var shouldBeDark = newTheme == "Dark";

        if (shouldBeDark != isDarkTheme)
        {
            // Ensure UI operations happen on the UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SetTheme(shouldBeDark);
            });
        }
    }
}