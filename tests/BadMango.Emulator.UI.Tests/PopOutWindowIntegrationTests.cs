// <copyright file="PopOutWindowIntegrationTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using Avalonia.Controls;

using BadMango.Emulator.UI.Abstractions;
using BadMango.Emulator.UI.Abstractions.Events;
using BadMango.Emulator.UI.Models;
using BadMango.Emulator.UI.Services;
using BadMango.Emulator.UI.ViewModels;
using BadMango.Emulator.UI.Views;

using Moq;

/// <summary>
/// Integration tests for pop-out window components working together.
/// </summary>
[TestFixture]
public class PopOutWindowIntegrationTests
{
    private string testLayoutPath = null!;

    /// <summary>
    /// Sets up the test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        testLayoutPath = Path.Combine(Path.GetTempPath(), $"backpocket-integration-test-{Guid.NewGuid()}");
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
    /// Tests that WindowManager and EventAggregator work together for window creation notifications.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WindowManager_WithEventAggregator_PublishesWindowCreatedEvent()
    {
        // Arrange
        var eventAggregator = new EventAggregator();
        var mockWindow = CreateMockPopOutWindow(PopOutComponent.VideoDisplay);
        var windowManager = new WindowManager(
            (component, machineId) => mockWindow.Object,
            layoutStoragePath: testLayoutPath);

        PopOutWindowEventArgs? receivedEvent = null;
        windowManager.WindowCreated += (_, args) =>
        {
            receivedEvent = args;
            eventAggregator.Publish(new WindowFocusRequestEvent(args.Window.ComponentType, args.Window.MachineId));
        };

        WindowFocusRequestEvent? focusEvent = null;
        eventAggregator.Subscribe<WindowFocusRequestEvent>(e => focusEvent = e);

        // Act
        await windowManager.CreatePopOutAsync(PopOutComponent.VideoDisplay);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(receivedEvent, Is.Not.Null);
            Assert.That(focusEvent, Is.Not.Null);
            Assert.That(focusEvent!.ComponentType, Is.EqualTo(PopOutComponent.VideoDisplay));
        });
    }

    /// <summary>
    /// Tests that WindowManager and ShutdownCoordinator work together for graceful shutdown.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ShutdownCoordinator_WithWindowManager_ClosesAllWindowsOnShutdown()
    {
        // Arrange
        var mockWindow = CreateMockPopOutWindow(PopOutComponent.VideoDisplay);
        var windowManager = new WindowManager(
            (component, machineId) => mockWindow.Object,
            layoutStoragePath: testLayoutPath);

        var shutdownCoordinator = new ShutdownCoordinator(windowManager);

        await windowManager.CreatePopOutAsync(PopOutComponent.VideoDisplay);
        await windowManager.CreatePopOutAsync(PopOutComponent.DebugConsole);

        // Act
        var result = await shutdownCoordinator.RequestShutdownAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(windowManager.PopOutWindows, Is.Empty);
        });
    }

    /// <summary>
    /// Tests that EventAggregator correctly broadcasts machine state changes to multiple subscribers.
    /// </summary>
    [Test]
    public void EventAggregator_MachineStateChange_BroadcastsToAllWindows()
    {
        // Arrange
        var eventAggregator = new EventAggregator();
        var receivedEvents = new List<MachineStateChangedEvent>();

        // Simulate multiple windows subscribing to state changes
        eventAggregator.Subscribe<MachineStateChangedEvent>(e => receivedEvents.Add(e));
        eventAggregator.Subscribe<MachineStateChangedEvent>(e => receivedEvents.Add(e));
        eventAggregator.Subscribe<MachineStateChangedEvent>(e => receivedEvents.Add(e));

        // Act
        eventAggregator.Publish(new MachineStateChangedEvent("machine-1", "Running"));

        // Assert
        Assert.That(receivedEvents, Has.Count.EqualTo(3));
        Assert.That(receivedEvents.All(e => e.MachineId == "machine-1" && e.NewState == "Running"), Is.True);
    }

    /// <summary>
    /// Tests that WindowManager save and restore cycle preserves window state.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WindowManager_SaveAndRestore_PreservesWindowState()
    {
        // Arrange - Create windows and save state
        var mockWindow = CreateMockPopOutWindow(PopOutComponent.VideoDisplay, "machine-1");
        var windowManager1 = new WindowManager(
            (component, machineId) => mockWindow.Object,
            layoutStoragePath: testLayoutPath);

        await windowManager1.CreatePopOutAsync(PopOutComponent.VideoDisplay, "machine-1");
        await windowManager1.SaveWindowStatesAsync("test-profile");

        // Act - Create new window manager and restore state
        int windowsRestored = 0;
        var mockWindowForRestore = CreateMockPopOutWindow(PopOutComponent.VideoDisplay, "machine-1");
        var windowManager2 = new WindowManager(
            (component, machineId) =>
            {
                windowsRestored++;
                return mockWindowForRestore.Object;
            },
            layoutStoragePath: testLayoutPath);

        await windowManager2.RestoreWindowStatesAsync("test-profile");

        // Assert
        Assert.That(windowsRestored, Is.EqualTo(1));
        Assert.That(windowManager2.PopOutWindows, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Tests that ShutdownCoordinator aggregates unsaved work from multiple providers.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ShutdownCoordinator_AggregatesUnsavedWorkFromMultipleSources()
    {
        // Arrange
        var mockWindowManager = new Mock<IWindowManager>();
        mockWindowManager.Setup(wm => wm.CloseAllWindowsAsync()).Returns(Task.CompletedTask);

        var shutdownCoordinator = new ShutdownCoordinator(mockWindowManager.Object);

        // Simulate multiple components registering unsaved work
        shutdownCoordinator.RegisterUnsavedWorkProvider(() => Task.FromResult<IReadOnlyList<UnsavedWorkItem>>(
            new[] { new UnsavedWorkItem { Name = "Editor 1", Description = "Unsaved assembly code" } }));

        shutdownCoordinator.RegisterUnsavedWorkProvider(() => Task.FromResult<IReadOnlyList<UnsavedWorkItem>>(
            new[] { new UnsavedWorkItem { Name = "Editor 2", Description = "Unsaved hex data" } }));

        // Act
        var unsavedWork = await shutdownCoordinator.GetUnsavedWorkAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(unsavedWork, Has.Count.EqualTo(2));
            Assert.That(unsavedWork.Any(w => w.Name == "Editor 1"), Is.True);
            Assert.That(unsavedWork.Any(w => w.Name == "Editor 2"), Is.True);
        });
    }

    /// <summary>
    /// Tests that EventAggregator handles breakpoint hit events correctly.
    /// </summary>
    [Test]
    public void EventAggregator_BreakpointHit_NotifiesDebugConsole()
    {
        // Arrange
        var eventAggregator = new EventAggregator();
        BreakpointHitEvent? receivedEvent = null;
        eventAggregator.Subscribe<BreakpointHitEvent>(e => receivedEvent = e);

        // Act
        eventAggregator.Publish(new BreakpointHitEvent("machine-1", 0x1234));

        // Assert
        Assert.That(receivedEvent, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(receivedEvent!.MachineId, Is.EqualTo("machine-1"));
            Assert.That(receivedEvent.Address, Is.EqualTo(0x1234));
        });
    }

    /// <summary>
    /// Tests that EventAggregator handles display mode changes correctly.
    /// </summary>
    [Test]
    public void EventAggregator_DisplayModeChanged_NotifiesVideoDisplay()
    {
        // Arrange
        var eventAggregator = new EventAggregator();
        DisplayModeChangedEvent? receivedEvent = null;
        eventAggregator.Subscribe<DisplayModeChangedEvent>(e => receivedEvent = e);

        // Act
        eventAggregator.Publish(new DisplayModeChangedEvent("machine-1", "HiRes"));

        // Assert
        Assert.That(receivedEvent, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(receivedEvent!.MachineId, Is.EqualTo("machine-1"));
            Assert.That(receivedEvent.NewMode, Is.EqualTo("HiRes"));
        });
    }

    /// <summary>
    /// Tests that multiple window managers can coexist with isolated state.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WindowManagers_HaveIsolatedState()
    {
        // Arrange
        var mockWindow1 = CreateMockPopOutWindow(PopOutComponent.VideoDisplay);
        var mockWindow2 = CreateMockPopOutWindow(PopOutComponent.DebugConsole);

        var path1 = Path.Combine(testLayoutPath, "profile1");
        var path2 = Path.Combine(testLayoutPath, "profile2");

        var windowManager1 = new WindowManager(
            (_, _) => mockWindow1.Object,
            layoutStoragePath: path1);

        var windowManager2 = new WindowManager(
            (_, _) => mockWindow2.Object,
            layoutStoragePath: path2);

        // Act
        await windowManager1.CreatePopOutAsync(PopOutComponent.VideoDisplay);
        await windowManager2.CreatePopOutAsync(PopOutComponent.DebugConsole);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(windowManager1.PopOutWindows, Has.Count.EqualTo(1));
            Assert.That(windowManager2.PopOutWindows, Has.Count.EqualTo(1));
            Assert.That(windowManager1.FindWindow(PopOutComponent.VideoDisplay), Is.Not.Null);
            Assert.That(windowManager2.FindWindow(PopOutComponent.DebugConsole), Is.Not.Null);
            Assert.That(windowManager1.FindWindow(PopOutComponent.DebugConsole), Is.Null);
            Assert.That(windowManager2.FindWindow(PopOutComponent.VideoDisplay), Is.Null);
        });
    }

    /// <summary>
    /// Tests complete workflow: create window, save state, close window, restore state.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task CompleteWorkflow_CreateSaveCloseRestore()
    {
        // Arrange
        var windowsCreated = 0;
        var mockWindow = CreateMockPopOutWindow(PopOutComponent.VideoDisplay);
        var windowManager = new WindowManager(
            (_, _) =>
            {
                windowsCreated++;
                return mockWindow.Object;
            },
            layoutStoragePath: testLayoutPath);

        // Step 1: Create window
        await windowManager.CreatePopOutAsync(PopOutComponent.VideoDisplay);
        Assert.That(windowManager.PopOutWindows, Has.Count.EqualTo(1));

        // Step 2: Save state
        await windowManager.SaveWindowStatesAsync("workflow-test");
        var layoutFile = Path.Combine(testLayoutPath, "layout-workflow-test.json");
        Assert.That(File.Exists(layoutFile), Is.True);

        // Step 3: Close all windows
        await windowManager.CloseAllWindowsAsync();
        Assert.That(windowManager.PopOutWindows, Is.Empty);

        // Step 4: Restore state
        await windowManager.RestoreWindowStatesAsync("workflow-test");

        // Assert
        Assert.That(windowsCreated, Is.EqualTo(2)); // One from create, one from restore
        Assert.That(windowManager.PopOutWindows, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Tests that ViewModel DockRequested event triggers window close.
    /// </summary>
    [Test]
    public void PopOutWindowViewModel_DockRequested_FiresCorrectly()
    {
        // Arrange
        var viewModel = new PopOutWindowViewModel(PopOutComponent.VideoDisplay);
        bool eventFired = false;
        viewModel.DockRequested += (_, _) => eventFired = true;

        // Act
        viewModel.DockToMainCommand.Execute(null);

        // Assert
        Assert.That(eventFired, Is.True);
    }

    private static Mock<IPopOutWindow> CreateMockPopOutWindow(PopOutComponent component, string? machineId = null)
    {
        var mockWindow = new Mock<IPopOutWindow>();
        mockWindow.Setup(w => w.WindowId).Returns(Guid.NewGuid().ToString());
        mockWindow.Setup(w => w.ComponentType).Returns(component);
        mockWindow.Setup(w => w.MachineId).Returns(machineId);
        mockWindow.Setup(w => w.State).Returns(WindowState.Normal);
        mockWindow.Setup(w => w.CloseAsync(It.IsAny<bool>())).Returns(Task.CompletedTask);
        mockWindow.Setup(w => w.GetStateInfo()).Returns(new WindowStateInfo
        {
            ComponentType = component,
            IsPopOut = true,
            MachineProfileId = machineId,
        });
        return mockWindow;
    }
}