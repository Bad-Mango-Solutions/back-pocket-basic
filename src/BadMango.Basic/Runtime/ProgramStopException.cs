// <copyright file="ProgramStopException.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

/// <summary>
/// Represents an exception that is thrown to signal the execution of a STOP command
/// in an Applesoft BASIC program.
/// </summary>
/// <remarks>
/// This exception is typically used to terminate the execution of a program
/// when a STOP statement is encountered. The exception includes the line number
/// where the STOP command was executed.
/// </remarks>
public class ProgramStopException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgramStopException"/> class with the specified line number.
    /// </summary>
    /// <param name="lineNumber">
    /// The line number in the Applesoft BASIC program where the STOP command was executed.
    /// </param>
    public ProgramStopException(int lineNumber)
        : base($"BREAK IN {lineNumber}")
    {
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the line number in the Applesoft BASIC program where the STOP command was executed.
    /// </summary>
    /// <value>
    /// The line number associated with the STOP command.
    /// </value>
    public int LineNumber { get; }
}