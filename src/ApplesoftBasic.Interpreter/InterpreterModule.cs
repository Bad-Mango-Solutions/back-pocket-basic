using ApplesoftBasic.Interpreter.Emulation;
using ApplesoftBasic.Interpreter.Execution;
using ApplesoftBasic.Interpreter.IO;
using ApplesoftBasic.Interpreter.Lexer;
using ApplesoftBasic.Interpreter.Parser;
using ApplesoftBasic.Interpreter.Runtime;
using Autofac;

namespace ApplesoftBasic.Interpreter;

/// <summary>
/// Autofac module for registering interpreter services
/// </summary>
public class InterpreterModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Lexer and Parser
        builder.RegisterType<BasicLexer>().As<ILexer>().InstancePerLifetimeScope();
        builder.RegisterType<BasicParser>().As<IParser>().InstancePerLifetimeScope();
        
        // Runtime managers
        builder.RegisterType<VariableManager>().As<IVariableManager>().InstancePerLifetimeScope();
        builder.RegisterType<FunctionManager>().As<IFunctionManager>().InstancePerLifetimeScope();
        builder.RegisterType<DataManager>().As<IDataManager>().InstancePerLifetimeScope();
        builder.RegisterType<LoopManager>().As<ILoopManager>().InstancePerLifetimeScope();
        builder.RegisterType<GosubManager>().As<IGosubManager>().InstancePerLifetimeScope();
        
        // I/O
        builder.RegisterType<ConsoleBasicIO>().As<IBasicIO>().InstancePerLifetimeScope();
        
        // Emulation
        builder.RegisterType<AppleMemory>().As<IMemory>().InstancePerLifetimeScope();
        builder.RegisterType<Cpu6502>().As<ICpu>().InstancePerLifetimeScope();
        builder.RegisterType<AppleSpeaker>().As<IAppleSpeaker>().InstancePerLifetimeScope();
        builder.RegisterType<AppleSystem>().As<IAppleSystem>().InstancePerLifetimeScope();
        
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
