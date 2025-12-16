// <copyright file="BasicRuntimeException.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Represents an exception that is thrown during the execution of an Applesoft BASIC program.
/// </summary>
/// <remarks>
/// This exception is used to indicate runtime errors specific to Applesoft BASIC programs,
/// such as syntax errors, logical errors, or other issues encountered during program execution.
/// </remarks>
public class BasicRuntimeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BasicRuntimeException"/> class with a specified error message
    /// and an optional line number where the error occurred.
    /// </summary>
    /// <param name="message">The error message that describes the runtime error.</param>
    /// <param name="lineNumber">
    /// The line number in the Applesoft BASIC program where the error occurred, or <c>null</c> if not applicable.
    /// </param>
    /// <remarks>
    /// If a line number is provided, it will be included in the error message in the format "MESSAGE IN LINE".
    /// </remarks>
    public BasicRuntimeException(string message, int? lineNumber = null)
        : base(lineNumber.HasValue ? $"{message} IN {lineNumber}" : message)
    {
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the line number in the Applesoft BASIC program where the runtime error occurred,
    /// or <c>null</c> if the line number is not applicable or not provided.
    /// </summary>
    /// <value>
    /// The line number associated with the runtime error, or <c>null</c> if unavailable.
    /// </value>
    /// <remarks>
    /// This property is useful for identifying the specific location in the Applesoft BASIC program
    /// where the error occurred, aiding in debugging and error resolution.
    /// </remarks>
    public int? LineNumber { get; }
}