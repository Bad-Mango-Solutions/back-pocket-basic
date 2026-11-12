// <copyright file="Program.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using BadMango.Emulator.Debug;
using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.UI;
using BadMango.Emulator.Debug.UI.Services;
using BadMango.Emulator.Devices;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

// Configure Serilog
// For verbose device-level logging, add to the chain below:
// .MinimumLevel.Override("BadMango.Emulator.Devices", LogEventLevel.Verbose)
var debugLogEventStream = new DebugLogEventStream();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Warning)
    .WriteTo.File(
        "logs/emudbg-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Sink(
        new DebugLogWindowSink(debugLogEventStream),
        restrictedToMinimumLevel: LogEventLevel.Warning)
    .CreateLogger();

// Give devices built by the device factory registry (speaker, Disk II, ...) a logger scoped
// to their type, so device diagnostics such as audio-backend failures reach the log.
DeviceFactoryRegistry.LoggerFactory = deviceType => Log.Logger.ForContext(deviceType);

static void PrintUsage()
{
    Console.WriteLine("""
        emudbg - Emulator Debug Console for BackPocketBASIC

        Usage:
          emudbg [options]

        Options:
          -p, --profile <name>   Load the specified machine profile on startup
                                     (e.g. pocket2e-a2-enh, pocket2e-lite, simple-65c02)
          -e, --exec <commands>  Execute the given commands then exit (non-interactive).
                                     Separate multiple commands with ; or \n
          -f, --file <path>      Read and execute commands from a text file (one command per line)
          --no-banner            Do not display the startup banner
          --headless             Use Avalonia headless platform (for AI video diagnostics, servers, CI without display)
          --agent                Agent mode: auto --headless --json + clean non-interactive
          -h, --help             Show this help and exit

        Examples:
          emudbg --profile pocket2e-lite
          emudbg --exec "boot;regs;step 20;regs;exit"
          emudbg -e "profile list\nboot\nregs" --no-banner
          emudbg --headless --json --exec "switches;regions;exit"
          emudbg --agent --exec "video screen;exit"
          emudbg --file my-debug-script.txt

        When no options are given, starts an interactive REPL.
        See AGENTS.md for guidance on using emudbg from AI agents and tools.
        """);
}

try
{
    var options = EmudbgOptions.Parse(args);

    if (options.ShowHelp)
    {
        PrintUsage();
        return 0;
    }

    // Validate script file early (before container) so we can exit with message
    if (!string.IsNullOrWhiteSpace(options.ScriptFile) && !File.Exists(options.ScriptFile))
    {
        Console.WriteLine($"Error: Script file not found: {options.ScriptFile}");
        return 1;
    }

    // Auto-enable headless + JSON for non-interactive input or agent mode (for low-noise AI use)
    bool forceNonInteractive = Console.IsInputRedirected ||
                               !string.IsNullOrEmpty(options.ExecCommands) ||
                               !string.IsNullOrEmpty(options.ScriptFile) ||
                               options.AgentMode;

    if (forceNonInteractive)
    {
        options = options with { Headless = true, JsonOutput = true, AgentMode = true };
    }

    // Configure Avalonia headless mode if requested (for AI video diagnostics, servers, etc.)
    AvaloniaBootstrapper.UseHeadless = options.Headless;

    Log.Information("Starting Emulator Debug Console");

    // Build the host
    var host = Host.CreateDefaultBuilder(args)
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .ConfigureContainer<ContainerBuilder>(builder =>
        {
            // Register startup options so modules and components can read CLI overrides (profile, etc.)
            builder.RegisterInstance(options).AsSelf().SingleInstance();

            builder.RegisterModule<DebugConsoleModule>();
            builder.RegisterModule<DebugUiModule>();  // Always register; headless mode is configured via AvaloniaBootstrapper.UseHeadless
            builder.RegisterInstance(debugLogEventStream).As<IDebugLogEventStream>().SingleInstance();
            builder.RegisterInstance(Log.Logger).As<ILogger>().SingleInstance();
        })
        .UseSerilog(Log.Logger)
        .Build();

    // Run the REPL (or script)
    using var scope = host.Services.CreateScope();
    var repl = scope.ServiceProvider.GetRequiredService<DebugRepl>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger>();

    // Configure non-interactive / scripting behavior
    bool isScriptMode = !string.IsNullOrEmpty(options.ExecCommands) || !string.IsNullOrEmpty(options.ScriptFile) || options.AgentMode;
    if (options.NoBanner || isScriptMode || options.JsonOutput)
    {
        repl.ShowBanner = false;
        repl.ShowPrompt = false;
        repl.PrintExitMessage = false;
    }

    logger.Information("Debug console initialized");

    if (isScriptMode)
    {
        // Lightweight "run commands and exit" path - bypasses the full interactive REPL loop
        // (no prompt reads, direct dispatch via ProcessLine in batch).
        // This is lower overhead and cleaner for scripting/AI agents.
        List<string> commandLines;
        if (!string.IsNullOrEmpty(options.ExecCommands))
        {
            commandLines = options.ExecCommands
                .Split(new[] { '\n', ';', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }
        else
        {
            commandLines = File.ReadAllLines(options.ScriptFile!)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s) && !s.StartsWith("#"))
                .ToList();
        }

        repl.ExecuteBatch(commandLines);
    }
    else
    {
        repl.Run();
    }

    logger.Information("Debug console exited normally");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Debug console terminated unexpectedly");
    Console.WriteLine($"Fatal error: {ex.Message}");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}