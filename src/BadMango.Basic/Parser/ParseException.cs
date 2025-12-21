// <copyright file="ParseException.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Parser;

/// <summary>
/// Represents an exception that is thrown when a parsing error occurs in the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// This exception provides details about the parsing error, including the line and column numbers
/// where the error occurred. It is typically used to signal syntax errors or other issues encountered
/// during the parsing phase of the interpreter.
/// </remarks>
public class ParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParseException"/> class with a specified error message,
    /// line number, and column number where the parsing error occurred.
    /// </summary>
    /// <param name="message">The error message that describes the parsing error.</param>
    /// <param name="line">The line number where the parsing error occurred.</param>
    /// <param name="column">The column number where the parsing error occurred.</param>
    public ParseException(string message, int line, int column)
        : base($"Parse error at line {line}, column {column}: {message}")
    {
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the line number where the parsing error occurred.
    /// </summary>
    /// <value>
    /// The line number in the source code that caused the parsing error.
    /// </value>
    public int Line { get; }

    /// <summary>
    /// Gets the column number where the parsing error occurred.
    /// </summary>
    /// <value>
    /// The column number associated with the parsing error.
    /// </value>
    /// <remarks>
    /// This property provides the specific column position in the source code where the parsing error was detected.
    /// It is useful for pinpointing the exact location of syntax issues or other parsing-related problems.
    /// </remarks>
    public int Column { get; }
}