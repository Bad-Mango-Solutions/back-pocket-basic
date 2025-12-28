// <copyright file="SettingsEventBridge.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Services;

using BadMango.Emulator.Configuration.Events;
using BadMango.Emulator.Configuration.Services;
using BadMango.Emulator.Infrastructure.Events;

/// <summary>
/// Bridges settings service events to the event aggregator for loose coupling.
/// This service subscribes to <see cref="ISettingsService.SettingsChanged"/>
/// and publishes <see cref="SettingsChangedEvent"/> to the event aggregator.
/// </summary>
public sealed class SettingsEventBridge : IDisposable
{
    private readonly ISettingsService settingsService;
    private readonly IEventAggregator eventAggregator;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsEventBridge"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service to subscribe to.</param>
    /// <param name="eventAggregator">The event aggregator to publish events to.</param>
    public SettingsEventBridge(ISettingsService settingsService, IEventAggregator eventAggregator)
    {
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        // Subscribe to settings changes
        this.settingsService.SettingsChanged += OnSettingsChanged;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!disposed)
        {
            settingsService.SettingsChanged -= OnSettingsChanged;
            disposed = true;
        }
    }

    /// <summary>
    /// Handles settings changed events and publishes them to the event aggregator.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="args">The settings changed event arguments.</param>
    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs args)
    {
        // Determine which setting changed
        string? settingName = null;
        if (args.ChangedKeys?.Count == 1)
        {
            settingName = args.ChangedKeys[0];
        }
        else if (args.IsFullReload)
        {
            settingName = null; // Full reload
        }

        // Publish to the event aggregator
        eventAggregator.Publish(new SettingsChangedEvent(settingName, null));
    }
}