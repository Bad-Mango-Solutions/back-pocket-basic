// <copyright file="CommandDispatcherTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for the <see cref="CommandDispatcher"/> class.
/// </summary>
[TestFixture]
public class CommandDispatcherTests
{
    /// <summary>
    /// Verifies that a new CommandDispatcher has no commands registered.
    /// </summary>
    [Test]
    public void CommandDispatcher_NewInstance_HasNoCommands()
    {
        var dispatcher = new CommandDispatcher();

        Assert.That(dispatcher.Commands, Is.Empty);
    }

    /// <summary>
    /// Verifies that Register adds a command handler.
    /// </summary>
    [Test]
    public void Register_AddsCommandHandler()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestCommand("test", "Test command");

        dispatcher.Register(handler);

        Assert.That(dispatcher.Commands, Has.Count.EqualTo(1));
        Assert.That(dispatcher.Commands[0], Is.SameAs(handler));
    }

    /// <summary>
    /// Verifies that Register throws ArgumentNullException for null handler.
    /// </summary>
    [Test]
    public void Register_ThrowsOnNullHandler()
    {
        var dispatcher = new CommandDispatcher();

        Assert.Throws<ArgumentNullException>(() => dispatcher.Register(null!));
    }

    /// <summary>
    /// Verifies that Register throws InvalidOperationException for duplicate command name.
    /// </summary>
    [Test]
    public void Register_ThrowsOnDuplicateCommandName()
    {
        var dispatcher = new CommandDispatcher();
        var handler1 = new TestCommand("test", "Test command 1");
        var handler2 = new TestCommand("test", "Test command 2");

        dispatcher.Register(handler1);

        Assert.Throws<InvalidOperationException>(() => dispatcher.Register(handler2));
    }

    /// <summary>
    /// Verifies that Register throws InvalidOperationException for duplicate alias.
    /// </summary>
    [Test]
    public void Register_ThrowsOnDuplicateAlias()
    {
        var dispatcher = new CommandDispatcher();
        var handler1 = new TestCommand("test1", "Test command 1", ["t"]);
        var handler2 = new TestCommand("test2", "Test command 2", ["t"]);

        dispatcher.Register(handler1);

        Assert.Throws<InvalidOperationException>(() => dispatcher.Register(handler2));
    }

    /// <summary>
    /// Verifies that Register lists all conflicting aliases in the exception message.
    /// </summary>
    [Test]
    public void Register_ListsAllConflictingAliasesInExceptionMessage()
    {
        var dispatcher = new CommandDispatcher();
        var handler1 = new TestCommand("cmd1", "Command 1", ["a", "b"]);
        var handler2 = new TestCommand("cmd2", "Command 2", ["c", "d"]);
        var handler3 = new TestCommand("cmd3", "Command 3", ["a", "b", "c"]);

        dispatcher.Register(handler1);
        dispatcher.Register(handler2);

        var ex = Assert.Throws<InvalidOperationException>(() => dispatcher.Register(handler3));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Contain("cmd3"));
            Assert.That(ex.Message, Does.Contain("'a' (already registered by 'cmd1')"));
            Assert.That(ex.Message, Does.Contain("'b' (already registered by 'cmd1')"));
            Assert.That(ex.Message, Does.Contain("'c' (already registered by 'cmd2')"));
            Assert.That(ex.Message, Does.Contain("aliases"));
        });
    }

    /// <summary>
    /// Verifies that TryGetHandler returns true and finds handler by name.
    /// </summary>
    [Test]
    public void TryGetHandler_FindsHandlerByName()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestCommand("test", "Test command");
        dispatcher.Register(handler);

        var found = dispatcher.TryGetHandler("test", out var result);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(result, Is.SameAs(handler));
        });
    }

    /// <summary>
    /// Verifies that TryGetHandler returns true and finds handler by alias.
    /// </summary>
    [Test]
    public void TryGetHandler_FindsHandlerByAlias()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestCommand("test", "Test command", ["t", "tst"]);
        dispatcher.Register(handler);

        var found = dispatcher.TryGetHandler("t", out var result);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(result, Is.SameAs(handler));
        });
    }

    /// <summary>
    /// Verifies that TryGetHandler is case-insensitive.
    /// </summary>
    [Test]
    public void TryGetHandler_IsCaseInsensitive()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestCommand("test", "Test command");
        dispatcher.Register(handler);

        var found = dispatcher.TryGetHandler("TEST", out var result);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(result, Is.SameAs(handler));
        });
    }

    /// <summary>
    /// Verifies that TryGetHandler returns false for unknown command.
    /// </summary>
    [Test]
    public void TryGetHandler_ReturnsFalseForUnknownCommand()
    {
        var dispatcher = new CommandDispatcher();

        var found = dispatcher.TryGetHandler("unknown", out var result);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.False);
            Assert.That(result, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that Dispatch executes the correct handler.
    /// </summary>
    [Test]
    public void Dispatch_ExecutesCorrectHandler()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestCommand("test", "Test command");
        dispatcher.Register(handler);
        var context = CreateTestContext(dispatcher);

        var result = dispatcher.Dispatch(context, "test");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(handler.ExecuteCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that Dispatch passes arguments to handler.
    /// </summary>
    [Test]
    public void Dispatch_PassesArgumentsToHandler()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestCommand("test", "Test command");
        dispatcher.Register(handler);
        var context = CreateTestContext(dispatcher);

        dispatcher.Dispatch(context, "test arg1 arg2 arg3");

        Assert.That(handler.LastArgs, Is.EqualTo(new[] { "arg1", "arg2", "arg3" }));
    }

    /// <summary>
    /// Verifies that Dispatch returns error for unknown command.
    /// </summary>
    [Test]
    public void Dispatch_ReturnsErrorForUnknownCommand()
    {
        var dispatcher = new CommandDispatcher();
        var context = CreateTestContext(dispatcher);

        var result = dispatcher.Dispatch(context, "unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Unknown command"));
        });
    }

    /// <summary>
    /// Verifies that Dispatch returns success for empty input.
    /// </summary>
    [Test]
    public void Dispatch_ReturnsSuccessForEmptyInput()
    {
        var dispatcher = new CommandDispatcher();
        var context = CreateTestContext(dispatcher);

        var result = dispatcher.Dispatch(context, string.Empty);

        Assert.That(result.Success, Is.True);
    }

    /// <summary>
    /// Verifies that Dispatch returns success for whitespace input.
    /// </summary>
    [Test]
    public void Dispatch_ReturnsSuccessForWhitespaceInput()
    {
        var dispatcher = new CommandDispatcher();
        var context = CreateTestContext(dispatcher);

        var result = dispatcher.Dispatch(context, "   ");

        Assert.That(result.Success, Is.True);
    }

    /// <summary>
    /// Verifies that ParseCommandLine handles simple commands.
    /// </summary>
    [Test]
    public void ParseCommandLine_HandlesSimpleCommand()
    {
        var result = CommandDispatcher.ParseCommandLine("help");

        Assert.That(result, Is.EqualTo(new[] { "help" }));
    }

    /// <summary>
    /// Verifies that ParseCommandLine handles command with arguments.
    /// </summary>
    [Test]
    public void ParseCommandLine_HandlesCommandWithArguments()
    {
        var result = CommandDispatcher.ParseCommandLine("mem 0x1000 0x100");

        Assert.That(result, Is.EqualTo(new[] { "mem", "0x1000", "0x100" }));
    }

    /// <summary>
    /// Verifies that ParseCommandLine handles quoted arguments.
    /// </summary>
    [Test]
    public void ParseCommandLine_HandlesQuotedArguments()
    {
        var result = CommandDispatcher.ParseCommandLine("load \"file with spaces.bin\"");

        Assert.That(result, Is.EqualTo(new[] { "load", "file with spaces.bin" }));
    }

    /// <summary>
    /// Verifies that ParseCommandLine handles escaped characters.
    /// </summary>
    [Test]
    public void ParseCommandLine_HandlesEscapedCharacters()
    {
        var result = CommandDispatcher.ParseCommandLine("test arg\\\"with\\\"quotes");

        Assert.That(result, Is.EqualTo(new[] { "test", "arg\"with\"quotes" }));
    }

    /// <summary>
    /// Verifies that ParseCommandLine returns empty array for empty input.
    /// </summary>
    [Test]
    public void ParseCommandLine_ReturnsEmptyForEmptyInput()
    {
        var result = CommandDispatcher.ParseCommandLine(string.Empty);

        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Verifies that ParseCommandLine returns empty array for whitespace input.
    /// </summary>
    [Test]
    public void ParseCommandLine_ReturnsEmptyForWhitespaceInput()
    {
        var result = CommandDispatcher.ParseCommandLine("   ");

        Assert.That(result, Is.Empty);
    }

    private static CommandContext CreateTestContext(ICommandDispatcher dispatcher)
    {
        return new(dispatcher, TextWriter.Null, TextWriter.Null);
    }

    private sealed class TestCommand : CommandHandlerBase
    {
        public TestCommand(string name, string description, string[]? aliases = null)
            : base(name, description)
        {
            this.CommandAliases = aliases ?? [];
        }

        public override IReadOnlyList<string> Aliases => this.CommandAliases;

        public int ExecuteCount { get; private set; }

        public string[]? LastArgs { get; private set; }

        private string[] CommandAliases { get; }

        public override CommandResult Execute(ICommandContext context, string[] args)
        {
            this.ExecuteCount++;
            this.LastArgs = args;
            return CommandResult.Ok();
        }
    }
}