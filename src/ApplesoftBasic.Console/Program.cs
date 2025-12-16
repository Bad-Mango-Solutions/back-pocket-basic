// <copyright file="Program.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Console;

using Interpreter;
using Interpreter.Execution;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

/// <summary>
/// Applesoft BASIC Interpreter Console Application.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Warning)
            .WriteTo.File(
                "logs/applesoft-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Applesoft BASIC Interpreter");

            if (args.Length == 0)
            {
                System.Console.WriteLine("Applesoft BASIC Interpreter");
                System.Console.WriteLine("Usage: ApplesoftBasic.Console <source-file.bas>");
                System.Console.WriteLine();
                System.Console.WriteLine("Options:");
                System.Console.WriteLine("  <source-file.bas>  Path to a BASIC source file to execute");
                return 1;
            }

            string sourceFile = args[0];

            if (!File.Exists(sourceFile))
            {
                System.Console.WriteLine($"Error: File not found: {sourceFile}");
                return 1;
            }

            // Build the host
            var host = Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    builder.RegisterModule<InterpreterModule>();
                })
                .UseSerilog()
                .Build();

            // Run the interpreter
            using var scope = host.Services.CreateScope();
            var interpreter = scope.ServiceProvider.GetRequiredService<IBasicInterpreter>();

            string source = await File.ReadAllTextAsync(sourceFile);

            Log.Information("Executing {SourceFile}", sourceFile);

            interpreter.Run(source);

            Log.Information("Execution complete");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            System.Console.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
