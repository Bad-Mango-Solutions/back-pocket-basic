// <copyright file="PopOutWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Views;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

using BadMango.Emulator.UI.Models;
using BadMango.Emulator.UI.Services;
using BadMango.Emulator.UI.ViewModels;

/// <summary>
/// Code-behind for the PopOutWindow view that implements IPopOutWindow.
/// </summary>
public partial class PopOutWindow : Window, IPopOutWindow
{
    private readonly TaskCompletionSource<bool> closingTaskSource = new();
    private bool dockOnClose;

    /// <summary>
    /// Initializes a new instance of the <see cref="PopOutWindow"/> class.
    /// </summary>
    public PopOutWindow()
    {
        InitializeComponent();
        WindowId = Guid.NewGuid().ToString();

        // Set up keyboard shortcuts
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PopOutWindow"/> class with a view model.
    /// </summary>
    /// <param name="viewModel">The view model for the window.</param>
    public PopOutWindow(PopOutWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        ComponentType = viewModel.ComponentType;
        MachineId = viewModel.MachineId;
        viewModel.DockRequested += OnDockRequested;
    }

    /// <inheritdoc />
    public string WindowId { get; }

    /// <inheritdoc />
    public PopOutComponent ComponentType { get; private set; }

    /// <inheritdoc />
    public string? MachineId { get; set; }

    /// <inheritdoc cref="IPopOutWindow.Title" />
    string? IPopOutWindow.Title => Title;

    /// <inheritdoc cref="IPopOutWindow.State" />
    WindowState IPopOutWindow.State => WindowState;

    /// <inheritdoc />
    public void BringToFront()
    {
        Activate();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
    }

    /// <inheritdoc />
    public Task CloseAsync(bool dockContent = false)
    {
        dockOnClose = dockContent;
        Close();
        return closingTaskSource.Task;
    }

    /// <inheritdoc />
    public WindowStateInfo GetStateInfo()
    {
        return new()
        {
            ComponentType = ComponentType,
            IsPopOut = true,
            Position = new Point(Position.X, Position.Y),
            Size = new Size(Width, Height),
            MonitorId = GetCurrentMonitorId(),
            IsMaximized = WindowState == WindowState.Maximized,
            MachineProfileId = MachineId,
        };
    }

    /// <inheritdoc />
    public void RestoreState(WindowStateInfo stateInfo)
    {
        ArgumentNullException.ThrowIfNull(stateInfo);

        if (stateInfo.Position is { } position)
        {
            Position = new((int)position.X, (int)position.Y);
        }

        if (stateInfo.Size is { } size)
        {
            Width = size.Width;
            Height = size.Height;
        }

        if (stateInfo.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        // Unregister event handlers to prevent memory leaks
        KeyDown -= OnKeyDown;

        if (DataContext is PopOutWindowViewModel viewModel)
        {
            viewModel.DockRequested -= OnDockRequested;
        }

        base.OnClosed(e);
        closingTaskSource.TrySetResult(dockOnClose);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+Shift+D to dock back to main window
        if (e.Key == Key.D && e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            dockOnClose = true;
            Close();
            e.Handled = true;
        }
    }

    private void OnDockRequested(object? sender, EventArgs e)
    {
        dockOnClose = true;
        Close();
    }

    private string? GetCurrentMonitorId()
    {
        var screens = Screens;
        if (screens is null)
        {
            return null;
        }

        var pos = Position;
        var currentScreen = screens.All.FirstOrDefault(s => s.Bounds.Contains(pos));
        return currentScreen?.DisplayName;
    }
}