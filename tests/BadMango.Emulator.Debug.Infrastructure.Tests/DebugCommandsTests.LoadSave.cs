// <copyright file="DebugCommandsTests.LoadSave.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="LoadCommand"/> and <see cref="SaveCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that LoadCommand has correct name.
    /// </summary>
    [Test]
    public void LoadCommand_HasCorrectName()
    {
        var command = new LoadCommand();
        Assert.That(command.Name, Is.EqualTo("load"));
    }

    /// <summary>
    /// Verifies that LoadCommand returns error when file not found.
    /// </summary>
    [Test]
    public void LoadCommand_ReturnsError_WhenFileNotFound()
    {
        var command = new LoadCommand();
        var result = command.Execute(debugContext, ["nonexistent.bin"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("File not found"));
        });
    }

    /// <summary>
    /// Verifies that LoadCommand returns error when filename missing.
    /// </summary>
    [Test]
    public void LoadCommand_ReturnsError_WhenFilenameMissing()
    {
        var command = new LoadCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Filename required"));
        });
    }

    /// <summary>
    /// Verifies that SaveCommand has correct name.
    /// </summary>
    [Test]
    public void SaveCommand_HasCorrectName()
    {
        var command = new SaveCommand();
        Assert.That(command.Name, Is.EqualTo("save"));
    }

    /// <summary>
    /// Verifies that SaveCommand returns error when arguments missing.
    /// </summary>
    [Test]
    public void SaveCommand_ReturnsError_WhenArgumentsMissing()
    {
        var command = new SaveCommand();
        var result = command.Execute(debugContext, ["test.bin"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Filename, address, and length required"));
        });
    }
}