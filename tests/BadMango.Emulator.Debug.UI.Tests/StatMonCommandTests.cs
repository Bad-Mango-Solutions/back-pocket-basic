// <copyright file="StatMonCommandTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

/// <summary>
/// Unit tests for the <see cref="StatMonCommand"/> class.
/// </summary>
[TestFixture]
public class StatMonCommandTests
{
    /// <summary>
    /// Verifies that the command name is 'statmon'.
    /// </summary>
    [Test]
    public void Name_ReturnsStatmon()
    {
        var command = new StatMonCommand();

        Assert.That(command.Name, Is.EqualTo("statmon"));
    }

    /// <summary>
    /// Verifies that the command has a description.
    /// </summary>
    [Test]
    public void Description_IsNotEmpty()
    {
        var command = new StatMonCommand();

        Assert.That(command.Description, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that Execute returns an error when no window manager is provided.
    /// </summary>
    [Test]
    public void Execute_WithoutWindowManager_ReturnsError()
    {
        var command = new StatMonCommand();
        var context = CreateTestContext(out var outputWriter);

        try
        {
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
        }
    }

    /// <summary>
    /// Verifies that Execute calls ShowWindowAsync when window manager is available.
    /// </summary>
    [Test]
    public void Execute_WithWindowManager_CallsShowWindowAsync()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        windowManager.Setup(w => w.IsAvaloniaRunning).Returns(true);
        windowManager.Setup(w => w.ShowWindowAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(true);

        var command = new StatMonCommand(windowManager.Object);
        var context = CreateTestContext(out var outputWriter);

        try
        {
            var result = command.Execute(context, []);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Message, Does.Contain("Opening"));
                windowManager.Verify(w => w.ShowWindowAsync("StatusMonitor", It.IsAny<object?>()), Times.Once);
            });
        }
        finally
        {
            outputWriter.Dispose();
        }
    }

    /// <summary>
    /// Verifies that Synopsis returns a non-empty string.
    /// </summary>
    [Test]
    public void Synopsis_ReturnsNonEmptyString()
    {
        var command = new StatMonCommand();

        Assert.That(command.Synopsis, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that DetailedDescription returns a non-empty string.
    /// </summary>
    [Test]
    public void DetailedDescription_ReturnsNonEmptyString()
    {
        var command = new StatMonCommand();

        Assert.That(command.DetailedDescription, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that SeeAlso includes related commands.
    /// </summary>
    [Test]
    public void SeeAlso_IncludesRelatedCommands()
    {
        var command = new StatMonCommand();

        Assert.Multiple(() =>
        {
            Assert.That(command.SeeAlso, Does.Contain("regs"));
            Assert.That(command.SeeAlso, Does.Contain("about"));
        });
    }

    /// <summary>
    /// Verifies that Execute does not block when showing window.
    /// </summary>
    [Test]
    public void Execute_DoesNotBlock_WhenShowingWindow()
    {
        var tcs = new TaskCompletionSource<bool>();
        var windowManager = new Mock<IDebugWindowManager>();
        windowManager.Setup(w => w.IsAvaloniaRunning).Returns(true);

        // Return a task that never completes to verify we don't await it
        windowManager.Setup(w => w.ShowWindowAsync(It.IsAny<string>(), It.IsAny<object?>())).Returns(tcs.Task);

        var command = new StatMonCommand(windowManager.Object);
        var context = CreateTestContext(out var outputWriter);

        try
        {
            // Execute should return immediately without blocking
            var result = command.Execute(context, []);

            Assert.That(result.Success, Is.True);

            // Complete the task to clean up
            tcs.SetResult(true);
        }
        finally
        {
            outputWriter.Dispose();
        }
    }

    private static ICommandContext CreateTestContext(out StringWriter outputWriter)
    {
        outputWriter = new StringWriter();
        var mockDispatcher = new Mock<ICommandDispatcher>();
        var mockContext = new Mock<ICommandContext>();
        mockContext.Setup(c => c.Output).Returns(outputWriter);
        mockContext.Setup(c => c.Error).Returns(new StringWriter());
        mockContext.Setup(c => c.Dispatcher).Returns(mockDispatcher.Object);
        return mockContext.Object;
    }
}