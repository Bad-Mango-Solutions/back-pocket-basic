// <copyright file="PopOutWindowViewModelTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using BadMango.Emulator.UI.Abstractions;
using BadMango.Emulator.UI.ViewModels;

/// <summary>
/// Tests for <see cref="PopOutWindowViewModel"/>.
/// </summary>
[TestFixture]
public class PopOutWindowViewModelTests
{
    /// <summary>
    /// Tests that the ViewModel initializes with the correct component type.
    /// </summary>
    [Test]
    public void Constructor_InitializesComponentType()
    {
        // Arrange & Act
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);

        // Assert
        Assert.That(viewModel.ComponentType, Is.EqualTo(PopOutComponent.VideoDisplay));
    }

    /// <summary>
    /// Tests that the ViewModel initializes with the correct machine ID.
    /// </summary>
    [Test]
    public void Constructor_InitializesMachineId()
    {
        // Arrange & Act
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay, "machine-1");

        // Assert
        Assert.That(viewModel.MachineId, Is.EqualTo("machine-1"));
    }

    /// <summary>
    /// Tests that the title includes the component name.
    /// </summary>
    /// <param name="component">The component type to test.</param>
    /// <param name="expectedTitle">The expected window title.</param>
    [TestCase(PopOutComponent.VideoDisplay, "Video Display")]
    [TestCase(PopOutComponent.DebugConsole, "Debug Console")]
    [TestCase(PopOutComponent.AssemblyEditor, "Assembly Editor")]
    [TestCase(PopOutComponent.HexEditor, "Hex Editor")]
    public void Constructor_SetsTitleBasedOnComponent(PopOutComponent component, string expectedTitle)
    {
        // Arrange & Act
        var viewModel = new PopOutWindowViewModel(component);

        // Assert
        Assert.That(viewModel.Title, Is.EqualTo(expectedTitle));
    }

    /// <summary>
    /// Tests that the title includes the machine ID when provided.
    /// </summary>
    [Test]
    public void Constructor_WithMachineId_IncludesMachineIdInTitle()
    {
        // Arrange & Act
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay, "My Apple IIe");

        // Assert
        Assert.That(viewModel.Title, Is.EqualTo("Video Display - My Apple IIe"));
    }

    /// <summary>
    /// Tests that the content view model is created.
    /// </summary>
    [Test]
    public void Constructor_CreatesContentViewModel()
    {
        // Arrange & Act
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);

        // Assert
        Assert.That(viewModel.ContentViewModel, Is.Not.Null);
        Assert.That(viewModel.ContentViewModel, Is.InstanceOf<PlaceholderViewModel>());
    }

    /// <summary>
    /// Tests that the content view model has the correct title.
    /// </summary>
    [Test]
    public void Constructor_ContentViewModelHasCorrectTitle()
    {
        // Arrange & Act
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);

        // Assert
        var placeholderVm = viewModel.ContentViewModel as PlaceholderViewModel;
        Assert.That(placeholderVm, Is.Not.Null);
        Assert.That(placeholderVm!.Title, Is.EqualTo("Video Display"));
    }

    /// <summary>
    /// Tests that the DockToMainCommand is available.
    /// </summary>
    [Test]
    public void DockToMainCommand_IsAvailable()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);

        // Assert
        Assert.That(viewModel.DockToMainCommand, Is.Not.Null);
        Assert.That(viewModel.DockToMainCommand.CanExecute(null), Is.True);
    }
}