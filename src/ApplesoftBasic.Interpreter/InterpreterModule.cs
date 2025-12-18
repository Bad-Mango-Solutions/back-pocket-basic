// <copyright file="InterpreterModule.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter;

using System.Diagnostics.CodeAnalysis;

using Autofac;
using Emulation;
using Execution;
using IO;
using Lexer;
using Microsoft.Extensions.Logging;
using Parser;
using Runtime;

/// <summary>
/// Autofac module for registering interpreter services.
/// </summary>
[ExcludeFromCodeCoverage]
public class InterpreterModule : Module
{
    /// <summary>
    /// Configures and registers the components required for the Applesoft BASIC interpreter.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ContainerBuilder"/> used to register components and manage their lifetimes.
    /// </param>
    /// <remarks>
    /// This method registers various services and components such as the lexer, parser, runtime managers,
    /// I/O, emulation components, and the interpreter itself. Additionally, it sets up a callback to
    /// wire the speaker to the I/O for CHR$(7) beep support.
    /// </remarks>
    protected override void Load(ContainerBuilder builder)
    {
        // Lexer and Parser
        builder.RegisterType<BasicLexer>().As<ILexer>().InstancePerLifetimeScope();
        builder.RegisterType<BasicParser>().As<IParser>().InstancePerLifetimeScope();

        // Runtime managers
        builder.RegisterType<VariableManager>().As<IVariableManager>().InstancePerLifetimeScope();
        builder.RegisterType<FunctionManager>().As<IFunctionManager>().InstancePerLifetimeScope();
        builder.RegisterType<DataManager>().As<IDataManager>().InstancePerLifetimeScope();
        builder.RegisterType<ForLoopManager>().As<ILoopManager>().InstancePerLifetimeScope();
        builder.RegisterType<GosubManager>().As<IGosubManager>().InstancePerLifetimeScope();

        // I/O
        builder.RegisterType<ConsoleBasicIO>().As<IBasicIO>().InstancePerLifetimeScope();

        // Emulation
        builder.RegisterType<AppleMemory>().As<IMemory>().InstancePerLifetimeScope();
        builder.RegisterType<Cpu6502>().As<ICpu>().InstancePerLifetimeScope();
        builder.RegisterType<AppleSpeaker>().As<IAppleSpeaker>().InstancePerLifetimeScope();
        builder.RegisterType<AppleSystem>().As<IAppleSystem>().InstancePerLifetimeScope();

        // BASIC Runtime Context (aggregates language runtime state)
        builder.RegisterType<BasicRuntimeContext>()
            .As<IBasicRuntimeContext>()
            .InstancePerLifetimeScope();

        // System Context (aggregates hardware/system services)
        builder.Register(ctx => new SystemContext(
                ctx.Resolve<IAppleSystem>(),
                ctx.Resolve<IBasicIO>(),
                ctx.Resolve<ILogger<BasicInterpreter>>()))
            .As<ISystemContext>()
            .InstancePerLifetimeScope();

        // Interpreter - with callback to wire up speaker to IO
        builder.RegisterType<BasicInterpreter>()
            .As<IBasicInterpreter>()
            .InstancePerLifetimeScope()
            .OnActivated(e =>
            {
                // Wire the speaker to the IO for CHR$(7) beep support
                var io = e.Context.Resolve<IBasicIO>();
                var speaker = e.Context.Resolve<IAppleSpeaker>();
                io.SetSpeaker(speaker);
            });
    }
}