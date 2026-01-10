// <copyright file="DebugConsoleModule.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using Autofac;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Core.Interfaces;
using BadMango.Emulator.Debug.Infrastructure.Commands;

using Commands.DeviceCommands;

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
    /// <summary>
    /// The name of the file storing the default profile setting.
    /// </summary>
    private const string DefaultProfileFileName = ".default-profile";

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

        builder.RegisterType<BootCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<PauseCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<ResumeCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<HaltCommand>()
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

        // Register new bus-aware debug commands (Phase D4)
        builder.RegisterType<PeekCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<ReadCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<WriteCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<CallCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<RegionsCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<PagesCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<BusLogCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<FaultCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<DeviceMapCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<ProfileCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder.RegisterType<SwitchesCommand>()
            .As<ICommandHandler>()
            .SingleInstance();

        // Register about command (uses optional IDebugWindowManager for UI popups)
        builder.Register(ctx =>
            {
                // Try to resolve IDebugWindowManager if registered (e.g., when UI is available)
                var windowManager = ctx.ResolveOptional<IDebugWindowManager>();
                return new AboutCommand(windowManager);
            })
            .As<ICommandHandler>()
            .SingleInstance();

        builder.Register(ctx =>
            {
                // Try to resolve IDebugWindowManager if registered (e.g., when UI is available)
                var windowManager = ctx.ResolveOptional<IDebugWindowManager>();
                return new CharacterMapCommand(windowManager);
            })
            .As<ICommandHandler>()
            .SingleInstance();

        // Register device-specific debug commands (auto-discovered)
        builder.RegisterModule<DeviceDebugCommandsModule>();

        builder.RegisterType<MachineProfileLoader>()
            .As<IMachineProfileLoader>()
            .SingleInstance();

        // Register the tracing debug listener
        builder.RegisterType<TracingDebugListener>()
            .AsSelf()
            .SingleInstance();

        // Register the default machine profile, respecting user's choice from .default-profile
        builder.Register(ctx =>
        {
            var loader = ctx.Resolve<IMachineProfileLoader>();
            string profileName = GetUserDefaultProfileName();

            // Try to load the user's chosen default profile
            var profile = loader.LoadProfile(profileName);
            if (profile is not null)
            {
                return profile;
            }

            // Fall back to the built-in default profile
            return loader.DefaultProfile;
        })
        .AsSelf()
        .SingleInstance();

        // Register the debug context factory (provides access to CPU, Bus, Disassembler)
        builder.Register(ctx =>
        {
            var dispatcher = ctx.Resolve<ICommandDispatcher>();
            var profile = ctx.Resolve<MachineProfile>();
            var tracingListener = ctx.Resolve<TracingDebugListener>();
            var context = DebugContext.CreateConsoleContext(dispatcher);

            // Create a path resolver with the library root for resolving library:// paths
            string libraryRoot = GetLibraryRoot();
            var pathResolver = new ProfilePathResolver(libraryRoot);

            // Create new machine with all debug components from profile
            (IMachine machine, IDisassembler disassembler, MachineInfo info) =
                MachineFactory.CreateDebugSystem(profile, pathResolver);

            // Attach the tracing listener to the CPU
            machine.Cpu.AttachDebugger(tracingListener);

            // Attach the full machine with all debug components
            context.AttachMachine(machine, disassembler, info, tracingListener);

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

    /// <summary>
    /// Gets the user's chosen default profile name from the .default-profile file.
    /// </summary>
    /// <returns>
    /// The profile name from the file, or <see cref="MachineProfileLoader.DefaultProfileName"/>
    /// if the file doesn't exist or is empty.
    /// </returns>
    private static string GetUserDefaultProfileName()
    {
        string profilesDir = Path.Combine(AppContext.BaseDirectory, "profiles");
        string defaultFilePath = Path.Combine(profilesDir, DefaultProfileFileName);

        if (File.Exists(defaultFilePath))
        {
            try
            {
                string profileName = File.ReadAllText(defaultFilePath).Trim();
                if (!string.IsNullOrEmpty(profileName))
                {
                    return profileName;
                }
            }
            catch (IOException)
            {
                // Fall through to default
            }
            catch (UnauthorizedAccessException)
            {
                // Fall through to default
            }
        }

        return MachineProfileLoader.DefaultProfileName;
    }

    /// <summary>
    /// Gets the library root directory (user's home directory + .backpocket).
    /// </summary>
    /// <returns>The library root path.</returns>
    private static string GetLibraryRoot()
    {
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".backpocket");
    }
}