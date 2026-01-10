// <copyright file="DebugCommandsTests.LifecycleCommands.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Moq;

/// <summary>
/// Unit tests for lifecycle commands (Boot, Pause, Resume, Halt).
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that BootCommand has correct name.
    /// </summary>
    [Test]
    public void BootCommand_HasCorrectName()
    {
        var command = new BootCommand();
        Assert.That(command.Name, Is.EqualTo("boot"));
    }

    /// <summary>
    /// Verifies that BootCommand returns error when no machine is attached.
    /// </summary>
    [Test]
    public void BootCommand_NoMachine_ReturnsError()
    {
        var command = new BootCommand();
        var result = command.Execute(debugContext, []);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("No machine"));
    }

    /// <summary>
    /// Verifies that BootCommand succeeds when machine is attached.
    /// </summary>
    [Test]
    public void BootCommand_WithMachine_Succeeds()
    {
        var command = new BootCommand();
        var mockMachine = CreateMockMachine();

        var contextWithMachine = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler);
        contextWithMachine.AttachMachine(mockMachine.Object);

        var result = command.Execute(contextWithMachine, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Does.Contain("booted"));
            mockMachine.Verify(m => m.BootAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that PauseCommand has correct name.
    /// </summary>
    [Test]
    public void PauseCommand_HasCorrectName()
    {
        var command = new PauseCommand();
        Assert.That(command.Name, Is.EqualTo("pause"));
    }

    /// <summary>
    /// Verifies that PauseCommand returns error when no machine is attached.
    /// </summary>
    [Test]
    public void PauseCommand_NoMachine_ReturnsError()
    {
        var command = new PauseCommand();
        var result = command.Execute(debugContext, []);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("No machine"));
    }

    /// <summary>
    /// Verifies that PauseCommand succeeds when machine is attached.
    /// </summary>
    [Test]
    public void PauseCommand_WithMachine_Succeeds()
    {
        var command = new PauseCommand();
        var mockMachine = CreateMockMachine(MachineState.Running);

        var contextWithMachine = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler);
        contextWithMachine.AttachMachine(mockMachine.Object);

        var result = command.Execute(contextWithMachine, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            mockMachine.Verify(m => m.Pause(), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that ResumeCommand has correct name.
    /// </summary>
    [Test]
    public void ResumeCommand_HasCorrectName()
    {
        var command = new ResumeCommand();
        Assert.That(command.Name, Is.EqualTo("resume"));
    }

    /// <summary>
    /// Verifies that ResumeCommand returns error when no machine is attached.
    /// </summary>
    [Test]
    public void ResumeCommand_NoMachine_ReturnsError()
    {
        var command = new ResumeCommand();
        var result = command.Execute(debugContext, []);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("No machine"));
    }

    /// <summary>
    /// Verifies that ResumeCommand returns error when not paused.
    /// </summary>
    [Test]
    public void ResumeCommand_NotPaused_ReturnsError()
    {
        var command = new ResumeCommand();
        var mockMachine = CreateMockMachine(MachineState.Stopped);

        var contextWithMachine = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler);
        contextWithMachine.AttachMachine(mockMachine.Object);

        var result = command.Execute(contextWithMachine, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("not paused"));
        });
    }

    /// <summary>
    /// Verifies that ResumeCommand succeeds when paused.
    /// </summary>
    [Test]
    public void ResumeCommand_WhenPaused_Succeeds()
    {
        var command = new ResumeCommand();
        var mockMachine = CreateMockMachine(MachineState.Paused);

        var contextWithMachine = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler);
        contextWithMachine.AttachMachine(mockMachine.Object);

        var result = command.Execute(contextWithMachine, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            mockMachine.Verify(m => m.ResumeAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that HaltCommand has correct name.
    /// </summary>
    [Test]
    public void HaltCommand_HasCorrectName()
    {
        var command = new HaltCommand();
        Assert.That(command.Name, Is.EqualTo("halt"));
    }

    /// <summary>
    /// Verifies that HaltCommand returns error when no machine is attached.
    /// </summary>
    [Test]
    public void HaltCommand_NoMachine_ReturnsError()
    {
        var command = new HaltCommand();
        var result = command.Execute(debugContext, []);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("No machine"));
    }

    /// <summary>
    /// Verifies that HaltCommand succeeds and calls Machine.Halt.
    /// </summary>
    [Test]
    public void HaltCommand_WithMachine_CallsHalt()
    {
        var command = new HaltCommand();
        var mockMachine = CreateMockMachine();

        var contextWithMachine = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler);
        contextWithMachine.AttachMachine(mockMachine.Object);

        var result = command.Execute(contextWithMachine, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Does.Contain("halted"));
            mockMachine.Verify(m => m.Halt(), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that StopCommand no longer has 'halt' as an alias.
    /// </summary>
    [Test]
    public void StopCommand_DoesNotHaveHaltAlias()
    {
        var command = new StopCommand();
        Assert.That(command.Aliases, Does.Not.Contain("halt"));
    }

    /// <summary>
    /// Verifies that BootCommand has correct aliases.
    /// </summary>
    [Test]
    public void BootCommand_HasCorrectAliases()
    {
        var command = new BootCommand();
        Assert.That(command.Aliases, Does.Contain("startup"));
    }

    /// <summary>
    /// Verifies that PauseCommand has correct aliases.
    /// </summary>
    [Test]
    public void PauseCommand_HasCorrectAliases()
    {
        var command = new PauseCommand();
        Assert.That(command.Aliases, Does.Contain("suspend"));
        Assert.That(command.Aliases, Does.Contain("freeze"));
    }

    /// <summary>
    /// Verifies that ResumeCommand has correct aliases.
    /// </summary>
    [Test]
    public void ResumeCommand_HasCorrectAliases()
    {
        var command = new ResumeCommand();
        Assert.That(command.Aliases, Does.Contain("continue"));
        Assert.That(command.Aliases, Does.Contain("cont"));
    }

    private static Mock<IMachine> CreateMockMachine(MachineState initialState = MachineState.Stopped)
    {
        var mock = new Mock<IMachine>();
        var state = initialState;

        mock.Setup(m => m.State).Returns(() => state);
        mock.Setup(m => m.Pause()).Callback(() => state = MachineState.Paused);
        mock.Setup(m => m.Halt()).Callback(() => state = MachineState.Stopped);
        mock.Setup(m => m.BootAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(m => m.ResumeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return mock;
    }
}