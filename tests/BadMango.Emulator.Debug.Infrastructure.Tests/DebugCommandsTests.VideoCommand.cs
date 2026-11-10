// <copyright file="DebugCommandsTests.VideoCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.Infrastructure.Commands;
using BadMango.Emulator.Rendering;

using Moq;

/// <content>
/// Unit tests for <see cref="VideoCommand"/> machine-context forwarding.
/// </content>
public partial class DebugCommandsTests
{
    /// <content>
    /// Verifies video open passes the current machine in context.
    /// </summary>
    [Test]
    public void VideoCommand_Open_PassesMachineInContext()
    {
        var machine = CreateAttachedMachine();
        var windowManager = CreateWindowManagerMock();
        var command = new VideoCommand(windowManager.Object);

        var result = command.Execute(debugContext, ["open"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            windowManager.Verify(
                manager => manager.ShowWindowAsync(
                    "Video",
                    It.Is<VideoWindowContext>(ctx => ctx.Machine == machine.Object)),
                Times.Once);
        });
    }

    /// <summary>
    /// Verifies video scale forwards machine and scale settings in context.
    /// </summary>
    [Test]
    public void VideoCommand_Scale_PassesMachineAndScaleInContext()
    {
        var machine = CreateAttachedMachine();
        var windowManager = CreateWindowManagerMock();
        var command = new VideoCommand(windowManager.Object);

        var result = command.Execute(debugContext, ["scale", "3"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            windowManager.Verify(
                manager => manager.ShowWindowAsync(
                    "Video",
                    It.Is<VideoWindowContext>(ctx => ctx.Machine == machine.Object && ctx.Scale == 3)),
                Times.Once);
        });
    }

    /// <summary>
    /// Verifies video color forwards machine and color mode settings in context.
    /// </summary>
    [Test]
    public void VideoCommand_Color_PassesMachineAndColorInContext()
    {
        var machine = CreateAttachedMachine();
        var windowManager = CreateWindowManagerMock();
        var command = new VideoCommand(windowManager.Object);

        var result = command.Execute(debugContext, ["color", "amber"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            windowManager.Verify(
                manager => manager.ShowWindowAsync(
                    "Video",
                    It.Is<VideoWindowContext>(ctx => ctx.Machine == machine.Object && ctx.ColorMode == DisplayColorMode.Amber)),
                Times.Once);
        });
    }

    /// <summary>
    /// Verifies video fps forwards machine and toggle settings in context.
    /// </summary>
    [Test]
    public void VideoCommand_Fps_PassesMachineAndFpsToggleInContext()
    {
        var machine = CreateAttachedMachine();
        var windowManager = CreateWindowManagerMock();
        var command = new VideoCommand(windowManager.Object);

        var result = command.Execute(debugContext, ["fps", "off"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            windowManager.Verify(
                manager => manager.ShowWindowAsync(
                    "Video",
                    It.Is<VideoWindowContext>(ctx => ctx.Machine == machine.Object && ctx.ToggleFps && ctx.ShowFps == false)),
                Times.Once);
        });
    }

    /// <summary>
    /// Verifies video refresh forwards machine and force-refresh settings in context.
    /// </summary>
    [Test]
    public void VideoCommand_Refresh_PassesMachineAndRefreshInContext()
    {
        var machine = CreateAttachedMachine();
        var windowManager = CreateWindowManagerMock();
        windowManager.Setup(manager => manager.IsWindowOpen("Video")).Returns(true);
        var command = new VideoCommand(windowManager.Object);

        var result = command.Execute(debugContext, ["refresh"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            windowManager.Verify(
                manager => manager.ShowWindowAsync(
                    "Video",
                    It.Is<VideoWindowContext>(ctx => ctx.Machine == machine.Object && ctx.ForceRefresh)),
                Times.Once);
        });
    }

    private Mock<IMachine> CreateAttachedMachine()
    {
        var machine = new Mock<IMachine>();
        machine.SetupGet(item => item.Cpu).Returns(cpu);
        machine.SetupGet(item => item.Bus).Returns(bus);
        debugContext.AttachMachine(machine.Object);
        return machine;
    }

    private Mock<IDebugWindowManager> CreateWindowManagerMock()
    {
        var windowManager = new Mock<IDebugWindowManager>();
        windowManager.Setup(manager => manager.IsWindowOpen("Video")).Returns(false);
        windowManager.Setup(manager => manager.ShowWindowAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(true);
        return windowManager;
    }
}