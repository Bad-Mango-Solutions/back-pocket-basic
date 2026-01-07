// <copyright file="DebugCommandsTests.Dasm.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="DasmCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that DasmCommand has correct name.
    /// </summary>
    [Test]
    public void DasmCommand_HasCorrectName()
    {
        var command = new DasmCommand();
        Assert.That(command.Name, Is.EqualTo("dasm"));
    }

    /// <summary>
    /// Verifies that DasmCommand has correct aliases.
    /// </summary>
    [Test]
    public void DasmCommand_HasCorrectAliases()
    {
        var command = new DasmCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "d", "disasm", "u", "unassemble" }));
    }

    /// <summary>
    /// Verifies that DasmCommand disassembles memory at current PC by default.
    /// </summary>
    [Test]
    public void DasmCommand_DisassemblesAtCurrentPc_ByDefault()
    {
        // Write NOP instruction at PC
        WriteByte(bus, 0x1000, 0xEA); // NOP

        var command = new DasmCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("$1000"));
            Assert.That(outputWriter.ToString(), Does.Contain("NOP"));
        });
    }

    /// <summary>
    /// Verifies that DasmCommand disassembles at specified address.
    /// </summary>
    [Test]
    public void DasmCommand_DisassemblesAtSpecifiedAddress()
    {
        // Write LDA #$42 at $2000
        WriteByte(bus, 0x2000, 0xA9); // LDA immediate
        WriteByte(bus, 0x2001, 0x42); // #$42

        var command = new DasmCommand();
        var result = command.Execute(debugContext, ["$2000"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("$2000"));
            Assert.That(outputWriter.ToString(), Does.Contain("LDA"));
        });
    }

    /// <summary>
    /// Verifies that DasmCommand returns error when no disassembler attached.
    /// </summary>
    [Test]
    public void DasmCommand_ReturnsError_WhenNoDisassemblerAttached()
    {
        var contextWithoutDisasm = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, null);
        var command = new DasmCommand();

        var result = command.Execute(contextWithoutDisasm, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("No disassembler attached"));
        });
    }
}
