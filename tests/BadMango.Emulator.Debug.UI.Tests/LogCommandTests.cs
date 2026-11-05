// <copyright file="LogCommandTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

/// <summary>
/// Unit tests for the <see cref="LogCommand"/> class.
/// </summary>
[TestFixture]
public class LogCommandTests
{
    /// <summary>
    /// Verifies that the command name is <c>log</c>.
    /// </summary>
    [Test]
    public void Name_ReturnsLog()
    {
        var command = new LogCommand();

        Assert.That(command.Name, Is.EqualTo("log"));
    }

    /// <summary>
    /// Verifies that Execute returns an error when no window manager is provided.
    /// </summary>
    [Test]
    public void Execute_WithoutWindowManager_ReturnsError()
    {
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var context = CreateTestContext(outputWriter, errorWriter);
        var command = new LogCommand();
        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("window manager"));
        });
    }

    /// <summary>
    /// Verifies that Execute opens the log window with no arguments.
    /// </summary>
    [Test]
    public void Execute_WithNoArguments_CallsShowWindowAsync()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        windowManager.Setup(w => w.ShowWindowAsync(It.IsAny<string>())).ReturnsAsync(true);

        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var context = CreateTestContext(outputWriter, errorWriter);
        var command = new LogCommand(windowManager.Object);
        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Does.Contain("Opening"));
            windowManager.Verify(w => w.ShowWindowAsync("Log"), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that <c>window</c> subcommand opens the log window.
    /// </summary>
    [Test]
    public void Execute_WithWindowArgument_CallsShowWindowAsync()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        windowManager.Setup(w => w.ShowWindowAsync(It.IsAny<string>())).ReturnsAsync(true);

        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var context = CreateTestContext(outputWriter, errorWriter);
        var command = new LogCommand(windowManager.Object);
        var result = command.Execute(context, ["window"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            windowManager.Verify(w => w.ShowWindowAsync("Log"), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that unknown arguments return usage errors.
    /// </summary>
    [Test]
    public void Execute_WithUnknownArgument_ReturnsError()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var context = CreateTestContext(outputWriter, errorWriter);
        var command = new LogCommand(windowManager.Object);
        var result = command.Execute(context, ["bogus"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Usage"));
        });
    }

    /// <summary>
    /// Verifies that an extra argument after the window subcommand is rejected.
    /// </summary>
    [Test]
    public void Execute_WithExtraWindowArgument_ReturnsError()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var context = CreateTestContext(outputWriter, errorWriter);
        var command = new LogCommand(windowManager.Object);
        var result = command.Execute(context, ["window", "extra"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Usage"));
            windowManager.Verify(w => w.ShowWindowAsync(It.IsAny<string>()), Times.Never);
        });
    }

    private static ICommandContext CreateTestContext(StringWriter outputWriter, StringWriter errorWriter)
    {
        var mockDispatcher = new Mock<ICommandDispatcher>();
        var mockContext = new Mock<ICommandContext>();
        mockContext.Setup(c => c.Output).Returns(outputWriter);
        mockContext.Setup(c => c.Error).Returns(errorWriter);
        mockContext.Setup(c => c.Dispatcher).Returns(mockDispatcher.Object);
        return mockContext.Object;
    }
}