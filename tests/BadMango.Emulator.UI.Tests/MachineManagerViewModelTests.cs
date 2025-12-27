// <copyright file="MachineManagerViewModelTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using BadMango.Emulator.UI.ViewModels;

/// <summary>
/// Tests for <see cref="MachineManagerViewModel"/>.
/// </summary>
[TestFixture]
public class MachineManagerViewModelTests
{
    /// <summary>
    /// Tests that the ViewModel initializes with default profiles.
    /// </summary>
    [Test]
    public void Constructor_InitializesWithDefaultProfiles()
    {
        // Arrange & Act
        var viewModel = new MachineManagerViewModel();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Profiles, Is.Not.Null);
            Assert.That(viewModel.Profiles.Count, Is.EqualTo(2));
            Assert.That(viewModel.SelectedProfile, Is.Not.Null);
        });
    }

    /// <summary>
    /// Tests that the ViewModel initializes with empty instances.
    /// </summary>
    [Test]
    public void Constructor_InitializesWithEmptyInstances()
    {
        // Arrange & Act
        var viewModel = new MachineManagerViewModel();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Instances, Is.Not.Null);
            Assert.That(viewModel.Instances.Count, Is.EqualTo(0));
            Assert.That(viewModel.SelectedInstance, Is.Null);
        });
    }

    /// <summary>
    /// Tests that CreateProfileCommand adds a new profile.
    /// </summary>
    [Test]
    public void CreateProfileCommand_AddsNewProfile()
    {
        // Arrange
        var viewModel = new MachineManagerViewModel();
        int initialCount = viewModel.Profiles.Count;

        // Act
        viewModel.CreateProfileCommand.Execute(null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Profiles.Count, Is.EqualTo(initialCount + 1));
            Assert.That(viewModel.SelectedProfile, Is.Not.Null);
            Assert.That(viewModel.SelectedProfile!.Name, Is.EqualTo("New Profile"));
        });
    }

    /// <summary>
    /// Tests that DeleteProfileCommand removes the selected profile.
    /// </summary>
    [Test]
    public void DeleteProfileCommand_RemovesSelectedProfile()
    {
        // Arrange
        var viewModel = new MachineManagerViewModel();
        var profileToDelete = viewModel.SelectedProfile;
        int initialCount = viewModel.Profiles.Count;

        // Act
        viewModel.DeleteProfileCommand.Execute(null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Profiles.Count, Is.EqualTo(initialCount - 1));
            Assert.That(viewModel.Profiles, Does.Not.Contain(profileToDelete));
        });
    }

    /// <summary>
    /// Tests that StartInstanceCommand creates a new instance.
    /// </summary>
    [Test]
    public void StartInstanceCommand_CreatesNewInstance()
    {
        // Arrange
        var viewModel = new MachineManagerViewModel();
        Assert.That(viewModel.SelectedProfile, Is.Not.Null);

        // Act
        viewModel.StartInstanceCommand.Execute(null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Instances.Count, Is.EqualTo(1));
            Assert.That(viewModel.SelectedInstance, Is.Not.Null);
            Assert.That(viewModel.SelectedInstance!.ProfileName, Is.EqualTo(viewModel.SelectedProfile!.Name));
            Assert.That(viewModel.SelectedInstance.Status, Is.EqualTo("Running"));
        });
    }

    /// <summary>
    /// Tests that StopInstanceCommand removes the selected instance.
    /// </summary>
    [Test]
    public void StopInstanceCommand_RemovesSelectedInstance()
    {
        // Arrange
        var viewModel = new MachineManagerViewModel();
        viewModel.StartInstanceCommand.Execute(null);
        Assert.That(viewModel.Instances.Count, Is.EqualTo(1));

        // Act
        viewModel.StopInstanceCommand.Execute(null);

        // Assert
        Assert.That(viewModel.Instances.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that PauseInstanceCommand toggles the instance status.
    /// </summary>
    [Test]
    public void PauseInstanceCommand_TogglesInstanceStatus()
    {
        // Arrange
        var viewModel = new MachineManagerViewModel();
        viewModel.StartInstanceCommand.Execute(null);
        Assert.That(viewModel.SelectedInstance!.Status, Is.EqualTo("Running"));

        // Act
        viewModel.PauseInstanceCommand.Execute(null);

        // Assert
        Assert.That(viewModel.SelectedInstance.Status, Is.EqualTo("Paused"));

        // Act again to toggle back
        viewModel.PauseInstanceCommand.Execute(null);

        // Assert
        Assert.That(viewModel.SelectedInstance.Status, Is.EqualTo("Running"));
    }

    /// <summary>
    /// Tests that ResetInstanceCommand sets status to Running.
    /// </summary>
    [Test]
    public void ResetInstanceCommand_SetsStatusToRunning()
    {
        // Arrange
        var viewModel = new MachineManagerViewModel();
        viewModel.StartInstanceCommand.Execute(null);
        viewModel.PauseInstanceCommand.Execute(null);
        Assert.That(viewModel.SelectedInstance!.Status, Is.EqualTo("Paused"));

        // Act
        viewModel.ResetInstanceCommand.Execute(null);

        // Assert
        Assert.That(viewModel.SelectedInstance.Status, Is.EqualTo("Running"));
    }
}