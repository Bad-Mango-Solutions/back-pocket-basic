// <copyright file="DebugCommandsTests.Mem.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="MemCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that MemCommand has correct name.
    /// </summary>
    [Test]
    public void MemCommand_HasCorrectName()
    {
        var command = new MemCommand();
        Assert.That(command.Name, Is.EqualTo("mem"));
    }

    /// <summary>
    /// Verifies that MemCommand has correct aliases.
    /// </summary>
    [Test]
    public void MemCommand_HasCorrectAliases()
    {
        var command = new MemCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "m", "dump", "hexdump" }));
    }

    /// <summary>
    /// Verifies that MemCommand displays memory contents.
    /// </summary>
    [Test]
    public void MemCommand_DisplaysMemoryContents()
    {
        // Write some known values
        WriteByte(bus, 0x0200, 0x41); // 'A'
        WriteByte(bus, 0x0201, 0x42); // 'B'
        WriteByte(bus, 0x0202, 0x43); // 'C'

        var command = new MemCommand();
        var result = command.Execute(debugContext, ["$0200", "16"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("41"));
            Assert.That(outputWriter.ToString(), Does.Contain("42"));
            Assert.That(outputWriter.ToString(), Does.Contain("43"));
            Assert.That(outputWriter.ToString(), Does.Contain("ABC")); // ASCII
        });
    }

    /// <summary>
    /// Verifies that MemCommand returns error when address missing.
    /// </summary>
    [Test]
    public void MemCommand_ReturnsError_WhenAddressMissing()
    {
        var command = new MemCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Address required"));
        });
    }
}
