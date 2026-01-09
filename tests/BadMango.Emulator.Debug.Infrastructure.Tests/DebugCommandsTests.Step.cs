// <copyright file="DebugCommandsTests.Step.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="StepCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that StepCommand has correct name.
    /// </summary>
    [Test]
    public void StepCommand_HasCorrectName()
    {
        var command = new StepCommand();
        Assert.That(command.Name, Is.EqualTo("step"));
    }

    /// <summary>
    /// Verifies that StepCommand has correct aliases.
    /// </summary>
    [Test]
    public void StepCommand_HasCorrectAliases()
    {
        var command = new StepCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "s", "si" }));
    }

    /// <summary>
    /// Verifies that StepCommand executes single instruction by default.
    /// </summary>
    [Test]
    public void StepCommand_ExecutesSingleInstruction_ByDefault()
    {
        // Write a NOP instruction at PC
        WriteByte(bus, 0x1000, 0xEA); // NOP
        cpu.Reset();

        var command = new StepCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Executed 1 instruction"));
            Assert.That(cpu.GetPC(), Is.EqualTo(0x1001u));
        });
    }

    /// <summary>
    /// Verifies that StepCommand executes multiple instructions when count specified.
    /// </summary>
    [Test]
    public void StepCommand_ExecutesMultipleInstructions_WhenCountSpecified()
    {
        // Write NOP instructions at PC
        WriteByte(bus, 0x1000, 0xEA); // NOP
        WriteByte(bus, 0x1001, 0xEA); // NOP
        WriteByte(bus, 0x1002, 0xEA); // NOP
        cpu.Reset();

        var command = new StepCommand();
        var result = command.Execute(debugContext, ["3"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Executed 3 instruction"));
            Assert.That(cpu.GetPC(), Is.EqualTo(0x1003u));
        });
    }

    /// <summary>
    /// Verifies that StepCommand returns error when CPU is halted.
    /// </summary>
    [Test]
    public void StepCommand_ReturnsError_WhenCpuHalted()
    {
        // Write STP instruction which halts the CPU
        WriteByte(bus, 0x1000, 0xDB); // STP
        cpu.Reset();
        cpu.Step(); // Execute STP to halt CPU

        var command = new StepCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("halted"));
        });
    }
}