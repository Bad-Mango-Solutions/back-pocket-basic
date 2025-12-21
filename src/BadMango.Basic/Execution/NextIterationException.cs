// <copyright file="NextIterationException.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Execution;

/// <summary>
/// Represents an exception that is thrown to signal the continuation of a loop iteration
/// within the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// This exception is used internally by the interpreter to manage control flow for
/// constructs such as FOR loops. It is caught and handled to resume execution
/// at the appropriate point in the loop.
/// </remarks>
internal class NextIterationException : Exception
{
}