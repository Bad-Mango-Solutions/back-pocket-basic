// <copyright file="MainWindowViewModelTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using BadMango.Emulator.UI.Abstractions;
using BadMango.Emulator.UI.Services;
using BadMango.Emulator.UI.ViewModels;

using Moq;

/// <summary>
/// Tests for <see cref="MainWindowViewModel"/>.
/// </summary>
[TestFixture]
public class MainWindowViewModelTests
{
    private Mock<IThemeService> mockThemeService = null!;
    private Mock<INavigationService> mockNavigationService = null!;

    /// <summary>
    /// Sets up the test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        mockThemeService = new Mock<IThemeService>();
        mockNavigationService = new Mock<INavigationService>();
    }

    /// <summary>
    /// Tests that the ViewModel initializes with correct default values.
    /// </summary>
    [Test]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel(mockThemeService.Object, mockNavigationService.Object);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Title, Is.EqualTo("BackPocket Emulator"));
            Assert.That(viewModel.IsDarkTheme, Is.True);
            Assert.That(viewModel.NavigationItems, Is.Not.Null);
            Assert.That(viewModel.NavigationItems.Count, Is.EqualTo(6));
            Assert.That(viewModel.CurrentView, Is.Not.Null);
            Assert.That(viewModel.CurrentView, Is.InstanceOf<MachineManagerViewModel>());
        });
    }

    /// <summary>
    /// Tests that navigation items are correctly initialized.
    /// </summary>
    [Test]
    public void NavigationItems_ContainsExpectedItems()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel(mockThemeService.Object, mockNavigationService.Object);

        // Assert
        var itemNames = viewModel.NavigationItems.Select(i => i.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(itemNames, Does.Contain("Machine Manager"));
            Assert.That(itemNames, Does.Contain("Storage"));
            Assert.That(itemNames, Does.Contain("Display"));
            Assert.That(itemNames, Does.Contain("Debug"));
            Assert.That(itemNames, Does.Contain("Editor"));
        });
    }

    /// <summary>
    /// Tests that the first navigation item is selected by default.
    /// </summary>
    [Test]
    public void NavigationItems_FirstItemIsSelected()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel(mockThemeService.Object, mockNavigationService.Object);

        // Assert
        Assert.That(viewModel.NavigationItems[0].IsSelected, Is.True);
    }

    /// <summary>
    /// Tests that ToggleThemeCommand toggles the theme.
    /// </summary>
    [Test]
    public void ToggleThemeCommand_TogglesIsDarkTheme()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(mockThemeService.Object, mockNavigationService.Object);
        Assert.That(viewModel.IsDarkTheme, Is.True);

        // Act
        viewModel.ToggleThemeCommand.Execute(null);

        // Assert
        Assert.That(viewModel.IsDarkTheme, Is.False);
        mockThemeService.Verify(s => s.SetTheme(false), Times.Once);
    }

    /// <summary>
    /// Tests that NavigateCommand changes the current view to MachineManager.
    /// </summary>
    [Test]
    public void NavigateCommand_ToMachineManager_SetsCorrectView()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(mockThemeService.Object, mockNavigationService.Object);

        // Act
        viewModel.NavigateCommand.Execute("Machine Manager");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.CurrentView, Is.InstanceOf<MachineManagerViewModel>());
            Assert.That(viewModel.SelectedNavigationItem, Is.EqualTo("Machine Manager"));
        });
    }

    /// <summary>
    /// Tests that NavigateCommand changes the current view to a placeholder for unimplemented views.
    /// </summary>
    [Test]
    public void NavigateCommand_ToStorage_SetsPlaceholderView()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(mockThemeService.Object, mockNavigationService.Object);

        // Act
        viewModel.NavigateCommand.Execute("Storage");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.CurrentView, Is.InstanceOf<PlaceholderViewModel>());
            Assert.That(viewModel.SelectedNavigationItem, Is.EqualTo("Storage"));
        });
    }

    /// <summary>
    /// Tests that NavigateCommand updates navigation item selection state.
    /// </summary>
    [Test]
    public void NavigateCommand_UpdatesNavigationItemSelection()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(mockThemeService.Object, mockNavigationService.Object);

        // Act
        viewModel.NavigateCommand.Execute("Debug");

        // Assert
        var debugItem = viewModel.NavigationItems.First(i => i.Name == "Debug");
        var machineItem = viewModel.NavigationItems.First(i => i.Name == "Machine Manager");
        Assert.Multiple(() =>
        {
            Assert.That(debugItem.IsSelected, Is.True);
            Assert.That(machineItem.IsSelected, Is.False);
        });
    }
}