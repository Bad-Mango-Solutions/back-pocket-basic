// <copyright file="DebugConsoleModule.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug;

using Autofac;

using Commands;

/// <summary>
/// Autofac module for registering debug console services.
/// </summary>
/// <remarks>
/// This module registers the command dispatcher, command handlers, and REPL
/// components for the debug console. New commands can be added by registering
/// additional <see cref="ICommandHandler"/> implementations.
/// </remarks>
public class DebugConsoleModule : Module
{
    /// <inheritdoc/>
    protected override void Load(ContainerBuilder builder)
    {
        // Register the command dispatcher as singleton
        builder.RegisterType<CommandDispatcher>()
            .As<ICommandDispatcher>()
            .SingleInstance();

        // Register built-in command handlers
        builder.RegisterType<HelpCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<ExitCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<VersionCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<ClearCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        // Register the command context factory
        builder.Register(ctx =>
        {
            var dispatcher = ctx.Resolve<ICommandDispatcher>();
            return CommandContext.CreateConsoleContext(dispatcher);
        })
        .As<ICommandContext>()
        .SingleInstance();

        // Register the REPL
        builder.Register(ctx =>
        {
            var dispatcher = ctx.Resolve<ICommandDispatcher>();
            var context = ctx.Resolve<ICommandContext>();

            // Register all command handlers with the dispatcher
            var handlers = ctx.Resolve<IEnumerable<ICommandHandler>>();
            foreach (var handler in handlers)
            {
                dispatcher.Register(handler);
            }

            return new DebugRepl(dispatcher, context, Console.In);
        })
        .AsSelf()
        .SingleInstance();
    }
}