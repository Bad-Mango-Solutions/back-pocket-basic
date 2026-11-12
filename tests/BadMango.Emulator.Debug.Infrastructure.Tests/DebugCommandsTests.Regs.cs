// <copyright file="DebugCommandsTests.Regs.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <content>
/// Unit tests for <see cref="RegsCommand"/>.
/// </content>
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

    /// <summary>
    /// Verifies that RegsCommand outputs JSON when --json flag or context JsonOutput is set.
    /// </summary>
    [Test]
    public void RegsCommand_OutputsJson_WhenRequested()
    {
        // Per-command flag
        var command = new RegsCommand();
        var result = command.Execute(debugContext, ["--json"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            var output = outputWriter.ToString();
            Assert.That(output, Does.Contain("\"mode\""));
            Assert.That(output, Does.Contain("\"pc\""));
            Assert.That(output, Does.Contain("Compat"));
        });

        // Reset writer
        outputWriter.GetStringBuilder().Clear();

        // Global via context
        var jsonContext = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, null, null, null, true);

        // Attach needed? but for test use the one with cpu
        var result2 = command.Execute(jsonContext, []);
        Assert.That(outputWriter.ToString(), Does.Contain("\"mode\""));
    }
}