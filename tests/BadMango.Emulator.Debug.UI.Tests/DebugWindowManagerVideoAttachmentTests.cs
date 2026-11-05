// <copyright file="DebugWindowManagerVideoAttachmentTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

using System.Reflection;

using Avalonia.Headless.NUnit;
using Avalonia.Threading;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Debug.Infrastructure.Commands;
using BadMango.Emulator.Debug.UI.Services;
using BadMango.Emulator.Debug.UI.Views;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;
using BadMango.Emulator.Rendering;

using Moq;

using EmulatorKeyboardDevice = BadMango.Emulator.Devices.Interfaces.IKeyboardDevice;

/// <summary>
/// Tests video window machine attachment through DebugWindowManager context application paths.
/// </summary>
[TestFixture]
public class DebugWindowManagerVideoAttachmentTests
{
    private static readonly DebugWindowManager WindowManager = new();

    private static MethodInfo CreateVideoWindowMethod =>
        typeof(DebugWindowManager).GetMethod(
            "CreateVideoWindow",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static MethodInfo ApplyContextToWindowMethod =>
        typeof(DebugWindowManager).GetMethod(
            "ApplyContextToWindow",
            BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Verifies CreateVideoWindow attaches the machine provided by VideoWindowContext.
    /// </summary>
    [AvaloniaTest]
    public void CreateVideoWindow_WithMachineInContext_SubscribesVBlank()
    {
        var mockMachine = CreateMockMachineWithPhysicalRam(out _, out var mockVideoDevice);
        var context = new VideoWindowContext { Machine = mockMachine.Object };
        var window = (VideoWindow)CreateVideoWindowMethod.Invoke(WindowManager, [context])!;

        mockVideoDevice.Raise(v => v.VBlankOccurred += null);
        Dispatcher.UIThread.RunJobs();

        Assert.That(GetFrameCount(window), Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies CreateVideoWindow attaches the machine when context is a direct IMachine
    /// (the same context form used by BootCommand auto-open).
    /// </summary>
    [AvaloniaTest]
    public void CreateVideoWindow_WithDirectMachineContext_SubscribesVBlank()
    {
        var mockMachine = CreateMockMachineWithPhysicalRam(out _, out var mockVideoDevice);
        var window = (VideoWindow)CreateVideoWindowMethod.Invoke(WindowManager, [mockMachine.Object])!;

        mockVideoDevice.Raise(v => v.VBlankOccurred += null);
        Dispatcher.UIThread.RunJobs();

        Assert.That(GetFrameCount(window), Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies reattaching to a new machine unsubscribes from the old device and avoids duplicate subscriptions.
    /// </summary>
    [AvaloniaTest]
    public void ApplyContextToWindow_ReattachMachine_UnsubscribesOldAndAvoidsDuplicates()
    {
        var machineA = CreateMockMachineWithPhysicalRam(out _, out var videoA);
        var machineB = CreateMockMachineWithPhysicalRam(out _, out var videoB);
        var window = (VideoWindow)CreateVideoWindowMethod.Invoke(
            WindowManager,
            [machineA.Object])!;

        videoA.Raise(v => v.VBlankOccurred += null);
        Dispatcher.UIThread.RunJobs();
        Assert.That(GetFrameCount(window), Is.EqualTo(1));

        ApplyContextToWindowMethod.Invoke(
            null,
            [window, machineB.Object]);

        videoA.Raise(v => v.VBlankOccurred += null);
        Dispatcher.UIThread.RunJobs();
        Assert.That(GetFrameCount(window), Is.EqualTo(1), "Old machine should no longer drive frames.");

        videoB.Raise(v => v.VBlankOccurred += null);
        Dispatcher.UIThread.RunJobs();
        Assert.That(GetFrameCount(window), Is.EqualTo(2), "New machine should drive exactly one frame.");

        ApplyContextToWindowMethod.Invoke(
            null,
            [window, machineB.Object]);
        videoB.Raise(v => v.VBlankOccurred += null);
        Dispatcher.UIThread.RunJobs();
        Assert.That(GetFrameCount(window), Is.EqualTo(3), "Reattaching same machine must not duplicate handlers.");
    }

    /// <summary>
    /// Verifies ForceRefresh after reattachment renders the new machine's memory.
    /// </summary>
    [AvaloniaTest]
    public void ApplyContextToWindow_ForceRefreshAfterReattach_RendersNewMachineMemory()
    {
        var machineA = CreateMockMachineWithPhysicalRam(out var mainA, out var videoA);
        var machineB = CreateMockMachineWithPhysicalRam(out var mainB, out _);
        var window = (VideoWindow)CreateVideoWindowMethod.Invoke(
            WindowManager,
            [new VideoWindowContext { Machine = machineA.Object }])!;

        mainA.AsSpan().Slice(0x2000, 0x2000).Fill(0x11);
        mainB.AsSpan().Slice(0x2000, 0x2000).Fill(0xEE);

        videoA.Raise(v => v.VBlankOccurred += null);
        Dispatcher.UIThread.RunJobs();

        ApplyContextToWindowMethod.Invoke(
            null,
            [window, new VideoWindowContext { Machine = machineB.Object, ForceRefresh = true }]);
        Dispatcher.UIThread.RunJobs();

        var expected = RenderReference(VideoMode.HiRes, mainB);
        var actual = GetWindowPixels(window);
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Is.Not.EqualTo(RenderReference(VideoMode.HiRes, mainA)));
        });
    }

    private static Mock<IMachine> CreateMockMachineWithPhysicalRam(
        out PhysicalMemory mainMemory,
        out Mock<IVideoDevice> mockVideoDevice)
    {
        mainMemory = new PhysicalMemory(65536, "TestMainRAM");

        var mainMemoryProvider = new Mock<IMainMemoryProvider>();
        mainMemoryProvider.Setup(provider => provider.MainRam).Returns(mainMemory.Memory);

        var mockBus = new Mock<IMemoryBus>();
        mockBus.Setup(bus => bus.Read8(It.IsAny<BusAccess>())).Returns(0xFF);

        mockVideoDevice = new Mock<IVideoDevice>();
        mockVideoDevice.Setup(video => video.CurrentMode).Returns(VideoMode.HiRes);
        mockVideoDevice.Setup(video => video.IsPage2).Returns(false);

        var machine = new Mock<IMachine>();
        machine.Setup(m => m.Bus).Returns(mockBus.Object);
        machine.Setup(m => m.GetComponent<IVideoDevice>()).Returns(mockVideoDevice.Object);
        machine.Setup(m => m.GetComponent<IMainMemoryProvider>()).Returns(mainMemoryProvider.Object);
        machine.Setup(m => m.GetComponent<IExtended80ColumnDevice>()).Returns((IExtended80ColumnDevice?)null);
        machine.Setup(m => m.GetComponent<ICharacterDevice>()).Returns((ICharacterDevice?)null);
        machine.Setup(m => m.GetComponent<ICharacterRomProvider>()).Returns((ICharacterRomProvider?)null);
        machine.Setup(m => m.GetComponent<EmulatorKeyboardDevice>()).Returns((EmulatorKeyboardDevice?)null);

        return machine;
    }

    private static int GetFrameCount(VideoWindow window)
    {
        var field = typeof(VideoWindow).GetField("frameCount", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (int)field.GetValue(window)!;
    }

    private static uint[] GetWindowPixels(VideoWindow window)
    {
        var field = typeof(VideoWindow).GetField("pixelBuffer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var buffer = (PixelBuffer)field.GetValue(window)!;
        return buffer.GetPixels().ToArray();
    }

    private static uint[] RenderReference(VideoMode mode, PhysicalMemory memory)
    {
        var renderer = new Pocket2VideoRenderer();
        var pixels = new uint[renderer.CanonicalWidth * renderer.CanonicalHeight];
        renderer.RenderFrame(
            pixels,
            mode,
            address => memory.Memory.Span[address],
            DefaultCharacterRom.GetRomData(),
            false,
            false,
            false,
            false,
            true,
            DisplayColorMode.Color,
            null);

        return pixels;
    }
}