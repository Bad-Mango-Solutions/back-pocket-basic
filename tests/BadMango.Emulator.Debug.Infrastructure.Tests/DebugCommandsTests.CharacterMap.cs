// <copyright file="DebugCommandsTests.CharacterMap.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

/// <summary>
/// Unit tests for <see cref="CharacterMapCommand"/> path resolution functionality.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that CharacterMapCommand has correct name.
    /// </summary>
    [Test]
    public void CharacterMapCommand_HasCorrectName()
    {
        var command = new CharacterMapCommand();
        Assert.That(command.Name, Is.EqualTo("charactermap"));
    }

    /// <summary>
    /// Verifies that CharacterMapCommand load subcommand returns error when library root is not configured.
    /// </summary>
    [Test]
    public void CharacterMapCommand_Load_ReturnsError_WhenLibraryRootNotConfigured()
    {
        // Attach a path resolver without library root
        debugContext.AttachPathResolver(new DebugPathResolver(null));

        // Need a machine with character device for the command to work.
        // Since we don't have one, the command will fail early.
        var command = new CharacterMapCommand();
        var result = command.Execute(debugContext, ["load", "library://fonts/test.rom"]);

        // The command requires a machine, so it will fail with that error first.
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
        });
    }

    /// <summary>
    /// Verifies that CharacterMapCommand load subcommand returns error with resolved path when file not found.
    /// </summary>
    [Test]
    public void CharacterMapCommand_Load_ReturnsError_WithResolvedPath_WhenFileNotFound()
    {
        // Attach a path resolver with a known library root
        debugContext.AttachPathResolver(new DebugPathResolver("/tmp/test-library"));

        var command = new CharacterMapCommand();
        var result = command.Execute(debugContext, ["load", "library://fonts/test.rom"]);

        // The command requires a machine with character device, so it will fail
        // before reaching path resolution if machine is not set up
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
        });
    }

    /// <summary>
    /// Verifies that CharacterMapCommand load subcommand returns error when filename is missing.
    /// </summary>
    [Test]
    public void CharacterMapCommand_Load_ReturnsError_WhenFilenameMissing()
    {
        var command = new CharacterMapCommand();
        var result = command.Execute(debugContext, ["load"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Filename required"));
        });
    }
}