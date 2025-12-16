// <copyright file="BasicRuntimeException.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Exception thrown during BASIC program execution.
/// </summary>
public class BasicRuntimeException : Exception
{
    public int? LineNumber { get; }

    public BasicRuntimeException(string message, int? lineNumber = null)
        : base(lineNumber.HasValue ? $"{message} IN {lineNumber}" : message)
    {
        LineNumber = lineNumber;
    }
}