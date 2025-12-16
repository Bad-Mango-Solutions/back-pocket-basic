// <copyright file="IBasicInterpreter.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Execution;

using Emulation;

/// <summary>
/// Interface for the BASIC interpreter.
/// </summary>
public interface IBasicInterpreter
{
    /// <summary>
    /// Runs a BASIC program.
    /// </summary>
    void Run(string source);

    /// <summary>
    /// Stops execution.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets the Apple system emulator.
    /// </summary>
    IAppleSystem AppleSystem { get; }
}