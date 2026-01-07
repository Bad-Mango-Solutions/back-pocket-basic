// <copyright file="DebugCommandsTests.BusCommands.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for bus-related commands: <see cref="PagesCommand"/>, <see cref="PeekCommand"/>,
/// <see cref="ReadCommand"/>, and <see cref="WriteCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that PagesCommand has correct name.
    /// </summary>
    [Test]
    public void PagesCommand_HasCorrectName()
    {
        var command = new PagesCommand();
        Assert.That(command.Name, Is.EqualTo("pages"));
    }

    /// <summary>
    /// Verifies that PagesCommand displays page table.
    /// </summary>
    [Test]
    public void PagesCommand_DisplaysPageTable()
    {
        var command = new PagesCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Page Table"));
            Assert.That(outputWriter.ToString(), Does.Contain("VirtAddr"));
        });
    }

    /// <summary>
    /// Verifies that PagesCommand accepts start page argument.
    /// </summary>
    [Test]
    public void PagesCommand_AcceptsStartPage()
    {
        var command = new PagesCommand();
        var result = command.Execute(debugContext, ["$04"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("$04"));
        });
    }

    /// <summary>
    /// Verifies that PeekCommand has correct name.
    /// </summary>
    [Test]
    public void PeekCommand_HasCorrectName()
    {
        var command = new PeekCommand();
        Assert.That(command.Name, Is.EqualTo("peek"));
    }

    /// <summary>
    /// Verifies that PeekCommand reads single byte.
    /// </summary>
    [Test]
    public void PeekCommand_ReadsSingleByte()
    {
        WriteByte(bus, 0x1234, 0xAB);
        var command = new PeekCommand();
        var result = command.Execute(debugContext, ["$1234"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("$1234"));
            Assert.That(outputWriter.ToString(), Does.Contain("AB"));
        });
    }

    /// <summary>
    /// Verifies that PeekCommand reads multiple bytes.
    /// </summary>
    [Test]
    public void PeekCommand_ReadsMultipleBytes()
    {
        WriteByte(bus, 0x1000, 0x11);
        WriteByte(bus, 0x1001, 0x22);
        WriteByte(bus, 0x1002, 0x33);
        var command = new PeekCommand();
        var result = command.Execute(debugContext, ["$1000", "3"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("11"));
            Assert.That(outputWriter.ToString(), Does.Contain("22"));
            Assert.That(outputWriter.ToString(), Does.Contain("33"));
        });
    }

    /// <summary>
    /// Verifies that ReadCommand has correct name.
    /// </summary>
    [Test]
    public void ReadCommand_HasCorrectName()
    {
        var command = new ReadCommand();
        Assert.That(command.Name, Is.EqualTo("read"));
    }

    /// <summary>
    /// Verifies that ReadCommand has correct aliases.
    /// </summary>
    [Test]
    public void ReadCommand_HasCorrectAliases()
    {
        var command = new ReadCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "rd" }));
    }

    /// <summary>
    /// Verifies that ReadCommand reads memory.
    /// </summary>
    [Test]
    public void ReadCommand_ReadsMemory()
    {
        WriteByte(bus, 0x2000, 0xCD);
        var command = new ReadCommand();
        var result = command.Execute(debugContext, ["$2000"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("CD"));
            Assert.That(outputWriter.ToString(), Does.Contain("side effects"));
        });
    }

    /// <summary>
    /// Verifies that WriteCommand has correct name.
    /// </summary>
    [Test]
    public void WriteCommand_HasCorrectName()
    {
        var command = new WriteCommand();
        Assert.That(command.Name, Is.EqualTo("write"));
    }

    /// <summary>
    /// Verifies that WriteCommand has correct aliases.
    /// </summary>
    [Test]
    public void WriteCommand_HasCorrectAliases()
    {
        var command = new WriteCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "wr" }));
    }

    /// <summary>
    /// Verifies that WriteCommand writes memory.
    /// </summary>
    [Test]
    public void WriteCommand_WritesMemory()
    {
        var command = new WriteCommand();
        var result = command.Execute(debugContext, ["$3000", "EF"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x3000), Is.EqualTo(0xEF));
            Assert.That(outputWriter.ToString(), Does.Contain("side effects"));
        });
    }
}
