// <copyright file="DeviceDebugCommandsModuleTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using Autofac;

using BadMango.Emulator.Debug.Infrastructure.Commands;
using BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

/// <summary>
/// Unit tests for the <see cref="DeviceDebugCommandsModule"/> class.
/// </summary>
[TestFixture]
public class DeviceDebugCommandsModuleTests
{
    /// <summary>
    /// Verifies that the module registers PwTimeCommand.
    /// </summary>
    [Test]
    public void Module_RegistersPwTimeCommand()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<DeviceDebugCommandsModule>();
        using var container = builder.Build();

        var handlers = container.Resolve<IEnumerable<ICommandHandler>>().ToList();

        Assert.That(handlers.Select(h => h.Name), Does.Contain("pwtime"));
    }

    /// <summary>
    /// Verifies that PwTimeCommand has correct aliases.
    /// </summary>
    [Test]
    public void PwTimeCommand_HasCorrectAliases()
    {
        var command = new PwTimeCommand();

        Assert.That(command.Aliases, Does.Contain("pocketwatch-time"));
        Assert.That(command.Aliases, Does.Contain("thundertime"));
    }

    /// <summary>
    /// Verifies that PwTimeCommand has correct description.
    /// </summary>
    [Test]
    public void PwTimeCommand_HasCorrectDescription()
    {
        var command = new PwTimeCommand();

        Assert.That(command.Description, Does.Contain("PocketWatch"));
    }

    /// <summary>
    /// Verifies that PwTimeCommand has help information.
    /// </summary>
    [Test]
    public void PwTimeCommand_ImplementsICommandHelp()
    {
        var command = new PwTimeCommand();

        Assert.That(command, Is.InstanceOf<ICommandHelp>());

        var help = (ICommandHelp)command;
        Assert.That(help.Synopsis, Is.Not.Null.And.Not.Empty);
        Assert.That(help.DetailedDescription, Is.Not.Null.And.Not.Empty);
        Assert.That(help.Examples, Is.Not.Empty);
        Assert.That(help.SeeAlso, Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that PwTimeCommand returns error without debug context.
    /// </summary>
    [Test]
    public void PwTimeCommand_Execute_WithoutDebugContext_ReturnsError()
    {
        var command = new PwTimeCommand();
        var mockContext = new Moq.Mock<ICommandContext>();

        var result = command.Execute(mockContext.Object, ["read", "$300"]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Debug context"));
    }

    /// <summary>
    /// Verifies that PwTimeCommand returns error without subcommand.
    /// </summary>
    [Test]
    public void PwTimeCommand_Execute_WithoutSubcommand_ReturnsError()
    {
        var command = new PwTimeCommand();
        var mockContext = new Moq.Mock<IDebugContext>();
        mockContext.Setup(c => c.IsBusAttached).Returns(true);
        mockContext.Setup(c => c.Bus).Returns(new Moq.Mock<BadMango.Emulator.Bus.Interfaces.IMemoryBus>().Object);

        var result = command.Execute(mockContext.Object, []);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("read"));
        Assert.That(result.Message, Does.Contain("slot"));
        Assert.That(result.Message, Does.Contain("copy"));
    }

    /// <summary>
    /// Verifies that PwTimeCommand returns error with unknown subcommand.
    /// </summary>
    [Test]
    public void PwTimeCommand_Execute_WithUnknownSubcommand_ReturnsError()
    {
        var command = new PwTimeCommand();
        var mockContext = new Moq.Mock<IDebugContext>();
        mockContext.Setup(c => c.IsBusAttached).Returns(true);
        mockContext.Setup(c => c.Bus).Returns(new Moq.Mock<BadMango.Emulator.Bus.Interfaces.IMemoryBus>().Object);

        var result = command.Execute(mockContext.Object, ["unknown"]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Unknown subcommand"));
    }

    /// <summary>
    /// Verifies that PwTimeCommand read subcommand returns error without address.
    /// </summary>
    [Test]
    public void PwTimeCommand_Read_WithoutAddress_ReturnsError()
    {
        var command = new PwTimeCommand();
        var mockContext = new Moq.Mock<IDebugContext>();
        mockContext.Setup(c => c.IsBusAttached).Returns(true);
        mockContext.Setup(c => c.Bus).Returns(new Moq.Mock<BadMango.Emulator.Bus.Interfaces.IMemoryBus>().Object);

        var result = command.Execute(mockContext.Object, ["read"]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Usage"));
    }

    /// <summary>
    /// Verifies that PwTimeCommand slot subcommand returns error without slot number.
    /// </summary>
    [Test]
    public void PwTimeCommand_Slot_WithoutSlotNumber_ReturnsError()
    {
        var command = new PwTimeCommand();
        var mockContext = new Moq.Mock<IDebugContext>();
        mockContext.Setup(c => c.IsBusAttached).Returns(true);
        mockContext.Setup(c => c.Bus).Returns(new Moq.Mock<BadMango.Emulator.Bus.Interfaces.IMemoryBus>().Object);

        var result = command.Execute(mockContext.Object, ["slot"]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Usage"));
    }

    /// <summary>
    /// Verifies that PwTimeCommand slot subcommand returns error with invalid slot number.
    /// </summary>
    [Test]
    public void PwTimeCommand_Slot_WithInvalidSlotNumber_ReturnsError()
    {
        var command = new PwTimeCommand();
        var mockContext = new Moq.Mock<IDebugContext>();
        mockContext.Setup(c => c.IsBusAttached).Returns(true);
        mockContext.Setup(c => c.Bus).Returns(new Moq.Mock<BadMango.Emulator.Bus.Interfaces.IMemoryBus>().Object);

        var result = command.Execute(mockContext.Object, ["slot", "8"]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Invalid slot"));
    }

    /// <summary>
    /// Verifies that PwTimeCommand copy subcommand returns error without enough arguments.
    /// </summary>
    [Test]
    public void PwTimeCommand_Copy_WithoutArguments_ReturnsError()
    {
        var command = new PwTimeCommand();
        var mockContext = new Moq.Mock<IDebugContext>();
        mockContext.Setup(c => c.IsBusAttached).Returns(true);
        mockContext.Setup(c => c.Bus).Returns(new Moq.Mock<BadMango.Emulator.Bus.Interfaces.IMemoryBus>().Object);

        var result = command.Execute(mockContext.Object, ["copy", "4"]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Usage"));
    }

    /// <summary>
    /// Verifies that PwTimeCommand usage shows subcommands.
    /// </summary>
    [Test]
    public void PwTimeCommand_Usage_ShowsSubcommands()
    {
        var command = new PwTimeCommand();

        Assert.That(command.Usage, Does.Contain("read"));
        Assert.That(command.Usage, Does.Contain("slot"));
        Assert.That(command.Usage, Does.Contain("copy"));
    }
}