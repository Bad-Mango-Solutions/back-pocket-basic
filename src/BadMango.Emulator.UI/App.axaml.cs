// <copyright file="App.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using BadMango.Emulator.UI.ViewModels;
using BadMango.Emulator.UI.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Avalonia application class for the BackPocket emulator UI.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets or sets the application host for dependency injection and configuration.
    /// </summary>
    public static IHost? AppHost { get; set; }

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        // Remove Avalonia's data validation to avoid duplicate validations with CommunityToolkit.Mvvm
        // Check if validators exist before removing (supports headless testing scenarios)
        if (BindingPlugins.DataValidators.Count > 0)
        {
            BindingPlugins.DataValidators.RemoveAt(0);
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = AppHost?.Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}