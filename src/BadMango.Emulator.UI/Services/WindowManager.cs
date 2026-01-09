// <copyright file="WindowManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Services;

using System.Collections.Concurrent;
using System.Text.Json;

using BadMango.Emulator.UI.Models;

using Microsoft.Extensions.Logging;

/// <summary>
/// Default implementation of <see cref="IWindowManager"/> for managing pop-out windows.
/// </summary>
public class WindowManager : IWindowManager
{
    /// <summary>
    /// The application name used for storage paths.
    /// </summary>
    private const string ApplicationName = "BackPocket";

    private readonly ConcurrentDictionary<string, IPopOutWindow> windows = new();
    private readonly Func<PopOutComponent, string?, IPopOutWindow> windowFactory;
    private readonly ILogger<WindowManager>? logger;
    private readonly string layoutStoragePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowManager"/> class.
    /// </summary>
    /// <param name="windowFactory">Factory function to create pop-out windows.</param>
    /// <param name="logger">Optional logger for window management operations.</param>
    /// <param name="layoutStoragePath">Optional path for storing layout state.</param>
    public WindowManager(
        Func<PopOutComponent, string?, IPopOutWindow> windowFactory,
        ILogger<WindowManager>? logger = null,
        string? layoutStoragePath = null)
    {
        this.windowFactory = windowFactory;
        this.logger = logger;
        this.layoutStoragePath = layoutStoragePath ?? GetDefaultLayoutStoragePath();
    }

    /// <inheritdoc />
    public event EventHandler<PopOutWindowEventArgs>? WindowCreated;

    /// <inheritdoc />
    public event EventHandler<PopOutWindowEventArgs>? WindowClosed;

    /// <inheritdoc />
    public IReadOnlyList<IPopOutWindow> PopOutWindows => [.. windows.Values];

    /// <inheritdoc />
    public Task<IPopOutWindow> CreatePopOutAsync(PopOutComponent component, string? machineId = null)
    {
        logger?.LogInformation("Creating pop-out window for component {Component}, machine {MachineId}", component, machineId);

        var window = windowFactory(component, machineId);
        windows[window.WindowId] = window;

        WindowCreated?.Invoke(this, new(window));

        logger?.LogInformation("Created pop-out window {WindowId} for component {Component}", window.WindowId, component);

        return Task.FromResult(window);
    }

    /// <inheritdoc />
    public async Task DockWindowAsync(IPopOutWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        logger?.LogInformation("Docking window {WindowId}", window.WindowId);

        await window.CloseAsync(dockContent: true).ConfigureAwait(false);
        windows.TryRemove(window.WindowId, out _);

        WindowClosed?.Invoke(this, new(window));

        logger?.LogInformation("Docked window {WindowId}", window.WindowId);
    }

    /// <inheritdoc />
    public async Task RestoreWindowStatesAsync(string profileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        logger?.LogInformation("Restoring window states for profile {ProfileId}", profileId);

        var layoutPath = GetLayoutPath(profileId);
        if (!File.Exists(layoutPath))
        {
            logger?.LogDebug("No saved layout found for profile {ProfileId}", profileId);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(layoutPath).ConfigureAwait(false);
            var layout = JsonSerializer.Deserialize<WindowLayoutState>(json, GetJsonOptions());

            if (layout?.Windows is not null)
            {
                foreach (var windowState in layout.Windows.Where(w => w.IsPopOut))
                {
                    var window = await CreatePopOutAsync(windowState.ComponentType, windowState.MachineProfileId).ConfigureAwait(false);
                    window.RestoreState(windowState);
                }

                logger?.LogInformation("Restored {Count} pop-out windows for profile {ProfileId}", layout.Windows.Count, profileId);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error restoring window states for profile {ProfileId}", profileId);
        }
    }

    /// <inheritdoc />
    public async Task SaveWindowStatesAsync(string profileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        logger?.LogInformation("Saving window states for profile {ProfileId}", profileId);

        var windowStates = windows.Values.Select(w => w.GetStateInfo()).ToList();

        var layout = new WindowLayoutState
        {
            Version = 1,
            Windows = windowStates,
        };

        try
        {
            var layoutPath = GetLayoutPath(profileId);
            var directory = Path.GetDirectoryName(layoutPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(layout, GetJsonOptions());
            await File.WriteAllTextAsync(layoutPath, json).ConfigureAwait(false);

            logger?.LogInformation("Saved {Count} window states for profile {ProfileId}", windowStates.Count, profileId);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error saving window states for profile {ProfileId}", profileId);
        }
    }

    /// <inheritdoc />
    public async Task CloseAllWindowsAsync()
    {
        logger?.LogInformation("Closing all {Count} pop-out windows", windows.Count);

        var windowsToClose = windows.Values.ToList();
        foreach (var window in windowsToClose)
        {
            try
            {
                await window.CloseAsync(dockContent: false).ConfigureAwait(false);
                windows.TryRemove(window.WindowId, out _);
                WindowClosed?.Invoke(this, new(window));
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error closing window {WindowId}", window.WindowId);
            }
        }

        logger?.LogInformation("Closed all pop-out windows");
    }

    /// <inheritdoc />
    public IPopOutWindow? FindWindow(PopOutComponent component, string? machineId = null)
    {
        return windows.Values.FirstOrDefault(w =>
            w.ComponentType == component &&
            (machineId is null || w.MachineId == machineId));
    }

    private static string GetDefaultLayoutStoragePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, ApplicationName, "layouts");
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    private string GetLayoutPath(string profileId)
    {
        return Path.Combine(layoutStoragePath, $"layout-{profileId}.json");
    }
}