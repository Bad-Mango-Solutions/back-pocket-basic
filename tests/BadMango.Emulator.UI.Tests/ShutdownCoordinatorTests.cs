// <copyright file="ShutdownCoordinatorTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using BadMango.Emulator.UI.Abstractions;
using BadMango.Emulator.UI.Services;

using Moq;

/// <summary>
/// Tests for <see cref="ShutdownCoordinator"/>.
/// </summary>
[TestFixture]
public class ShutdownCoordinatorTests
{
    private Mock<IWindowManager> mockWindowManager = null!;
    private ShutdownCoordinator coordinator = null!;

    /// <summary>
    /// Sets up the test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        mockWindowManager = new Mock<IWindowManager>();
        mockWindowManager.Setup(wm => wm.CloseAllWindowsAsync()).Returns(Task.CompletedTask);

        coordinator = new ShutdownCoordinator(mockWindowManager.Object);
    }

    /// <summary>
    /// Tests that RequestShutdownAsync closes all windows.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RequestShutdownAsync_ClosesAllWindows()
    {
        // Act
        await coordinator.RequestShutdownAsync();

        // Assert
        mockWindowManager.Verify(wm => wm.CloseAllWindowsAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that RequestShutdownAsync returns true by default.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RequestShutdownAsync_ReturnsTrue()
    {
        // Act
        var result = await coordinator.RequestShutdownAsync();

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Tests that GetUnsavedWorkAsync returns empty list when no providers.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetUnsavedWorkAsync_NoProviders_ReturnsEmptyList()
    {
        // Act
        var result = await coordinator.GetUnsavedWorkAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Tests that registered provider is called.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetUnsavedWorkAsync_CallsRegisteredProviders()
    {
        // Arrange
        var unsavedItems = new List<UnsavedWorkItem>
        {
            new() { Name = "Test File", Description = "Unsaved changes" },
        };
        coordinator.RegisterUnsavedWorkProvider(() => Task.FromResult<IReadOnlyList<UnsavedWorkItem>>(unsavedItems));

        // Act
        var result = await coordinator.GetUnsavedWorkAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Test File"));
    }

    /// <summary>
    /// Tests that disposing the provider registration removes it.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RegisterUnsavedWorkProvider_DisposingRemovesProvider()
    {
        // Arrange
        var unsavedItems = new List<UnsavedWorkItem>
        {
            new() { Name = "Test File", Description = "Unsaved changes" },
        };
        var registration = coordinator.RegisterUnsavedWorkProvider(
            () => Task.FromResult<IReadOnlyList<UnsavedWorkItem>>(unsavedItems));

        // Act
        registration.Dispose();
        var result = await coordinator.GetUnsavedWorkAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Tests that multiple providers are aggregated.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetUnsavedWorkAsync_AggregatesMultipleProviders()
    {
        // Arrange
        coordinator.RegisterUnsavedWorkProvider(() => Task.FromResult<IReadOnlyList<UnsavedWorkItem>>(
            new[] { new UnsavedWorkItem { Name = "File1", Description = "Changes1" } }));
        coordinator.RegisterUnsavedWorkProvider(() => Task.FromResult<IReadOnlyList<UnsavedWorkItem>>(
            new[] { new UnsavedWorkItem { Name = "File2", Description = "Changes2" } }));

        // Act
        var result = await coordinator.GetUnsavedWorkAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Tests that a failing provider does not prevent other providers from running.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetUnsavedWorkAsync_ProviderThrows_OtherProvidersStillRun()
    {
        // Arrange
        coordinator.RegisterUnsavedWorkProvider(() => throw new InvalidOperationException("Test error"));
        coordinator.RegisterUnsavedWorkProvider(() => Task.FromResult<IReadOnlyList<UnsavedWorkItem>>(
            new[] { new UnsavedWorkItem { Name = "File1", Description = "Changes1" } }));

        // Act
        var result = await coordinator.GetUnsavedWorkAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Tests that RegisterUnsavedWorkProvider throws when provider is null.
    /// </summary>
    [Test]
    public void RegisterUnsavedWorkProvider_NullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => coordinator.RegisterUnsavedWorkProvider(null!));
    }
}