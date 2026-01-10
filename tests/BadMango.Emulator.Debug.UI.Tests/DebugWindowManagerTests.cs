// <copyright file="DebugWindowManagerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

/// <summary>
/// Unit tests for the <see cref="DebugWindowManager"/> class.
/// </summary>
[TestFixture]
public class DebugWindowManagerTests
{
    /// <summary>
    /// Verifies that IsAvaloniaRunning returns false when Avalonia is not running.
    /// </summary>
    [Test]
    public void IsAvaloniaRunning_WhenNoAvaloniaApp_ReturnsFalse()
    {
        var manager = new DebugWindowManager();

        Assert.That(manager.IsAvaloniaRunning, Is.False);
    }

    /// <summary>
    /// Verifies that GetAvailableWindowTypes returns expected window types.
    /// </summary>
    [Test]
    public void GetAvailableWindowTypes_ReturnsExpectedTypes()
    {
        var manager = new DebugWindowManager();

        var types = manager.GetAvailableWindowTypes().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(types, Does.Contain("About"));
            Assert.That(types, Does.Contain("CharacterPreview"));
            Assert.That(types, Does.Contain("StatusMonitor"));
        });
    }

    /// <summary>
    /// Verifies that IsWindowOpen returns false when no windows are open.
    /// </summary>
    [Test]
    public void IsWindowOpen_WhenNoWindowsOpen_ReturnsFalse()
    {
        var manager = new DebugWindowManager();

        Assert.That(manager.IsWindowOpen("About"), Is.False);
    }

    /// <summary>
    /// Verifies that CloseWindowAsync returns false when window is not open.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task CloseWindowAsync_WhenWindowNotOpen_ReturnsFalse()
    {
        var manager = new DebugWindowManager();

        var result = await manager.CloseWindowAsync("About");

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that CloseAllWindowsAsync completes without error when no windows open.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task CloseAllWindowsAsync_WhenNoWindowsOpen_CompletesSuccessfully()
    {
        var manager = new DebugWindowManager();

        await manager.CloseAllWindowsAsync();

        // No exception should be thrown
        Assert.Pass();
    }
}