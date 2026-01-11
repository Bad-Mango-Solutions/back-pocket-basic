// <copyright file="DebugWindowManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Services;

using System.Collections.Concurrent;

using Avalonia.Controls;
using Avalonia.Threading;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.Infrastructure.Commands;
using BadMango.Emulator.Debug.UI.StatusMonitor;
using BadMango.Emulator.Debug.UI.Views;
using BadMango.Emulator.Devices.Interfaces;
using BadMango.Emulator.TextEditor;

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
    private static readonly string[] AvailableTypes =
    [
        nameof(DebugWindowComponent.About),
        nameof(DebugWindowComponent.CharacterPreview),
        nameof(DebugWindowComponent.StatusMonitor),
        nameof(DebugWindowComponent.TextEditor),
        nameof(DebugWindowComponent.Video),
    ];

    private readonly ConcurrentDictionary<string, Window> openWindows = new(StringComparer.OrdinalIgnoreCase);
    private int textEditorCounter;

    /// <inheritdoc />
    public bool IsAvaloniaRunning => AvaloniaBootstrapper.IsRunning;

    /// <inheritdoc />
    public Task<bool> ShowWindowAsync(string windowType)
    {
        return this.ShowWindowAsync(windowType, null);
    }

    /// <inheritdoc />
    public async Task<bool> ShowWindowAsync(string windowType, object? context)
    {
        // Ensure Avalonia is initialized
        AvaloniaBootstrapper.EnsureInitialized();

        if (!this.IsAvaloniaRunning)
        {
            return false;
        }

        // TextEditor windows support multiple instances
        if (windowType.Equals("TextEditor", StringComparison.OrdinalIgnoreCase))
        {
            return await this.ShowTextEditorWindowAsync(context);
        }

        // If window is already open, apply context and bring it to front
        if (this.openWindows.TryGetValue(windowType, out var existingWindow))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Apply context to existing window
                ApplyContextToWindow(existingWindow, context);

                existingWindow.Activate();
                if (existingWindow.WindowState == WindowState.Minimized)
                {
                    existingWindow.WindowState = WindowState.Normal;
                }
            });
            return true;
        }

        // Create the window on the UI thread
        var window = await Dispatcher.UIThread.InvokeAsync(() => this.CreateWindow(windowType, context));
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

    private static CharacterPreviewWindow CreateCharacterPreviewWindow(object? context)
    {
        var window = new CharacterPreviewWindow();

        // If context is ICharacterRomProvider (e.g., IVideoDevice), set it on the window
        if (context is ICharacterRomProvider provider)
        {
            window.SetCharacterRomProvider(provider);
        }

        return window;
    }

    private static StatusMonitorWindow CreateStatusMonitorWindow(object? context)
    {
        var window = new StatusMonitorWindow();

        // If context is IMachine, create a stats provider for it
        if (context is IMachine machine)
        {
            var statsProvider = new MachineStatsProvider(machine);

            // Look for device extensions to register
            var clockDevice = machine.GetComponent<IClockDevice>();
            if (clockDevice != null)
            {
                var pocketWatchExtension = new PocketWatchStatusExtension(clockDevice);
                statsProvider.RegisterExtension(pocketWatchExtension);
            }

            window.SetStatsProvider(statsProvider);
        }

        return window;
    }

    private static TextEditorWindow CreateTextEditorWindow(object? context)
    {
        var window = new TextEditorWindow();

        // If context is a file path string, open the file
        if (context is string filePath && !string.IsNullOrWhiteSpace(filePath))
        {
            // Schedule the file open to happen after the window is shown
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await window.OpenFileAsync(filePath);
            });
        }

        return window;
    }

    private static VideoWindow CreateVideoWindow(object? context)
    {
        var window = new VideoWindow();

        // If context is IMachine, attach it to the window
        if (context is IMachine machine)
        {
            window.AttachMachine(machine);
        }

        // Apply VideoWindowContext settings if provided
        ApplyVideoWindowContext(window, context);

        return window;
    }

    /// <summary>
    /// Applies context data to an existing window.
    /// </summary>
    /// <param name="window">The window to apply context to.</param>
    /// <param name="context">The context data.</param>
    private static void ApplyContextToWindow(Window window, object? context)
    {
        if (window is VideoWindow videoWindow)
        {
            ApplyVideoWindowContext(videoWindow, context);
        }
    }

    /// <summary>
    /// Applies VideoWindowContext settings to a VideoWindow.
    /// </summary>
    /// <param name="window">The video window.</param>
    /// <param name="context">The context data (may be VideoWindowContext or IMachine).</param>
    private static void ApplyVideoWindowContext(VideoWindow window, object? context)
    {
        if (context is not VideoWindowContext videoContext)
        {
            return;
        }

        // Apply scale if specified
        if (videoContext.Scale.HasValue)
        {
            window.Scale = videoContext.Scale.Value;
        }

        // Apply color mode if specified
        if (videoContext.ColorMode.HasValue)
        {
            window.ColorMode = videoContext.ColorMode.Value;
        }

        // Toggle or set FPS display
        if (videoContext.ToggleFps)
        {
            window.ShowFps = videoContext.ShowFps ?? !window.ShowFps;
        }
        else if (videoContext.ShowFps.HasValue)
        {
            window.ShowFps = videoContext.ShowFps.Value;
        }

        // Force refresh if requested
        if (videoContext.ForceRefresh)
        {
            window.ForceRedraw();
        }
    }

    /// <summary>
    /// Shows a new text editor window, allowing multiple instances.
    /// </summary>
    /// <param name="context">Optional file path to open.</param>
    /// <returns>True if the window was shown; otherwise, false.</returns>
    private async Task<bool> ShowTextEditorWindowAsync(object? context)
    {
        var windowId = $"TextEditor_{Interlocked.Increment(ref this.textEditorCounter)}";

        var window = await Dispatcher.UIThread.InvokeAsync(() => CreateTextEditorWindow(context));
        if (window is null)
        {
            return false;
        }

        // Track the window and handle its closing
        this.openWindows[windowId] = window;
        window.Closed += (_, _) => this.openWindows.TryRemove(windowId, out _);

        // Show the window
        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        return true;
    }

    /// <summary>
    /// Creates a window of the specified type.
    /// </summary>
    /// <param name="windowType">The type of window to create.</param>
    /// <param name="context">Optional context data to pass to the window.</param>
    /// <returns>The created window, or null if the type is not supported.</returns>
    /// <remarks>
    /// This method must be called on the Avalonia UI thread.
    /// </remarks>
    private Window? CreateWindow(string windowType, object? context)
    {
        return windowType.ToUpperInvariant() switch
        {
            "ABOUT" => new AboutWindow(),
            "CHARACTERPREVIEW" => CreateCharacterPreviewWindow(context),
            "STATUSMONITOR" => CreateStatusMonitorWindow(context),
            "TEXTEDITOR" => CreateTextEditorWindow(context),
            "VIDEO" => CreateVideoWindow(context),
            _ => null,
        };
    }
}