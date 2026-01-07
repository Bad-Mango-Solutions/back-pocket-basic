// <copyright file="DebugCommandsTests.Poke.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Unit tests for <see cref="PokeCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that PokeCommand has correct name.
    /// </summary>
    [Test]
    public void PokeCommand_HasCorrectName()
    {
        var command = new PokeCommand();
        Assert.That(command.Name, Is.EqualTo("poke"));
    }

    /// <summary>
    /// Verifies that PokeCommand writes single byte.
    /// </summary>
    [Test]
    public void PokeCommand_WritesSingleByte()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0300", "$AB"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0300), Is.EqualTo(0xAB));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand writes multiple bytes.
    /// </summary>
    [Test]
    public void PokeCommand_WritesMultipleBytes()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0400", "$11", "$22", "$33"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0400), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0401), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0402), Is.EqualTo(0x33));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand returns error when address missing.
    /// </summary>
    [Test]
    public void PokeCommand_ReturnsError_WhenAddressMissing()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Address required"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand accepts unprefixed hex bytes.
    /// </summary>
    [Test]
    public void PokeCommand_AcceptsUnprefixedHexBytes()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0900", "ab", "cd", "ef"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0900), Is.EqualTo(0xAB));
            Assert.That(ReadByte(bus, 0x0901), Is.EqualTo(0xCD));
            Assert.That(ReadByte(bus, 0x0902), Is.EqualTo(0xEF));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand accepts mixed prefixed and unprefixed bytes.
    /// </summary>
    [Test]
    public void PokeCommand_AcceptsMixedPrefixedAndUnprefixedBytes()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0950", "$11", "22", "0x33"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0950), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0951), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0952), Is.EqualTo(0x33));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode writes bytes from input.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_WritesBytesFromInput()
    {
        // Set up input with some hex bytes and blank line to finish
        var inputText = "AA BB CC\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0500", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0500), Is.EqualTo(0xAA));
            Assert.That(ReadByte(bus, 0x0501), Is.EqualTo(0xBB));
            Assert.That(ReadByte(bus, 0x0502), Is.EqualTo(0xCC));
            Assert.That(outputWriter.ToString(), Does.Contain("Interactive poke mode"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode handles multiple lines.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_HandlesMultipleLines()
    {
        var inputText = "11 22\n33 44\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0600", "--interactive"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0600), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0601), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0602), Is.EqualTo(0x33));
            Assert.That(ReadByte(bus, 0x0603), Is.EqualTo(0x44));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode exits on empty line.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_ExitsOnEmptyLine()
    {
        var inputText = "55\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0700", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0700), Is.EqualTo(0x55));
            Assert.That(outputWriter.ToString(), Does.Contain("Interactive mode complete"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode returns error when no input available.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_ReturnsError_WhenNoInputAvailable()
    {
        // Create context without input reader
        var contextWithoutInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, null);

        var command = new PokeCommand();
        var result = command.Execute(contextWithoutInput, ["$0800", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Interactive mode not available"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode supports address prefix to change write location.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_SupportsAddressPrefix()
    {
        // Start at $0A00, then change to $0B00
        var inputText = "11 22\n$0B00: 33 44\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0A00", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0A00), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0A01), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0B00), Is.EqualTo(0x33));
            Assert.That(ReadByte(bus, 0x0B01), Is.EqualTo(0x44));
            Assert.That(outputWriter.ToString(), Does.Contain("Address changed to $0B00"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode supports address-only line to change location.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_SupportsAddressOnlyLine()
    {
        // Start at $0C00, change to $0D00, then write
        var inputText = "$0D00:\n55 66\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0C00", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0D00), Is.EqualTo(0x55));
            Assert.That(ReadByte(bus, 0x0D01), Is.EqualTo(0x66));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode with 0x address prefix works.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_Supports0xAddressPrefix()
    {
        var inputText = "0x0E00: 77 88\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0100", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0E00), Is.EqualTo(0x77));
            Assert.That(ReadByte(bus, 0x0E01), Is.EqualTo(0x88));
        });
    }
}
