// <copyright file="AboutCommandTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

/// <summary>
/// Unit tests for the <see cref="AboutCommand"/> class.
/// </summary>
[TestFixture]
public class AboutCommandTests
{
    /// <summary>
    /// Verifies that the command name is 'about'.
    /// </summary>
    [Test]
    public void Name_ReturnsAbout()
    {
        var command = new AboutCommand();

        Assert.That(command.Name, Is.EqualTo("about"));
    }

    /// <summary>
    /// Verifies that the command has a description.
    /// </summary>
    [Test]
    public void Description_IsNotEmpty()
    {
        var command = new AboutCommand();

        Assert.That(command.Description, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that Execute displays console output when no window manager is provided.
    /// </summary>
    [Test]
    public void Execute_WithoutWindowManager_DisplaysConsoleOutput()
    {
        var command = new AboutCommand();
        var context = CreateTestContext(out var outputWriter);

        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("BackPocket BASIC"));
            Assert.That(outputWriter.ToString(), Does.Contain("Copyright"));
        });
    }

    /// <summary>
    /// Verifies that Execute displays console output when window manager is provided but Avalonia is not running.
    /// </summary>
    [Test]
    public void Execute_WithWindowManagerButAvaloniaNotRunning_DisplaysConsoleOutput()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        windowManager.Setup(w => w.IsAvaloniaRunning).Returns(false);

        var command = new AboutCommand(windowManager.Object);
        var context = CreateTestContext(out var outputWriter);

        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("BackPocket BASIC"));
        });
    }

    /// <summary>
    /// Verifies that Execute calls ShowWindowAsync when Avalonia is running.
    /// </summary>
    [Test]
    public void Execute_WithWindowManagerAndAvaloniaRunning_CallsShowWindowAsync()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        windowManager.Setup(w => w.IsAvaloniaRunning).Returns(true);
        windowManager.Setup(w => w.ShowWindowAsync(It.IsAny<string>())).ReturnsAsync(true);

        var command = new AboutCommand(windowManager.Object);
        var context = CreateTestContext(out _);

        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Does.Contain("Opening"));
            windowManager.Verify(w => w.ShowWindowAsync("About"), Times.Once);
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
        windowManager.Setup(w => w.ShowWindowAsync(It.IsAny<string>())).Returns(tcs.Task);

        var command = new AboutCommand(windowManager.Object);
        var context = CreateTestContext(out _);

        // Execute should return immediately without blocking
        var result = command.Execute(context, []);

        Assert.That(result.Success, Is.True);

        // Complete the task to clean up
        tcs.SetResult(true);
    }

    /// <summary>
    /// Verifies that Synopsis returns a non-empty string.
    /// </summary>
    [Test]
    public void Synopsis_ReturnsNonEmptyString()
    {
        var command = new AboutCommand();

        Assert.That(command.Synopsis, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that DetailedDescription returns a non-empty string.
    /// </summary>
    [Test]
    public void DetailedDescription_ReturnsNonEmptyString()
    {
        var command = new AboutCommand();

        Assert.That(command.DetailedDescription, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that SeeAlso includes related commands.
    /// </summary>
    [Test]
    public void SeeAlso_IncludesRelatedCommands()
    {
        var command = new AboutCommand();

        Assert.Multiple(() =>
        {
            Assert.That(command.SeeAlso, Does.Contain("version"));
            Assert.That(command.SeeAlso, Does.Contain("help"));
        });
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