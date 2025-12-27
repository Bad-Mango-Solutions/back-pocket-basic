// <copyright file="WindowManagerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using Avalonia.Controls;

using BadMango.Emulator.UI.Abstractions;
using BadMango.Emulator.UI.Models;
using BadMango.Emulator.UI.Services;

using Moq;

/// <summary>
/// Tests for <see cref="WindowManager"/>.
/// </summary>
[TestFixture]
public class WindowManagerTests
{
    private WindowManager windowManager = null!;
    private Mock<IPopOutWindow> mockWindow = null!;
    private string testLayoutPath = null!;

    /// <summary>
    /// Sets up the test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        mockWindow = new Mock<IPopOutWindow>();
        mockWindow.Setup(w => w.WindowId).Returns("test-window-1");
        mockWindow.Setup(w => w.ComponentType).Returns(PopOutComponent.VideoDisplay);
        mockWindow.Setup(w => w.State).Returns(WindowState.Normal);
        mockWindow.Setup(w => w.CloseAsync(It.IsAny<bool>())).Returns(Task.CompletedTask);
        mockWindow.Setup(w => w.GetStateInfo()).Returns(new WindowStateInfo
        {
            ComponentType = PopOutComponent.VideoDisplay,
            IsPopOut = true,
        });

        testLayoutPath = Path.Combine(Path.GetTempPath(), $"backpocket-ui-test-{Guid.NewGuid()}");

        windowManager = new WindowManager(
            (component, machineId) => mockWindow.Object,
            logger: null,
            layoutStoragePath: testLayoutPath);
    }

    /// <summary>
    /// Tears down the test environment.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testLayoutPath))
        {
            Directory.Delete(testLayoutPath, recursive: true);
        }
    }

    /// <summary>
    /// Tests that CreatePopOutAsync creates and returns a window.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task CreatePopOutAsync_CreatesWindow()
    {
        // Act
        var window = await windowManager.CreatePopOutAsync(PopOutComponent.VideoDisplay);

        // Assert
        Assert.That(window, Is.Not.Null);
        Assert.That(windowManager.PopOutWindows, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Tests that CreatePopOutAsync raises WindowCreated event.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task CreatePopOutAsync_RaisesWindowCreatedEvent()
    {
        // Arrange
        PopOutWindowEventArgs? eventArgs = null;
        windowManager.WindowCreated += (_, args) => eventArgs = args;

        // Act
        await windowManager.CreatePopOutAsync(PopOutComponent.VideoDisplay);

        // Assert
        Assert.That(eventArgs, Is.Not.Null);
        Assert.That(eventArgs!.Window, Is.EqualTo(mockWindow.Object));
    }

    /// <summary>
    /// Tests that DockWindowAsync closes the window with dock flag.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task DockWindowAsync_ClosesWindowWithDockFlag()
    {
        // Arrange
        await windowManager.CreatePopOutAsync(PopOutComponent.VideoDisplay);

        // Act
        await windowManager.DockWindowAsync(mockWindow.Object);

        // Assert
        mockWindow.Verify(w => w.CloseAsync(true), Times.Once);
        Assert.That(windowManager.PopOutWindows, Is.Empty);
    }

    /// <summary>
    /// Tests that DockWindowAsync raises WindowClosed event.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task DockWindowAsync_RaisesWindowClosedEvent()
    {
        // Arrange
        await windowManager.CreatePopOutAsync(PopOutComponent.VideoDisplay);
        PopOutWindowEventArgs? eventArgs = null;
        windowManager.WindowClosed += (_, args) => eventArgs = args;

        // Act
        await windowManager.DockWindowAsync(mockWindow.Object);

        // Assert
        Assert.That(eventArgs, Is.Not.Null);
    }

    /// <summary>
    /// Tests that CloseAllWindowsAsync closes all windows.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task CloseAllWindowsAsync_ClosesAllWindows()
    {
        // Arrange
        await windowManager.CreatePopOutAsync(PopOutComponent.VideoDisplay);
        await windowManager.CreatePopOutAsync(PopOutComponent.DebugConsole);

        // Act
        await windowManager.CloseAllWindowsAsync();

        // Assert
        Assert.That(windowManager.PopOutWindows, Is.Empty);
    }

    /// <summary>
    /// Tests that FindWindow returns the correct window.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task FindWindow_ReturnsCorrectWindow()
    {
        // Arrange
        await windowManager.CreatePopOutAsync(PopOutComponent.VideoDisplay);

        // Act
        var found = windowManager.FindWindow(PopOutComponent.VideoDisplay);

        // Assert
        Assert.That(found, Is.EqualTo(mockWindow.Object));
    }

    /// <summary>
    /// Tests that FindWindow returns null when not found.
    /// </summary>
    [Test]
    public void FindWindow_NotFound_ReturnsNull()
    {
        // Act
        var found = windowManager.FindWindow(PopOutComponent.VideoDisplay);

        // Assert
        Assert.That(found, Is.Null);
    }

    /// <summary>
    /// Tests that SaveWindowStatesAsync creates the layout file.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SaveWindowStatesAsync_CreatesLayoutFile()
    {
        // Arrange
        await windowManager.CreatePopOutAsync(PopOutComponent.VideoDisplay);

        // Act
        await windowManager.SaveWindowStatesAsync("test-profile");

        // Assert
        var layoutFile = Path.Combine(testLayoutPath, "layout-test-profile.json");
        Assert.That(File.Exists(layoutFile), Is.True);
    }

    /// <summary>
    /// Tests that DockWindowAsync throws when window is null.
    /// </summary>
    [Test]
    public void DockWindowAsync_NullWindow_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => windowManager.DockWindowAsync(null!));
    }

    /// <summary>
    /// Tests that SaveWindowStatesAsync throws when profileId is null.
    /// </summary>
    [Test]
    public void SaveWindowStatesAsync_NullProfileId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => windowManager.SaveWindowStatesAsync(null!));
    }

    /// <summary>
    /// Tests that SaveWindowStatesAsync throws when profileId is empty or whitespace.
    /// </summary>
    /// <param name="profileId">The profile ID to test.</param>
    [TestCase("")]
    [TestCase("   ")]
    public void SaveWindowStatesAsync_EmptyProfileId_ThrowsArgumentException(string profileId)
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => windowManager.SaveWindowStatesAsync(profileId));
    }

    /// <summary>
    /// Tests that RestoreWindowStatesAsync throws when profileId is null.
    /// </summary>
    [Test]
    public void RestoreWindowStatesAsync_NullProfileId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => windowManager.RestoreWindowStatesAsync(null!));
    }

    /// <summary>
    /// Tests that RestoreWindowStatesAsync throws when profileId is empty or whitespace.
    /// </summary>
    /// <param name="profileId">The profile ID to test.</param>
    [TestCase("")]
    [TestCase("   ")]
    public void RestoreWindowStatesAsync_EmptyProfileId_ThrowsArgumentException(string profileId)
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => windowManager.RestoreWindowStatesAsync(profileId));
    }
}