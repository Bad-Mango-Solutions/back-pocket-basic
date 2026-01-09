// <copyright file="PopOutWindowViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for a pop-out window.
/// </summary>
public partial class PopOutWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    [ObservableProperty]
    private string title = "Pop-Out Window";

    /// <summary>
    /// Gets or sets the component type displayed in this window.
    /// </summary>
    [ObservableProperty]
    private PopOutComponent componentType;

    /// <summary>
    /// Gets or sets the associated machine ID.
    /// </summary>
    [ObservableProperty]
    private string? machineId;

    /// <summary>
    /// Gets or sets the content view model.
    /// </summary>
    [ObservableProperty]
    private ViewModelBase? contentViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="PopOutWindowViewModel"/> class.
    /// </summary>
    /// <param name="componentType">The component type to display.</param>
    /// <param name="machineId">Optional machine ID for machine-specific windows.</param>
    public PopOutWindowViewModel(PopOutComponent componentType, string? machineId = null)
    {
        this.ComponentType = componentType;
        this.MachineId = machineId;
        this.Title = GetWindowTitle(componentType, machineId);
        this.ContentViewModel = CreateContentViewModel(componentType);
    }

    /// <summary>
    /// Event raised when the user requests to dock the window back to the main window.
    /// </summary>
    public event EventHandler? DockRequested;

    private static string GetWindowTitle(PopOutComponent componentType, string? machineId)
    {
        var componentName = componentType switch
        {
            PopOutComponent.VideoDisplay => "Video Display",
            PopOutComponent.DebugConsole => "Debug Console",
            PopOutComponent.AssemblyEditor => "Assembly Editor",
            PopOutComponent.HexEditor => "Hex Editor",
            _ => "Pop-Out Window",
        };

        return machineId is not null
            ? $"{componentName} - {machineId}"
            : componentName;
    }

    private static ViewModelBase? CreateContentViewModel(PopOutComponent componentType)
    {
        // Create a placeholder view model for now
        // In a full implementation, this would create the appropriate content view model
        return componentType switch
        {
            PopOutComponent.VideoDisplay => new("Video Display", "Video display content will be rendered here."),
            PopOutComponent.DebugConsole => new("Debug Console", "Debug console content will be rendered here."),
            PopOutComponent.AssemblyEditor => new("Assembly Editor", "Assembly editor content will be rendered here."),
            PopOutComponent.HexEditor => new PlaceholderViewModel("Hex Editor", "Hex editor content will be rendered here."),
            _ => null,
        };
    }

    /// <summary>
    /// Command to dock the window back to the main window.
    /// </summary>
    [RelayCommand]
    private void DockToMain()
    {
        DockRequested?.Invoke(this, EventArgs.Empty);
    }
}