// <copyright file="DebugConsoleModule.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug;

using Autofac;

using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.Infrastructure.Commands;

using Core.Interfaces.Cpu;

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

        // Register debug command handlers
        builder.RegisterType<RegsCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<StepCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<RunCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<StopCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<ResetCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<PcCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<MemCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<PokeCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<LoadCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<SaveCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<DasmCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<MachineProfileLoader>()
            .As<IMachineProfileLoader>()
            .SingleInstance();

        // Register the tracing debug listener
        builder.RegisterType<TracingDebugListener>()
            .AsSelf()
            .SingleInstance();

        // Register the default machine profile.
        builder.Register(ctx =>
        {
            var loader = ctx.Resolve<IMachineProfileLoader>();
            return loader.DefaultProfile;
        });

        // Register the debug context factory (provides access to CPU, Memory, Disassembler)
        builder.Register(ctx =>
        {
            var dispatcher = ctx.Resolve<ICommandDispatcher>();
            var profile = ctx.Resolve<MachineProfile>();
            var tracingListener = ctx.Resolve<TracingDebugListener>();
            var context = DebugContext.CreateConsoleContext(dispatcher);

            (ICpu cpu, IMemory memory, IDisassembler disassembler, MachineInfo info) = MachineFactory.CreateSystem(profile);

            // Attach the tracing listener to the CPU
            cpu.AttachDebugger(tracingListener);

            context.AttachSystem(cpu, memory, disassembler, info, tracingListener);

            return context;
        })
        .As<IDebugContext>()
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