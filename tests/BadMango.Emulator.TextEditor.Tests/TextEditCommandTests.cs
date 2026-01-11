// <copyright file="TextEditCommandTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.TextEditor.Tests;

using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Unit tests for the <see cref="TextEditCommand"/> class.
/// </summary>
[TestFixture]
public class TextEditCommandTests
{
    /// <summary>
    /// Verifies that the command name is 'textedit'.
    /// </summary>
    [Test]
    public void Name_ReturnsTextedit()
    {
        var command = new TextEditCommand();

        Assert.That(command.Name, Is.EqualTo("textedit"));
    }

    /// <summary>
    /// Verifies that the command has aliases.
    /// </summary>
    [Test]
    public void Aliases_ContainsEditAndTe()
    {
        var command = new TextEditCommand();

        Assert.Multiple(() =>
        {
            Assert.That(command.Aliases, Contains.Item("edit"));
            Assert.That(command.Aliases, Contains.Item("te"));
        });
    }

    /// <summary>
    /// Verifies that the command has a description.
    /// </summary>
    [Test]
    public void Description_IsNotEmpty()
    {
        var command = new TextEditCommand();

        Assert.That(command.Description, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that Execute fails when no window manager is provided.
    /// </summary>
    [Test]
    public void Execute_WithoutWindowManager_ReturnsFail()
    {
        var command = new TextEditCommand();
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var context = CreateTestContext(outputWriter, errorWriter);

        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(outputWriter.ToString(), Does.Contain("requires Avalonia UI"));
        });
    }

    /// <summary>
    /// Verifies that Execute calls ShowWindowAsync when window manager is provided.
    /// </summary>
    [Test]
    public void Execute_WithWindowManager_CallsShowWindowAsync()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        windowManager.Setup(w => w.ShowWindowAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(true);

        var command = new TextEditCommand(windowManager.Object);
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var context = CreateTestContext(outputWriter, errorWriter);

        var result = command.Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Does.Contain("Opening"));
            windowManager.Verify(w => w.ShowWindowAsync("TextEditor", null), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that Execute passes file path to ShowWindowAsync.
    /// </summary>
    [Test]
    public void Execute_WithFilePath_PassesPathToShowWindowAsync()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        windowManager.Setup(w => w.ShowWindowAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(true);

        var command = new TextEditCommand(windowManager.Object);
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var context = CreateTestContext(outputWriter, errorWriter);

        command.Execute(context, ["test.s"]);

        windowManager.Verify(w => w.ShowWindowAsync("TextEditor", "test.s"), Times.Once);
    }

    /// <summary>
    /// Verifies that Synopsis returns a non-empty string.
    /// </summary>
    [Test]
    public void Synopsis_ReturnsNonEmptyString()
    {
        var command = new TextEditCommand();

        Assert.That(command.Synopsis, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that DetailedDescription returns a non-empty string.
    /// </summary>
    [Test]
    public void DetailedDescription_ReturnsNonEmptyString()
    {
        var command = new TextEditCommand();

        Assert.That(command.DetailedDescription, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that SeeAlso includes related commands.
    /// </summary>
    [Test]
    public void SeeAlso_IncludesRelatedCommands()
    {
        var command = new TextEditCommand();

        Assert.That(command.SeeAlso, Contains.Item("help"));
    }

    /// <summary>
    /// Verifies that Options includes filepath option.
    /// </summary>
    [Test]
    public void Options_ContainsFilepathOption()
    {
        var command = new TextEditCommand();

        Assert.That(command.Options, Has.Count.GreaterThan(0));
        Assert.That(command.Options[0].Name, Is.EqualTo("filepath"));
    }

    /// <summary>
    /// Verifies that Execute does not block when showing window.
    /// </summary>
    [Test]
    public void Execute_DoesNotBlock_WhenShowingWindow()
    {
        var tcs = new TaskCompletionSource<bool>();
        var windowManager = new Mock<IDebugWindowManager>();

        // Return a task that never completes to verify we don't await it
        windowManager.Setup(w => w.ShowWindowAsync(It.IsAny<string>(), It.IsAny<object?>())).Returns(tcs.Task);

        var command = new TextEditCommand(windowManager.Object);
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var context = CreateTestContext(outputWriter, errorWriter);

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