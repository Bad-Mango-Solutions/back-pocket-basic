// <copyright file="DebugWindowManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Services;

using System.Collections.Concurrent;

using Avalonia.Controls;
using Avalonia.Threading;

using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.UI.Views;

/// <summary>
/// Manages debug popup windows for the console debugger REPL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation handles the Avalonia threading model by dispatching all UI
/// operations to the Avalonia UI thread. Windows are tracked in a thread-safe
/// dictionary to support the non-blocking async pattern required by the REPL.
/// </para>
/// <para>
/// The manager automatically initializes Avalonia on a background thread when
/// a window is requested, allowing the console REPL to continue running on the
/// main thread while UI windows are managed separately.
/// </para>
/// <para>
/// The manager supports creating different types of debug windows based on a
/// string identifier, making it extensible for future window types like video
/// displays, text editors, and memory viewers.
/// </para>
/// </remarks>
/// <seealso href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/video/Pocket2e%20Debug%20Video%20Window%20(Avalonia)%20%E2%80%94%20Specification.md#8-threading-model">
/// Threading Model Specification
/// </seealso>
public class DebugWindowManager : IDebugWindowManager
{
    private static readonly string[] AvailableTypes = [nameof(DebugWindowComponent.About), "CharacterPreview"];

    private readonly ConcurrentDictionary<string, Window> openWindows = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool IsAvaloniaRunning => AvaloniaBootstrapper.IsRunning;

    /// <inheritdoc />
    public async Task<bool> ShowWindowAsync(string windowType)
    {
        // Ensure Avalonia is initialized
        AvaloniaBootstrapper.EnsureInitialized();

        if (!this.IsAvaloniaRunning)
        {
            return false;
        }

        // If window is already open, bring it to front
        if (this.openWindows.TryGetValue(windowType, out var existingWindow))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                existingWindow.Activate();
                if (existingWindow.WindowState == WindowState.Minimized)
                {
                    existingWindow.WindowState = WindowState.Normal;
                }
            });
            return true;
        }

        // Create the window on the UI thread
        var window = await Dispatcher.UIThread.InvokeAsync(() => this.CreateWindow(windowType));
        if (window is null)
        {
            return false;
        }

        // Track the window and handle its closing
        this.openWindows[windowType] = window;
        window.Closed += (_, _) => this.openWindows.TryRemove(windowType, out _);

        // Show the window
        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CloseWindowAsync(string windowType)
    {
        if (!this.openWindows.TryRemove(windowType, out var window))
        {
            return false;
        }

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());
        return true;
    }

    /// <inheritdoc />
    public bool IsWindowOpen(string windowType)
    {
        return this.openWindows.ContainsKey(windowType);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAvailableWindowTypes()
    {
        return AvailableTypes;
    }

    /// <inheritdoc />
    public async Task CloseAllWindowsAsync()
    {
        var windowsToClose = this.openWindows.Values.ToList();
        this.openWindows.Clear();

        foreach (var window in windowsToClose)
        {
            await Dispatcher.UIThread.InvokeAsync(() => window.Close());
        }
    }

    /// <summary>
    /// Creates a window of the specified type.
    /// </summary>
    /// <param name="windowType">The type of window to create.</param>
    /// <returns>The created window, or null if the type is not supported.</returns>
    /// <remarks>
    /// This method must be called on the Avalonia UI thread.
    /// </remarks>
    private Window? CreateWindow(string windowType)
    {
        return windowType.ToUpperInvariant() switch
        {
            "ABOUT" => new AboutWindow(),
            "CHARACTERPREVIEW" => new CharacterPreviewWindow(),
            _ => null,
        };
    }
}