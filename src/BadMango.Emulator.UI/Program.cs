// <copyright file="Program.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI;

using System;
using System.IO;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using Avalonia;

using BadMango.Emulator.UI.Services;
using BadMango.Emulator.UI.ViewModels;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

/// <summary>
/// Application entry point for the BackPocket emulator UI.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        // Build the host first to configure services
        var host = CreateHostBuilder(args).Build();

        // Store host in App for service resolution
        App.AppHost = host;

        // Start Avalonia
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Builds the Avalonia application configuration.
    /// </summary>
    /// <returns>An <see cref="AppBuilder"/> configured for the application.</returns>
    /// <remarks>
    /// This method is used by visual designers to initialize the application.
    /// </remarks>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Creates and configures the host builder with Autofac, Serilog, and configuration.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A configured <see cref="IHostBuilder"/>.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables("BACKPOCKET_");
                config.AddCommandLine(args);
            })
            .UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File(
                        Path.Combine(AppContext.BaseDirectory, "logs", "backpocket-ui-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7);
            })
            .ConfigureContainer<ContainerBuilder>((context, builder) =>
            {
                // Register services
                builder.RegisterType<ThemeService>().As<IThemeService>().SingleInstance();
                builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();

                // Register ViewModels
                builder.RegisterType<MainWindowViewModel>().AsSelf();
                builder.RegisterType<MachineManagerViewModel>().AsSelf();
            });
}