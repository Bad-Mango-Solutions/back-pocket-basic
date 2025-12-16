// <copyright file="ProgramEndException.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Represents an exception that is thrown to indicate the termination of an Applesoft BASIC program.
/// </summary>
/// <remarks>
/// This exception is used internally by the interpreter to signal the normal end of program execution.
/// It is caught and handled to ensure proper cleanup and logging of the program's termination.
/// </remarks>
public class ProgramEndException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgramEndException"/> class
    /// with a default message indicating that the program has ended.
    /// </summary>
    public ProgramEndException()
        : base("Program ended")
    {
    }
}