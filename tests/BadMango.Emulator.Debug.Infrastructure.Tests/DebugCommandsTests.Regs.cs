// <copyright file="DebugCommandsTests.Regs.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="RegsCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that RegsCommand has correct name.
    /// </summary>
    [Test]
    public void RegsCommand_HasCorrectName()
    {
        var command = new RegsCommand();
        Assert.That(command.Name, Is.EqualTo("regs"));
    }

    /// <summary>
    /// Verifies that RegsCommand has correct aliases.
    /// </summary>
    [Test]
    public void RegsCommand_HasCorrectAliases()
    {
        var command = new RegsCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "r", "registers" }));
    }

    /// <summary>
    /// Verifies that RegsCommand displays registers when CPU is attached.
    /// </summary>
    [Test]
    public void RegsCommand_DisplaysRegisters_WhenCpuAttached()
    {
        var command = new RegsCommand();

        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("CPU Registers"));
            Assert.That(outputWriter.ToString(), Does.Contain("PC"));
            Assert.That(outputWriter.ToString(), Does.Contain("SP"));
        });
    }

    /// <summary>
    /// Verifies that RegsCommand returns error when CPU is not attached.
    /// </summary>
    [Test]
    public void RegsCommand_ReturnsError_WhenNoCpuAttached()
    {
        var contextWithoutCpu = new DebugContext(dispatcher, outputWriter, errorWriter);
        var command = new RegsCommand();

        var result = command.Execute(contextWithoutCpu, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("No CPU attached"));
        });
    }
}