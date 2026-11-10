// <copyright file="Program.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

using Autofac;
using Autofac.Extensions.DependencyInjection;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Core.Interfaces;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Debug.Agent;
using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.Infrastructure.Commands;
using BadMango.Emulator.Storage.Formats;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

var builder = Host.CreateDefaultBuilder(args)
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(containerBuilder =>
    {
        containerBuilder.RegisterInstance(new EmudbgOptions(Profile: "pocket2e-a2-enh")).AsSelf().SingleInstance();
        containerBuilder.RegisterModule<DebugConsoleModule>();
    })
    .Build();

using var scope = builder.Services.CreateScope();
var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
var baseContext = scope.ServiceProvider.GetRequiredService<IDebugContext>();

_ = scope.ServiceProvider.GetRequiredService<DebugRepl>();

Console.Error.WriteLine("Emudbg Agent MCP host starting (stdio JSON-RPC).");

await new McpServer(dispatcher, baseContext).RunAsync();