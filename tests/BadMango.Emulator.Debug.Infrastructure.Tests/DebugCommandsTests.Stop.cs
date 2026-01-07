// <copyright file="DebugCommandsTests.Stop.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="StopCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that StopCommand has correct name.
    /// </summary>
    [Test]
    public void StopCommand_HasCorrectName()
    {
        var command = new StopCommand();
        Assert.That(command.Name, Is.EqualTo("stop"));
    }

    /// <summary>
    /// Verifies that StopCommand requests CPU to stop.
    /// </summary>
    [Test]
    public void StopCommand_RequestsStop()
    {
        var command = new StopCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(cpu.IsStopRequested, Is.True);
        });
    }
}
