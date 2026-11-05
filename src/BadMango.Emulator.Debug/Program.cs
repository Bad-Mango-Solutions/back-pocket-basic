// <copyright file="Program.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

using Autofac;
using Autofac.Extensions.DependencyInjection;

using BadMango.Emulator.Debug;
using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.UI;
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

try
{
    Log.Information("Starting Emulator Debug Console");

    // Build the host
    var host = Host.CreateDefaultBuilder(args)
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .ConfigureContainer<ContainerBuilder>(builder =>
        {
            builder.RegisterModule<DebugConsoleModule>();
            builder.RegisterModule<DebugUiModule>();
            builder.RegisterInstance(debugLogEventStream).As<IDebugLogEventStream>().SingleInstance();
            builder.RegisterInstance(Log.Logger).As<ILogger>().SingleInstance();
        })
        .UseSerilog(Log.Logger)
        .Build();

    // Run the REPL
    using var scope = host.Services.CreateScope();
    var repl = scope.ServiceProvider.GetRequiredService<DebugRepl>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger>();

    logger.Information("Debug console initialized");
    repl.Run();

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