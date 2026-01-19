// <copyright file="SchedMonCommandTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

/// <summary>
/// Unit tests for the <see cref="SchedMonCommand"/> class.
/// </summary>
[TestFixture]
public class SchedMonCommandTests
{
    /// <summary>
    /// Verifies that the command name is 'schedmon'.
    /// </summary>
    [Test]
    public void Name_ReturnsSchedmon()
    {
        var command = new SchedMonCommand();

        Assert.That(command.Name, Is.EqualTo("schedmon"));
    }

    /// <summary>
    /// Verifies that the command has a description.
    /// </summary>
    [Test]
    public void Description_IsNotEmpty()
    {
        var command = new SchedMonCommand();

        Assert.That(command.Description, Is.Not.Empty);
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
        var command = new SchedMonCommand();

        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("window manager"));
        });
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

        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var context = CreateTestContext(outputWriter, errorWriter);
        var command = new SchedMonCommand(windowManager.Object);

        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Does.Contain("Opening"));
            windowManager.Verify(w => w.ShowWindowAsync("ScheduleMonitor", It.IsAny<object?>()), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that Synopsis returns a non-empty string.
    /// </summary>
    [Test]
    public void Synopsis_ReturnsNonEmptyString()
    {
        var command = new SchedMonCommand();

        Assert.That(command.Synopsis, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that DetailedDescription returns a non-empty string.
    /// </summary>
    [Test]
    public void DetailedDescription_ReturnsNonEmptyString()
    {
        var command = new SchedMonCommand();

        Assert.That(command.DetailedDescription, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that SeeAlso includes related commands.
    /// </summary>
    [Test]
    public void SeeAlso_IncludesRelatedCommands()
    {
        var command = new SchedMonCommand();

        Assert.Multiple(() =>
        {
            Assert.That(command.SeeAlso, Does.Contain("statmon"));
            Assert.That(command.SeeAlso, Does.Contain("trapmon"));
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

        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var context = CreateTestContext(outputWriter, errorWriter);
        var command = new SchedMonCommand(windowManager.Object);

        // Execute should return immediately without blocking
        var result = command.Execute(context, []);

        Assert.That(result.Success, Is.True);

        // Complete the task to clean up
        tcs.SetResult(true);
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