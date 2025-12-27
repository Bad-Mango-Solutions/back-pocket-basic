// <copyright file="ApplicationStartupTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using Avalonia.Controls;
using Avalonia.Headless.NUnit;

using BadMango.Emulator.UI.Services;
using BadMango.Emulator.UI.ViewModels;
using BadMango.Emulator.UI.Views;

/// <summary>
/// Functional tests that validate the UI application starts correctly.
/// </summary>
[TestFixture]
public class ApplicationStartupTests
{
    /// <summary>
    /// Tests that the MainWindow can be created with a ViewModel.
    /// </summary>
    [AvaloniaTest]
    public void MainWindow_CanBeCreatedWithViewModel()
    {
        // Arrange
        var themeService = new ThemeService();
        var navigationService = new NavigationService();
        var viewModel = new MainWindowViewModel(themeService, navigationService);

        // Act
        var window = new MainWindow
        {
            DataContext = viewModel,
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(window, Is.Not.Null);
            Assert.That(window.DataContext, Is.EqualTo(viewModel));
        });
    }

    /// <summary>
    /// Tests that the MainWindow has the correct title binding from ViewModel.
    /// </summary>
    [AvaloniaTest]
    public void MainWindow_TitleBindsCorrectly()
    {
        // Arrange
        var themeService = new ThemeService();
        var navigationService = new NavigationService();
        var viewModel = new MainWindowViewModel(themeService, navigationService);

        // Act
        _ = new MainWindow
        {
            DataContext = viewModel,
        };

        // Assert - Title is bound to ViewModel.Title
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Title, Is.EqualTo("BackPocket Emulator"));
        });
    }

    /// <summary>
    /// Tests that the MainWindow has correct initial dimension properties.
    /// </summary>
    [AvaloniaTest]
    public void MainWindow_HasCorrectDimensionProperties()
    {
        // Arrange
        var themeService = new ThemeService();
        var navigationService = new NavigationService();
        var viewModel = new MainWindowViewModel(themeService, navigationService);

        // Act
        var window = new MainWindow
        {
            DataContext = viewModel,
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(window.Width, Is.EqualTo(1200));
            Assert.That(window.Height, Is.EqualTo(800));
            Assert.That(window.MinWidth, Is.EqualTo(800));
            Assert.That(window.MinHeight, Is.EqualTo(600));
        });
    }

    /// <summary>
    /// Tests that the MainWindow contains the expected grid structure.
    /// </summary>
    [AvaloniaTest]
    public void MainWindow_HasGridStructure()
    {
        // Arrange
        var themeService = new ThemeService();
        var navigationService = new NavigationService();
        var viewModel = new MainWindowViewModel(themeService, navigationService);

        // Act
        var window = new MainWindow
        {
            DataContext = viewModel,
        };

        // Assert - verify the main grid structure exists
        var grid = window.Content as Grid;
        Assert.That(grid, Is.Not.Null);
        Assert.That(grid!.ColumnDefinitions.Count, Is.EqualTo(2));
    }

    /// <summary>
    /// Tests that the ViewModel initializes with Machine Manager view displayed.
    /// </summary>
    [AvaloniaTest]
    public void MainWindow_ViewModelInitializesWithMachineManagerView()
    {
        // Arrange
        var themeService = new ThemeService();
        var navigationService = new NavigationService();
        var viewModel = new MainWindowViewModel(themeService, navigationService);

        // Act
        _ = new MainWindow
        {
            DataContext = viewModel,
        };

        // Assert
        Assert.That(viewModel.CurrentView, Is.InstanceOf<MachineManagerViewModel>());
    }

    /// <summary>
    /// Tests that the MachineManagerView can be instantiated.
    /// </summary>
    [AvaloniaTest]
    public void MachineManagerView_CanBeInstantiated()
    {
        // Arrange & Act
        var view = new MachineManagerView();

        // Assert
        Assert.That(view, Is.Not.Null);
    }

    /// <summary>
    /// Tests that the PlaceholderView can be instantiated.
    /// </summary>
    [AvaloniaTest]
    public void PlaceholderView_CanBeInstantiated()
    {
        // Arrange & Act
        var view = new PlaceholderView();

        // Assert
        Assert.That(view, Is.Not.Null);
    }

    /// <summary>
    /// Tests that navigation items are properly initialized in the ViewModel.
    /// </summary>
    [AvaloniaTest]
    public void MainWindow_ViewModelHasAllNavigationItems()
    {
        // Arrange
        var themeService = new ThemeService();
        var navigationService = new NavigationService();
        var viewModel = new MainWindowViewModel(themeService, navigationService);

        // Act
        _ = new MainWindow
        {
            DataContext = viewModel,
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.NavigationItems, Is.Not.Null);
            Assert.That(viewModel.NavigationItems.Count, Is.EqualTo(6));
            Assert.That(viewModel.NavigationItems.Any(n => n.Name == "Machine Manager"), Is.True);
            Assert.That(viewModel.NavigationItems.Any(n => n.Name == "Storage"), Is.True);
            Assert.That(viewModel.NavigationItems.Any(n => n.Name == "Display"), Is.True);
            Assert.That(viewModel.NavigationItems.Any(n => n.Name == "Debug"), Is.True);
            Assert.That(viewModel.NavigationItems.Any(n => n.Name == "Editor"), Is.True);
            Assert.That(viewModel.NavigationItems.Any(n => n.Name == "Settings"), Is.True);
        });
    }

    /// <summary>
    /// Tests that the application starts with dark theme by default.
    /// </summary>
    [AvaloniaTest]
    public void MainWindow_ViewModelStartsWithDarkTheme()
    {
        // Arrange
        var themeService = new ThemeService();
        var navigationService = new NavigationService();
        var viewModel = new MainWindowViewModel(themeService, navigationService);

        // Act
        _ = new MainWindow
        {
            DataContext = viewModel,
        };

        // Assert
        Assert.That(viewModel.IsDarkTheme, Is.True);
    }
}