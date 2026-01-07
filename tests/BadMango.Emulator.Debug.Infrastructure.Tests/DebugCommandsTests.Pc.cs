// <copyright file="DebugCommandsTests.Pc.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="PcCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that PcCommand has correct name.
    /// </summary>
    [Test]
    public void PcCommand_HasCorrectName()
    {
        var command = new PcCommand();
        Assert.That(command.Name, Is.EqualTo("pc"));
    }

    /// <summary>
    /// Verifies that PcCommand displays current PC when called without arguments.
    /// </summary>
    [Test]
    public void PcCommand_DisplaysCurrentPc_WithoutArguments()
    {
        var command = new PcCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("PC = $1000"));
        });
    }

    /// <summary>
    /// Verifies that PcCommand sets PC when address specified.
    /// </summary>
    [Test]
    public void PcCommand_SetsPc_WhenAddressSpecified()
    {
        var command = new PcCommand();
        var result = command.Execute(debugContext, ["$2000"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(cpu.GetPC(), Is.EqualTo(0x2000u));
            Assert.That(outputWriter.ToString(), Does.Contain("PC set to $2000"));
        });
    }

    /// <summary>
    /// Verifies that PcCommand accepts 0x hex format.
    /// </summary>
    [Test]
    public void PcCommand_AcceptsHexFormat()
    {
        var command = new PcCommand();
        var result = command.Execute(debugContext, ["0x3000"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(cpu.GetPC(), Is.EqualTo(0x3000u));
        });
    }
}
