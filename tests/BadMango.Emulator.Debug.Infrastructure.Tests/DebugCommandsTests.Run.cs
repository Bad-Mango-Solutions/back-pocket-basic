// <copyright file="DebugCommandsTests.Run.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="RunCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that RunCommand has correct name.
    /// </summary>
    [Test]
    public void RunCommand_HasCorrectName()
    {
        var command = new RunCommand();
        Assert.That(command.Name, Is.EqualTo("run"));
    }

    /// <summary>
    /// Verifies that RunCommand has correct aliases.
    /// </summary>
    [Test]
    public void RunCommand_HasCorrectAliases()
    {
        var command = new RunCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "g", "go" }));
    }

    /// <summary>
    /// Verifies that RunCommand runs until CPU halts.
    /// </summary>
    [Test]
    public void RunCommand_RunsUntilHalt()
    {
        // Write a few NOPs then STP
        WriteByte(bus, 0x1000, 0xEA); // NOP
        WriteByte(bus, 0x1001, 0xEA); // NOP
        WriteByte(bus, 0x1002, 0xDB); // STP
        cpu.Reset();

        var command = new RunCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("CPU halted"));
            Assert.That(cpu.Halted, Is.True);
        });
    }

    /// <summary>
    /// Verifies that RunCommand respects instruction limit.
    /// </summary>
    [Test]
    public void RunCommand_RespectsInstructionLimit()
    {
        // Write infinite NOP loop
        WriteByte(bus, 0x1000, 0xEA); // NOP
        WriteByte(bus, 0x1001, 0x4C); // JMP $1000
        WriteByte(bus, 0x1002, 0x00);
        WriteByte(bus, 0x1003, 0x10);
        cpu.Reset();

        var command = new RunCommand();
        var result = command.Execute(debugContext, ["10"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("instruction limit"));
        });
    }
}