// <copyright file="GotoException.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Execution;

/// <summary>
/// Represents an exception used to signal a GOTO operation within the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// This exception is thrown to transfer control to a specific line number during the execution
/// of Applesoft BASIC programs. It is utilized in statements such as GOTO, GOSUB, and ON-GOTO.
/// </remarks>
internal class GotoException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GotoException"/> class with the specified line number.
    /// </summary>
    /// <param name="lineNumber">The line number to which the execution should jump.</param>
    public GotoException(int lineNumber) => LineNumber = lineNumber;

    /// <summary>
    /// The non-existent line number that the BASIC program attempted to jump to.
    /// </summary>
    public int LineNumber { get; }
}