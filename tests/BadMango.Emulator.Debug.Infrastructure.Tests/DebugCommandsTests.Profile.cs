// <copyright file="DebugCommandsTests.Profile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="ProfileCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that ProfileCommand has correct name.
    /// </summary>
    [Test]
    public void ProfileCommand_HasCorrectName()
    {
        var command = new ProfileCommand();
        Assert.That(command.Name, Is.EqualTo("profile"));
    }

    /// <summary>
    /// Verifies that ProfileCommand displays machine profile info.
    /// </summary>
    [Test]
    public void ProfileCommand_DisplaysProfileInfo()
    {
        var command = new ProfileCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Machine Profile"));
        });
    }

    /// <summary>
    /// Verifies that ProfileCommand list shows available profiles.
    /// </summary>
    [Test]
    public void ProfileCommand_List_ShowsAvailableProfiles()
    {
        var command = new ProfileCommand();
        var result = command.Execute(debugContext, ["list"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Available Profiles"));
        });
    }

    /// <summary>
    /// Verifies that ProfileCommand load with missing name returns error.
    /// </summary>
    [Test]
    public void ProfileCommand_Load_MissingName_ReturnsError()
    {
        var command = new ProfileCommand();
        var result = command.Execute(debugContext, ["load"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Usage"));
        });
    }

    /// <summary>
    /// Verifies that ProfileCommand save with missing name returns error.
    /// </summary>
    [Test]
    public void ProfileCommand_Save_MissingName_ReturnsError()
    {
        var command = new ProfileCommand();
        var result = command.Execute(debugContext, ["save"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Usage"));
        });
    }

    /// <summary>
    /// Verifies that ProfileCommand default shows current default.
    /// </summary>
    [Test]
    public void ProfileCommand_Default_ShowsCurrentDefault()
    {
        var command = new ProfileCommand();
        var result = command.Execute(debugContext, ["default"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Current default profile"));
        });
    }

    /// <summary>
    /// Verifies that ProfileCommand has correct help synopsis.
    /// </summary>
    [Test]
    public void ProfileCommand_HasCorrectSynopsis()
    {
        var command = new ProfileCommand();
        Assert.That(command.Synopsis, Does.Contain("list"));
        Assert.That(command.Synopsis, Does.Contain("load"));
        Assert.That(command.Synopsis, Does.Contain("save"));
        Assert.That(command.Synopsis, Does.Contain("default"));
        Assert.That(command.Synopsis, Does.Contain("initroms"));
    }

    /// <summary>
    /// Verifies that ProfileCommand initroms with missing name returns error.
    /// </summary>
    [Test]
    public void ProfileCommand_InitRoms_MissingName_ReturnsError()
    {
        var command = new ProfileCommand();
        var result = command.Execute(this.debugContext, ["initroms"]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Usage"));
    }

    /// <summary>
    /// Verifies that ProfileCommand initroms with invalid profile returns error.
    /// </summary>
    [Test]
    public void ProfileCommand_InitRoms_InvalidProfile_ReturnsError()
    {
        var command = new ProfileCommand();
        var result = command.Execute(this.debugContext, ["initroms", "nonexistent-profile"]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("not found"));
    }
}