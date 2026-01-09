// <copyright file="BuiltInCommandsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for the built-in command handlers.
/// </summary>
[TestFixture]
public class BuiltInCommandsTests
{
    /// <summary>
    /// Verifies that HelpCommand has correct name.
    /// </summary>
    [Test]
    public void HelpCommand_HasCorrectName()
    {
        var command = new HelpCommand();

        Assert.That(command.Name, Is.EqualTo("help"));
    }

    /// <summary>
    /// Verifies that HelpCommand has correct aliases.
    /// </summary>
    [Test]
    public void HelpCommand_HasCorrectAliases()
    {
        var command = new HelpCommand();

        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "?", "h" }));
    }

    /// <summary>
    /// Verifies that HelpCommand lists all commands when called without arguments.
    /// </summary>
    [Test]
    public void HelpCommand_ListsAllCommandsWithoutArguments()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new HelpCommand());
        dispatcher.Register(new ExitCommand());
        var context = CreateTestContext(dispatcher, out var output, out _);
        var command = new HelpCommand();

        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(output.ToString(), Does.Contain("help"));
            Assert.That(output.ToString(), Does.Contain("exit"));
        });
    }

    /// <summary>
    /// Verifies that HelpCommand shows command details when called with command name.
    /// </summary>
    [Test]
    public void HelpCommand_ShowsCommandDetailsWithArgument()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new ExitCommand());
        var context = CreateTestContext(dispatcher, out var output, out _);
        var command = new HelpCommand();

        var result = command.Execute(context, ["exit"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(output.ToString(), Does.Contain("exit"));
            Assert.That(output.ToString(), Does.Contain("Exit the debug console"));
        });
    }

    /// <summary>
    /// Verifies that HelpCommand returns error for unknown command.
    /// </summary>
    [Test]
    public void HelpCommand_ReturnsErrorForUnknownCommand()
    {
        var dispatcher = new CommandDispatcher();
        var context = CreateTestContext(dispatcher, out _, out _);
        var command = new HelpCommand();

        var result = command.Execute(context, ["unknown"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Unknown command"));
        });
    }

    /// <summary>
    /// Verifies that ExitCommand has correct name.
    /// </summary>
    [Test]
    public void ExitCommand_HasCorrectName()
    {
        var command = new ExitCommand();

        Assert.That(command.Name, Is.EqualTo("exit"));
    }

    /// <summary>
    /// Verifies that ExitCommand has correct aliases.
    /// </summary>
    [Test]
    public void ExitCommand_HasCorrectAliases()
    {
        var command = new ExitCommand();

        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "quit", "q" }));
    }

    /// <summary>
    /// Verifies that ExitCommand returns exit result.
    /// </summary>
    [Test]
    public void ExitCommand_ReturnsExitResult()
    {
        var dispatcher = new CommandDispatcher();
        var context = CreateTestContext(dispatcher, out _, out _);
        var command = new ExitCommand();

        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ShouldExit, Is.True);
            Assert.That(result.Message, Is.EqualTo("Goodbye!"));
        });
    }

    /// <summary>
    /// Verifies that VersionCommand has correct name.
    /// </summary>
    [Test]
    public void VersionCommand_HasCorrectName()
    {
        var command = new VersionCommand();

        Assert.That(command.Name, Is.EqualTo("version"));
    }

    /// <summary>
    /// Verifies that VersionCommand has correct aliases.
    /// </summary>
    [Test]
    public void VersionCommand_HasCorrectAliases()
    {
        var command = new VersionCommand();

        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "ver", "v" }));
    }

    /// <summary>
    /// Verifies that VersionCommand displays version information.
    /// </summary>
    [Test]
    public void VersionCommand_DisplaysVersionInformation()
    {
        var dispatcher = new CommandDispatcher();
        var context = CreateTestContext(dispatcher, out var output, out _);
        var command = new VersionCommand();

        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(output.ToString(), Does.Contain("Emulator Debug Console"));
            Assert.That(output.ToString(), Does.Contain("Bad Mango Solutions"));
        });
    }

    /// <summary>
    /// Verifies that ClearCommand has correct name.
    /// </summary>
    [Test]
    public void ClearCommand_HasCorrectName()
    {
        var command = new ClearCommand();

        Assert.That(command.Name, Is.EqualTo("clear"));
    }

    /// <summary>
    /// Verifies that ClearCommand has correct aliases.
    /// </summary>
    [Test]
    public void ClearCommand_HasCorrectAliases()
    {
        var command = new ClearCommand();

        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "cls" }));
    }

    /// <summary>
    /// Verifies that ClearCommand returns success.
    /// </summary>
    [Test]
    public void ClearCommand_ReturnsSuccess()
    {
        var dispatcher = new CommandDispatcher();
        var context = CreateTestContext(dispatcher, out _, out _);
        var command = new ClearCommand();

        var result = command.Execute(context, []);

        Assert.That(result.Success, Is.True);
    }

    private static CommandContext CreateTestContext(ICommandDispatcher dispatcher, out StringWriter outputWriter, out StringWriter errorWriter)
    {
        outputWriter = new();
        errorWriter = new();
        return new(dispatcher, outputWriter, errorWriter);
    }
}