// <copyright file="DebugConsoleModuleTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Tests;

using Autofac;

/// <summary>
/// Unit tests for the <see cref="DebugConsoleModule"/> class.
/// </summary>
[TestFixture]
public class DebugConsoleModuleTests
{
    /// <summary>
    /// Verifies that the module registers ICommandDispatcher.
    /// </summary>
    [Test]
    public void Module_RegistersCommandDispatcher()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<DebugConsoleModule>();
        using var container = builder.Build();

        var dispatcher = container.Resolve<ICommandDispatcher>();

        Assert.That(dispatcher, Is.Not.Null);
        Assert.That(dispatcher, Is.InstanceOf<CommandDispatcher>());
    }

    /// <summary>
    /// Verifies that the module registers ICommandContext.
    /// </summary>
    [Test]
    public void Module_RegistersCommandContext()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<DebugConsoleModule>();
        using var container = builder.Build();

        var context = container.Resolve<ICommandContext>();

        Assert.That(context, Is.Not.Null);
        Assert.That(context, Is.InstanceOf<DebugContext>());
    }

    /// <summary>
    /// Verifies that the module registers all built-in command handlers.
    /// </summary>
    [Test]
    public void Module_RegistersBuiltInCommandHandlers()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<DebugConsoleModule>();
        using var container = builder.Build();

        var handlers = container.Resolve<IEnumerable<ICommandHandler>>().ToList();
        var handlerNames = handlers.Select(h => h.Name).ToList();

        // Verify core built-in commands are registered
        Assert.That(handlerNames, Does.Contain("help"));
        Assert.That(handlerNames, Does.Contain("exit"));
        Assert.That(handlerNames, Does.Contain("version"));
        Assert.That(handlerNames, Does.Contain("clear"));
        Assert.That(handlerNames, Does.Contain("about"));

        // Verify debug commands are registered
        Assert.That(handlerNames, Does.Contain("regs"));
        Assert.That(handlerNames, Does.Contain("step"));
        Assert.That(handlerNames, Does.Contain("run"));
        Assert.That(handlerNames, Does.Contain("reset"));
        Assert.That(handlerNames, Does.Contain("mem"));
        Assert.That(handlerNames, Does.Contain("dasm"));

        // Verify bus-aware commands are registered
        Assert.That(handlerNames, Does.Contain("regions"));
        Assert.That(handlerNames, Does.Contain("pages"));
        Assert.That(handlerNames, Does.Contain("switches"));
    }

    /// <summary>
    /// Verifies that the module registers DebugRepl with handlers wired up.
    /// </summary>
    [Test]
    public void Module_RegistersDebugReplWithHandlers()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<DebugConsoleModule>();
        using var container = builder.Build();

        var repl = container.Resolve<DebugRepl>();
        var dispatcher = container.Resolve<ICommandDispatcher>();

        Assert.Multiple(() =>
        {
            Assert.That(repl, Is.Not.Null);
            Assert.That(dispatcher.Commands, Is.Not.Empty);
        });
    }

    /// <summary>
    /// Verifies that ICommandDispatcher is registered as singleton.
    /// </summary>
    [Test]
    public void Module_CommandDispatcherIsSingleton()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<DebugConsoleModule>();
        using var container = builder.Build();

        var dispatcher1 = container.Resolve<ICommandDispatcher>();
        var dispatcher2 = container.Resolve<ICommandDispatcher>();

        Assert.That(dispatcher1, Is.SameAs(dispatcher2));
    }

    /// <summary>
    /// Verifies that custom command handlers can be added via the module.
    /// </summary>
    [Test]
    public void Module_AllowsAdditionalCommandHandlerRegistration()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<DebugConsoleModule>();
        builder.RegisterType<TestCustomCommand>()
            .As<ICommandHandler>()
            .SingleInstance();
        using var container = builder.Build();

        var handlers = container.Resolve<IEnumerable<ICommandHandler>>().ToList();

        Assert.That(handlers.Select(h => h.Name), Does.Contain("testcmd"));
    }

    private sealed class TestCustomCommand : CommandHandlerBase
    {
        public TestCustomCommand()
            : base("testcmd", "A test custom command")
        {
        }

        public override CommandResult Execute(ICommandContext context, string[] args)
        {
            return CommandResult.Ok("Custom command executed");
        }
    }
}