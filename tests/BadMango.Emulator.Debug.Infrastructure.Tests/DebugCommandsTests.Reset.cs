// <copyright file="DebugCommandsTests.Reset.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;

using Moq;

/// <content>
/// Unit tests for <see cref="ResetCommand"/>.
/// </content>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that ResetCommand has correct name.
    /// </summary>
    [Test]
    public void ResetCommand_HasCorrectName()
    {
        var command = new ResetCommand();
        Assert.That(command.Name, Is.EqualTo("reset"));
    }

    /// <summary>
    /// Verifies that ResetCommand performs soft reset by default.
    /// </summary>
    [Test]
    public void ResetCommand_PerformsSoftReset_ByDefault()
    {
        // Change PC to different value
        cpu.SetPC(0x5000);

        var command = new ResetCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(cpu.GetPC(), Is.EqualTo(0x1000u)); // Reset vector
            Assert.That(outputWriter.ToString(), Does.Contain("Soft reset"));
        });
    }

    /// <summary>
    /// Verifies that ResetCommand performs hard reset when flag specified.
    /// </summary>
    [Test]
    public void ResetCommand_PerformsHardReset_WhenFlagSpecified()
    {
        // Write some data to memory
        WriteByte(bus, 0x0200, 0xFF);

        // Need to re-set reset vector after clear
        WriteWord(bus, 0xFFFC, 0x1000);

        var command = new ResetCommand();
        var result = command.Execute(debugContext, ["--hard"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Hard reset"));
        });
    }

    /// <summary>
    /// Verifies that reset delegates to machine reset when a machine is attached.
    /// </summary>
    [Test]
    public void ResetCommand_UsesMachineReset_WhenMachineAttached()
    {
        var mockCpu = new Mock<ICpu>();
        mockCpu.Setup(c => c.GetPC()).Returns(0x1234);

        var mockMachine = new Mock<IMachine>();
        mockMachine.SetupGet(m => m.Cpu).Returns(mockCpu.Object);
        mockMachine.SetupGet(m => m.Bus).Returns(bus);

        debugContext.AttachMachine(mockMachine.Object);

        var command = new ResetCommand();
        var result = command.Execute(debugContext, ["--hard"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            mockMachine.Verify(m => m.Reset(true), Times.Once);
            mockCpu.Verify(c => c.Reset(), Times.Never);
            Assert.That(outputWriter.ToString(), Does.Contain("Hard reset completed."));
        });
    }

    /// <summary>
    /// Verifies that reset can hold Open Apple / Closed Apple through the reset pulse.
    /// </summary>
    [Test]
    public void ResetCommand_HoldsAppleKeysThroughReset_WhenRequested()
    {
        var mockKeyboard = new Mock<IKeyboardDevice>();
        mockKeyboard.SetupGet(k => k.Modifiers).Returns(KeyboardModifiers.None);

        var mockMachine = new Mock<IMachine>();
        mockMachine.SetupGet(m => m.Cpu).Returns(cpu);
        mockMachine.SetupGet(m => m.Bus).Returns(bus);
        mockMachine.Setup(m => m.GetComponent<IKeyboardDevice>()).Returns(mockKeyboard.Object);
        mockMachine.Setup(m => m.PulseReset(It.IsAny<Action>(), It.IsAny<Action>(), It.IsAny<bool>()))
            .Callback<Action, Action, bool>((assertLine, releaseLine, hard) =>
            {
                assertLine();
                releaseLine();
            });

        debugContext.AttachMachine(mockMachine.Object);

        var command = new ResetCommand();
        var result = command.Execute(debugContext, ["--open-apple", "--diag"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            mockKeyboard.Verify(k => k.SetModifiers(KeyboardModifiers.Control | KeyboardModifiers.OpenApple | KeyboardModifiers.ClosedApple), Times.Once);
            mockKeyboard.Verify(k => k.PressResetKey(), Times.Once);
            mockKeyboard.Verify(k => k.ReleaseResetKey(), Times.Once);
        });
    }
}