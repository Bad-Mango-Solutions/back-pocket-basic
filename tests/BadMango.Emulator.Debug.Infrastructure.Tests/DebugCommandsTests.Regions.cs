// <copyright file="DebugCommandsTests.Regions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="RegionsCommand"/> and context validation tests.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that debug commands return error with non-debug context.
    /// </summary>
    [Test]
    public void DebugCommands_ReturnError_WithNonDebugContext()
    {
        var normalContext = new CommandContext(dispatcher, outputWriter, errorWriter);

        var commands = new ICommandHandler[]
        {
            new RegsCommand(),
            new StepCommand(),
            new RunCommand(),
            new StopCommand(),
            new ResetCommand(),
            new PcCommand(),
            new MemCommand(),
            new PokeCommand(),
            new LoadCommand(),
            new SaveCommand(),
            new DasmCommand(),
        };

        foreach (var command in commands)
        {
            // Use empty args - the check for debug context should happen before argument validation
            var result = command.Execute(normalContext, []);
            Assert.That(result.Success, Is.False, $"Command {command.Name} should fail with non-debug context");
            Assert.That(result.Message, Does.Contain("Debug context required"), $"Command {command.Name} should mention debug context");
        }
    }

    /// <summary>
    /// Verifies that RegionsCommand has correct name.
    /// </summary>
    [Test]
    public void RegionsCommand_HasCorrectName()
    {
        var command = new RegionsCommand();
        Assert.That(command.Name, Is.EqualTo("regions"));
    }

    /// <summary>
    /// Verifies that RegionsCommand displays memory regions.
    /// </summary>
    [Test]
    public void RegionsCommand_DisplaysMemoryRegions()
    {
        var command = new RegionsCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Memory Regions"));
            Assert.That(outputWriter.ToString(), Does.Contain("Ram"));
        });
    }

    /// <summary>
    /// Verifies that RegionsCommand returns error when no bus attached.
    /// </summary>
    [Test]
    public void RegionsCommand_ReturnsError_WhenNoBusAttached()
    {
        var contextWithoutBus = new DebugContext(dispatcher, outputWriter, errorWriter);
        var command = new RegionsCommand();

        var result = command.Execute(contextWithoutBus, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("No bus attached"));
        });
    }
}
