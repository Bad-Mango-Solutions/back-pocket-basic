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
        var context = CreateTestContext(out var outputWriter, out var errorWriter);

        try
        {
            var command = new LogCommand();
            var result = command.Execute(context, []);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Message, Does.Contain("window manager"));
            });
        }
        finally
        {
            outputWriter.Dispose();
            errorWriter.Dispose();
        }
    }

    /// <summary>
    /// Verifies that Execute opens the log window with no arguments.
    /// </summary>
    [Test]
    public void Execute_WithNoArguments_CallsShowWindowAsync()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        windowManager.Setup(w => w.ShowWindowAsync(It.IsAny<string>())).ReturnsAsync(true);

        var context = CreateTestContext(out var outputWriter, out var errorWriter);

        try
        {
            var command = new LogCommand(windowManager.Object);
            var result = command.Execute(context, []);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Message, Does.Contain("Opening"));
                windowManager.Verify(w => w.ShowWindowAsync("Log"), Times.Once);
            });
        }
        finally
        {
            outputWriter.Dispose();
            errorWriter.Dispose();
        }
    }

    /// <summary>
    /// Verifies that <c>window</c> subcommand opens the log window.
    /// </summary>
    [Test]
    public void Execute_WithWindowArgument_CallsShowWindowAsync()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        windowManager.Setup(w => w.ShowWindowAsync(It.IsAny<string>())).ReturnsAsync(true);

        var context = CreateTestContext(out var outputWriter, out var errorWriter);

        try
        {
            var command = new LogCommand(windowManager.Object);
            var result = command.Execute(context, ["window"]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                windowManager.Verify(w => w.ShowWindowAsync("Log"), Times.Once);
            });
        }
        finally
        {
            outputWriter.Dispose();
            errorWriter.Dispose();
        }
    }

    /// <summary>
    /// Verifies that unknown arguments return usage errors.
    /// </summary>
    [Test]
    public void Execute_WithUnknownArgument_ReturnsError()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        var context = CreateTestContext(out var outputWriter, out var errorWriter);

        try
        {
            var command = new LogCommand(windowManager.Object);
            var result = command.Execute(context, ["bogus"]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Message, Does.Contain("Usage"));
            });
        }
        finally
        {
            outputWriter.Dispose();
            errorWriter.Dispose();
        }
    }

    private static ICommandContext CreateTestContext(out StringWriter outputWriter, out StringWriter errorWriter)
    {
        outputWriter = new StringWriter();
        errorWriter = new StringWriter();
        var mockDispatcher = new Mock<ICommandDispatcher>();
        var mockContext = new Mock<ICommandContext>();
        mockContext.Setup(c => c.Output).Returns(outputWriter);
        mockContext.Setup(c => c.Error).Returns(errorWriter);
        mockContext.Setup(c => c.Dispatcher).Returns(mockDispatcher.Object);
        return mockContext.Object;
    }
}