// <copyright file="PopOutWindowFunctionalTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using Avalonia.Controls;
using Avalonia.Headless.NUnit;

using BadMango.Emulator.UI.Abstractions;
using BadMango.Emulator.UI.Models;
using BadMango.Emulator.UI.ViewModels;
using BadMango.Emulator.UI.Views;

/// <summary>
/// Functional tests for the PopOutWindow view.
/// </summary>
[TestFixture]
public class PopOutWindowFunctionalTests
{
    /// <summary>
    /// Tests that PopOutWindow can be created with a ViewModel.
    /// </summary>
    [AvaloniaTest]
    public void PopOutWindow_CanBeCreatedWithViewModel()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);

        // Act
        var window = new PopOutWindow(viewModel);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(window, Is.Not.Null);
            Assert.That(window.DataContext, Is.EqualTo(viewModel));
            Assert.That(window.ComponentType, Is.EqualTo(PopOutComponent.VideoDisplay));
        });
    }

    /// <summary>
    /// Tests that PopOutWindow has correct default dimensions.
    /// </summary>
    [AvaloniaTest]
    public void PopOutWindow_HasCorrectDefaultDimensions()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);

        // Act
        var window = new PopOutWindow(viewModel);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(window.Width, Is.EqualTo(800));
            Assert.That(window.Height, Is.EqualTo(600));
            Assert.That(window.MinWidth, Is.EqualTo(400));
            Assert.That(window.MinHeight, Is.EqualTo(300));
        });
    }

    /// <summary>
    /// Tests that PopOutWindow has correct title binding from ViewModel.
    /// </summary>
    [AvaloniaTest]
    public void PopOutWindow_TitleBindsCorrectly()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.DebugConsole);

        // Act
        var window = new PopOutWindow(viewModel);

        // Assert
        Assert.That(window.Title, Is.EqualTo("Debug Console"));
    }

    /// <summary>
    /// Tests that PopOutWindow has unique WindowId.
    /// </summary>
    [AvaloniaTest]
    public void PopOutWindow_HasUniqueWindowId()
    {
        // Arrange
        var viewModel1 = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);
        var viewModel2 = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);

        // Act
        var window1 = new PopOutWindow(viewModel1);
        var window2 = new PopOutWindow(viewModel2);

        // Assert
        Assert.That(window1.WindowId, Is.Not.EqualTo(window2.WindowId));
    }

    /// <summary>
    /// Tests that PopOutWindow MachineId is set from ViewModel.
    /// </summary>
    [AvaloniaTest]
    public void PopOutWindow_MachineIdSetFromViewModel()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay, "test-machine");

        // Act
        var window = new PopOutWindow(viewModel);

        // Assert
        Assert.That(window.MachineId, Is.EqualTo("test-machine"));
    }

    /// <summary>
    /// Tests that PopOutWindow contains expected DockPanel structure.
    /// </summary>
    [AvaloniaTest]
    public void PopOutWindow_HasDockPanelStructure()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);

        // Act
        var window = new PopOutWindow(viewModel);

        // Assert
        var dockPanel = window.Content as DockPanel;
        Assert.That(dockPanel, Is.Not.Null);
    }

    /// <summary>
    /// Tests that PopOutWindow GetStateInfo returns correct component type.
    /// </summary>
    [AvaloniaTest]
    public void PopOutWindow_GetStateInfo_ReturnsCorrectComponentType()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.HexEditor);
        var window = new PopOutWindow(viewModel);

        // Act
        var stateInfo = window.GetStateInfo();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(stateInfo.ComponentType, Is.EqualTo(PopOutComponent.HexEditor));
            Assert.That(stateInfo.IsPopOut, Is.True);
        });
    }

    /// <summary>
    /// Tests that PopOutWindow GetStateInfo captures window size.
    /// </summary>
    [AvaloniaTest]
    public void PopOutWindow_GetStateInfo_CapturesWindowSize()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);
        var window = new PopOutWindow(viewModel);
        window.Width = 1024;
        window.Height = 768;

        // Act
        var stateInfo = window.GetStateInfo();

        // Assert
        Assert.That(stateInfo.Size, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(stateInfo.Size!.Value.Width, Is.EqualTo(1024));
            Assert.That(stateInfo.Size!.Value.Height, Is.EqualTo(768));
        });
    }

    /// <summary>
    /// Tests that PopOutWindow RestoreState applies position.
    /// </summary>
    [AvaloniaTest]
    public void PopOutWindow_RestoreState_AppliesPosition()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);
        var window = new PopOutWindow(viewModel);
        var stateInfo = new WindowStateInfo
        {
            ComponentType = PopOutComponent.VideoDisplay,
            IsPopOut = true,
            Position = new Avalonia.Point(100, 200),
        };

        // Act
        window.RestoreState(stateInfo);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(window.Position.X, Is.EqualTo(100));
            Assert.That(window.Position.Y, Is.EqualTo(200));
        });
    }

    /// <summary>
    /// Tests that PopOutWindow RestoreState applies size.
    /// </summary>
    [AvaloniaTest]
    public void PopOutWindow_RestoreState_AppliesSize()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);
        var window = new PopOutWindow(viewModel);
        var stateInfo = new WindowStateInfo
        {
            ComponentType = PopOutComponent.VideoDisplay,
            IsPopOut = true,
            Size = new Avalonia.Size(1024, 768),
        };

        // Act
        window.RestoreState(stateInfo);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(window.Width, Is.EqualTo(1024));
            Assert.That(window.Height, Is.EqualTo(768));
        });
    }

    /// <summary>
    /// Tests that PopOutWindow RestoreState throws on null stateInfo.
    /// </summary>
    [AvaloniaTest]
    public void PopOutWindow_RestoreState_ThrowsOnNull()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);
        var window = new PopOutWindow(viewModel);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => window.RestoreState(null!));
    }

    /// <summary>
    /// Tests that PopOutWindow can be created for each component type.
    /// </summary>
    /// <param name="component">The component type to test.</param>
    [AvaloniaTest]
    [TestCase(PopOutComponent.VideoDisplay)]
    [TestCase(PopOutComponent.DebugConsole)]
    [TestCase(PopOutComponent.AssemblyEditor)]
    [TestCase(PopOutComponent.HexEditor)]
    public void PopOutWindow_CanBeCreatedForAllComponents(PopOutComponent component)
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(component);

        // Act
        var window = new PopOutWindow(viewModel);

        // Assert
        Assert.That(window.ComponentType, Is.EqualTo(component));
    }
}