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

        // 5 built-in (help, exit, version, clear, about) + 11 debug commands + 11 bus-aware commands + 3 device commands = 30 total
        Assert.That(handlers, Has.Count.EqualTo(30));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("help"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("exit"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("version"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("clear"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("about"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("regs"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("step"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("run"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("stop"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("reset"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("pc"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("mem"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("poke"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("load"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("save"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("dasm"));

        // Bus-aware commands
        Assert.That(handlers.Select(h => h.Name), Does.Contain("regions"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("pages"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("buslog"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("fault"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("devicemap"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("profile"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("switches"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("read"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("write"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("peek"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("call"));

        // Device debug commands
        Assert.That(handlers.Select(h => h.Name), Does.Contain("pwtime"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("lcstatus"));
        Assert.That(handlers.Select(h => h.Name), Does.Contain("keyboard"));
    }

    /// <summary>
    /// Verifies that the module registers DebugRepl with all handlers wired up.
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

            // 5 built-in (help, exit, version, clear, about) + 11 debug commands + 11 bus-aware commands + 3 device commands = 30 total
            Assert.That(dispatcher.Commands, Has.Count.EqualTo(30));
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

        // 5 built-in + 11 debug + 11 bus-aware + 3 device + 1 custom = 31 total
        Assert.That(handlers, Has.Count.EqualTo(31));
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