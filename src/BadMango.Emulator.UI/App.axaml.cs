// <copyright file="App.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
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
        // Note: In Avalonia 11.x, the recommended CommunityToolkit.Mvvm workaround was to remove
        // the first BindingPlugins.DataValidators entry to avoid duplicate validations. In
        // Avalonia 12, the binding system was rewritten (compiled bindings are now standard) and
        // BindingPlugins was made internal. The workaround is no longer required and removed.
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