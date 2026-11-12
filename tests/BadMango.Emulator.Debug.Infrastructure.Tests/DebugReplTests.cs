// <copyright file="DebugReplTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for the <see cref="DebugRepl"/> class.
/// </summary>
[TestFixture]
public class DebugReplTests
{
    /// <summary>
    /// Verifies that ProcessLine executes registered command.
    /// </summary>
    [Test]
    public void ProcessLine_ExecutesRegisteredCommand()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestCommand("test", "Test command");
        dispatcher.Register(handler);
        var context = CreateTestDebugContext(dispatcher, out _, out _);
        var repl = new DebugRepl(dispatcher, context, new StringReader(string.Empty));

        var result = repl.ProcessLine("test");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(handler.ExecuteCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that ProcessLine returns error for unknown command.
    /// </summary>
    [Test]
    public void ProcessLine_ReturnsErrorForUnknownCommand()
    {
        var dispatcher = new CommandDispatcher();
        var context = CreateTestDebugContext(dispatcher, out _, out var errorWriter);
        var repl = new DebugRepl(dispatcher, context, new StringReader(string.Empty));

        var result = repl.ProcessLine("unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(errorWriter.ToString(), Does.Contain("Error:"));
        });
    }

    /// <summary>
    /// Verifies that ProcessLine returns success for empty input.
    /// </summary>
    [Test]
    public void ProcessLine_ReturnsSuccessForEmptyInput()
    {
        var dispatcher = new CommandDispatcher();
        var context = CreateTestDebugContext(dispatcher, out _, out _);
        var repl = new DebugRepl(dispatcher, context, new StringReader(string.Empty));

        var result = repl.ProcessLine(string.Empty);

        Assert.That(result.Success, Is.True);
    }

    /// <summary>
    /// Verifies that ProcessLine returns success for whitespace input.
    /// </summary>
    [Test]
    public void ProcessLine_ReturnsSuccessForWhitespaceInput()
    {
        var dispatcher = new CommandDispatcher();
        var context = CreateTestDebugContext(dispatcher, out _, out _);
        var repl = new DebugRepl(dispatcher, context, new StringReader(string.Empty));

        var result = repl.ProcessLine("   ");

        Assert.That(result.Success, Is.True);
    }

    /// <summary>
    /// Verifies that ProcessLine writes success message to output.
    /// </summary>
    [Test]
    public void ProcessLine_WritesSuccessMessageToOutput()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestCommand("test", "Test command", resultMessage: "Success!");
        dispatcher.Register(handler);
        var context = CreateTestDebugContext(dispatcher, out var outputWriter, out _);
        var repl = new DebugRepl(dispatcher, context, new StringReader(string.Empty));

        repl.ProcessLine("test");

        Assert.That(outputWriter.ToString(), Does.Contain("Success!"));
    }

    /// <summary>
    /// Verifies that Run executes commands until exit.
    /// </summary>
    [Test]
    public void Run_ExecutesCommandsUntilExit()
    {
        var dispatcher = new CommandDispatcher();
        var handler1 = new TestCommand("cmd1", "Command 1");
        var handler2 = new TestCommand("cmd2", "Command 2");
        dispatcher.Register(handler1);
        dispatcher.Register(handler2);
        dispatcher.Register(new ExitCommand());
        var input = new StringReader("cmd1\ncmd2\nexit\n");
        var context = CreateTestDebugContext(dispatcher, out _, out _);
        var repl = new DebugRepl(dispatcher, context, input) { ShowBanner = false };

        repl.Run();

        Assert.Multiple(() =>
        {
            Assert.That(handler1.ExecuteCount, Is.EqualTo(1));
            Assert.That(handler2.ExecuteCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that Run stops when input is exhausted.
    /// </summary>
    [Test]
    public void Run_StopsWhenInputExhausted()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestCommand("cmd", "Command");
        dispatcher.Register(handler);
        var input = new StringReader("cmd\ncmd\n");
        var context = CreateTestDebugContext(dispatcher, out _, out _);
        var repl = new DebugRepl(dispatcher, context, input) { ShowBanner = false };

        repl.Run();

        Assert.That(handler.ExecuteCount, Is.EqualTo(2));
    }

    /// <summary>
    /// Verifies that Run displays banner when ShowBanner is true.
    /// </summary>
    [Test]
    public void Run_DisplaysBannerWhenShowBannerIsTrue()
    {
        var dispatcher = new CommandDispatcher();
        var input = new StringReader(string.Empty);
        var context = CreateTestDebugContext(dispatcher, out var outputWriter, out _);
        var repl = new DebugRepl(dispatcher, context, input) { ShowBanner = true, IsInteractive = true };

        repl.Run();

        Assert.That(outputWriter.ToString(), Does.Contain("Emulator Debug Console"));
    }

    /// <summary>
    /// Verifies that Run does not display banner when ShowBanner is false.
    /// </summary>
    [Test]
    public void Run_DoesNotDisplayBannerWhenShowBannerIsFalse()
    {
        var dispatcher = new CommandDispatcher();
        var input = new StringReader(string.Empty);
        var context = CreateTestDebugContext(dispatcher, out var outputWriter, out _);
        var repl = new DebugRepl(dispatcher, context, input) { ShowBanner = false };

        repl.Run();

        Assert.That(outputWriter.ToString(), Does.Not.Contain("Emulator Debug Console"));
    }

    /// <summary>
    /// Verifies that Run displays prompt.
    /// </summary>
    [Test]
    public void Run_DisplaysPrompt()
    {
        var dispatcher = new CommandDispatcher();
        var input = new StringReader(string.Empty);
        var context = CreateTestDebugContext(dispatcher, out var outputWriter, out _);
        var repl = new DebugRepl(dispatcher, context, input, "dbg> ") { ShowBanner = false, IsInteractive = true };

        repl.Run();

        Assert.That(outputWriter.ToString(), Does.Contain("dbg> "));
    }

    /// <summary>
    /// Verifies that CreateConsoleRepl creates REPL with built-in commands.
    /// </summary>
    [Test]
    public void CreateConsoleRepl_CreatesReplWithBuiltInCommands()
    {
        var repl = DebugRepl.CreateConsoleRepl();

        // The REPL should have help, exit, version, and clear commands registered.
        // We verify this by executing the commands via ProcessLine and checking the results.
        var helpResult = repl.ProcessLine("help");
        var exitResult = repl.ProcessLine("exit");
        var versionResult = repl.ProcessLine("version");
        var clearResult = repl.ProcessLine("clear");

        Assert.Multiple(() =>
        {
            Assert.That(helpResult.Success, Is.True, "Help command should be registered and succeed.");
            Assert.That(exitResult.Success, Is.True, "Exit command should be registered and succeed.");
            Assert.That(versionResult.Success, Is.True, "Version command should be registered and succeed.");
            Assert.That(clearResult.Success, Is.True, "Clear command should be registered and succeed.");
        });
    }

    /// <summary>
    /// Verifies that Run displays machine information in banner.
    /// </summary>
    [Test]
    public void Run_DisplaysMachineInfoInBanner()
    {
        var dispatcher = new CommandDispatcher();
        var input = new StringReader(string.Empty);
        var context = CreateTestDebugContext(dispatcher, out var outputWriter, out _);
        var repl = new DebugRepl(dispatcher, context, input) { ShowBanner = true, IsInteractive = true };

        repl.Run();

        Assert.That(outputWriter.ToString(), Does.Contain("Machine:"));
    }

    /// <summary>
    /// Verifies that non-interactive input (StringReader) auto-suppresses banner and prompt.
    /// </summary>
    [Test]
    public void Run_AutoSuppressesForNonInteractiveInput()
    {
        var dispatcher = new CommandDispatcher();
        var input = new StringReader("exit\n");
        var context = CreateTestDebugContext(dispatcher, out var outputWriter, out _);

        // Do not set Show* explicitly; rely on auto-detect
        var repl = new DebugRepl(dispatcher, context, input);
        dispatcher.Register(new ExitCommand());

        repl.Run();

        var output = outputWriter.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Not.Contain("Emulator Debug Console"));
            Assert.That(output, Does.Not.Contain(">"));
            Assert.That(output, Does.Not.Contain("Goodbye!"));
        });
    }

    /// <summary>
    /// Verifies that IsInteractive override forces interactive behavior even with StringReader.
    /// </summary>
    [Test]
    public void Run_RespectsIsInteractiveOverride()
    {
        var dispatcher = new CommandDispatcher();
        var input = new StringReader(string.Empty);
        var context = CreateTestDebugContext(dispatcher, out var outputWriter, out _);
        var repl = new DebugRepl(dispatcher, context, input) { IsInteractive = true };

        repl.Run();

        Assert.That(outputWriter.ToString(), Does.Contain("Emulator Debug Console"));
    }

    /// <summary>
    /// Verifies PrintExitMessage controls whether Goodbye is printed.
    /// </summary>
    [Test]
    public void Run_PrintExitMessageControlsGoodbye()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new ExitCommand());
        var input = new StringReader("exit\n");
        var context = CreateTestDebugContext(dispatcher, out var outputWriter, out _);
        var repl = new DebugRepl(dispatcher, context, input) { PrintExitMessage = false, ShowBanner = false, IsInteractive = true };

        repl.Run();

        Assert.That(outputWriter.ToString(), Does.Not.Contain("Goodbye!"));
    }

    /// <summary>
    /// Verifies the lightweight ExecuteBatch path runs commands directly without interactive loop.
    /// </summary>
    [Test]
    public void ExecuteBatch_RunsCommandsAndStopsOnExit()
    {
        var dispatcher = new CommandDispatcher();
        var handler1 = new TestCommand("cmd1", "Command 1");
        var handler2 = new TestCommand("cmd2", "Command 2");
        dispatcher.Register(handler1);
        dispatcher.Register(handler2);
        dispatcher.Register(new ExitCommand());
        var context = CreateTestDebugContext(dispatcher, out _, out _);
        var repl = new DebugRepl(dispatcher, context, new StringReader(string.Empty)) { ShowBanner = false };

        var commands = new[] { "cmd1", "cmd2", "exit", "cmd3" };
        repl.ExecuteBatch(commands);

        Assert.Multiple(() =>
        {
            Assert.That(handler1.ExecuteCount, Is.EqualTo(1));
            Assert.That(handler2.ExecuteCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies ExecuteBatch stops when input exhausted (no explicit exit).
    /// </summary>
    [Test]
    public void ExecuteBatch_StopsOnExhaustion()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestCommand("cmd", "Command");
        dispatcher.Register(handler);
        var context = CreateTestDebugContext(dispatcher, out _, out _);
        var repl = new DebugRepl(dispatcher, context, new StringReader(string.Empty)) { ShowBanner = false };

        var commands = new[] { "cmd", "cmd" };
        repl.ExecuteBatch(commands);

        Assert.That(handler.ExecuteCount, Is.EqualTo(2));
    }

    private static DebugContext CreateTestDebugContext(ICommandDispatcher dispatcher, out StringWriter outputWriter, out StringWriter errorWriter)
    {
        outputWriter = new();
        errorWriter = new();
        return new(dispatcher, outputWriter, errorWriter);
    }

    private sealed class TestCommand : CommandHandlerBase
    {
        private readonly string? resultMessage;

        public TestCommand(string name, string description, string? resultMessage = null)
            : base(name, description)
        {
            this.resultMessage = resultMessage;
        }

        public int ExecuteCount { get; private set; }

        public override CommandResult Execute(ICommandContext context, string[] args)
        {
            this.ExecuteCount++;
            return this.resultMessage is not null
                ? CommandResult.Ok(this.resultMessage)
                : CommandResult.Ok();
        }
    }
}