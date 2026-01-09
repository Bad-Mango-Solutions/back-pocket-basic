// <copyright file="Program.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

using Autofac;
using Autofac.Extensions.DependencyInjection;

using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.UI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;

// Configure Serilog
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
    .CreateLogger();

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
        })
        .UseSerilog()
        .Build();

    // Run the REPL
    using var scope = host.Services.CreateScope();
    var repl = scope.ServiceProvider.GetRequiredService<DebugRepl>();

    Log.Information("Debug console initialized");
    repl.Run();

    Log.Information("Debug console exited normally");
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