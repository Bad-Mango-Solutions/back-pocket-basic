// <copyright file="DebugCommandsTests.Reset.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="ResetCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that ResetCommand has correct name.
    /// </summary>
    [Test]
    public void ResetCommand_HasCorrectName()
    {
        var command = new ResetCommand();
        Assert.That(command.Name, Is.EqualTo("reset"));
    }

    /// <summary>
    /// Verifies that ResetCommand performs soft reset by default.
    /// </summary>
    [Test]
    public void ResetCommand_PerformsSoftReset_ByDefault()
    {
        // Change PC to different value
        cpu.SetPC(0x5000);

        var command = new ResetCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(cpu.GetPC(), Is.EqualTo(0x1000u)); // Reset vector
            Assert.That(outputWriter.ToString(), Does.Contain("Soft reset"));
        });
    }

    /// <summary>
    /// Verifies that ResetCommand performs hard reset when flag specified.
    /// </summary>
    [Test]
    public void ResetCommand_PerformsHardReset_WhenFlagSpecified()
    {
        // Write some data to memory
        WriteByte(bus, 0x0200, 0xFF);

        // Need to re-set reset vector after clear
        WriteWord(bus, 0xFFFC, 0x1000);

        var command = new ResetCommand();
        var result = command.Execute(debugContext, ["--hard"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Hard reset"));
        });
    }
}
