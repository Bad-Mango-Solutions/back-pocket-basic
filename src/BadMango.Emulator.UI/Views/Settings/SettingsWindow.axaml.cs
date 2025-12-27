// <copyright file="SettingsWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Views.Settings;

using Avalonia.Controls;
using Avalonia.Interactivity;

using BadMango.Emulator.UI.ViewModels.Settings;

/// <summary>
/// Settings window for configuring emulator options.
/// </summary>
public partial class SettingsWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
    /// </summary>
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private async void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsWindowViewModel vm)
        {
            await vm.ApplyCommand.ExecuteAsync(null);
        }

        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}