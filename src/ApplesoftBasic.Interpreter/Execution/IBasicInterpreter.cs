// <copyright file="IBasicInterpreter.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Execution;

using Emulation;

/// <summary>
/// Represents the interface for executing Applesoft BASIC programs.
/// </summary>
/// <remarks>
/// This interface provides methods to run and stop the execution of BASIC programs,
/// as well as access to the underlying Apple system emulation.
/// </remarks>
public interface IBasicInterpreter
{
    /// <summary>
    /// Gets the emulated Apple II system associated with the interpreter.
    /// </summary>
    /// <remarks>
    /// This property provides access to the underlying Apple II system emulation,
    /// allowing interaction with components such as the CPU, memory, and speaker.
    /// It also supports operations like memory manipulation (PEEK/POKE),
    /// machine code execution (CALL), and system reset.
    /// </remarks>
    IAppleSystem AppleSystem { get; }

    /// <summary>
    /// Executes the specified Applesoft BASIC program.
    /// </summary>
    /// <param name="source">The source code of the Applesoft BASIC program to execute.</param>
    /// <remarks>
    /// This method parses the provided source code, initializes the runtime environment,
    /// and begins execution of the program. If any runtime or parsing errors occur,
    /// they are handled and logged appropriately.
    /// </remarks>
    void Run(string source);

    /// <summary>
    /// Stops the execution of the currently running Applesoft BASIC program.
    /// </summary>
    /// <remarks>
    /// This method signals the interpreter to halt execution. It is typically used to
    /// terminate a running program gracefully. The interpreter will stop processing
    /// further instructions after the current operation completes.
    /// </remarks>
    void Stop();
}