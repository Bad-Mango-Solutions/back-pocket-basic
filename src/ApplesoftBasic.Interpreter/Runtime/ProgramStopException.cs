// <copyright file="ProgramStopException.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Exception thrown to signal STOP command.
/// </summary>
public class ProgramStopException : Exception
{
    public int LineNumber { get; }

    public ProgramStopException(int lineNumber)
        : base($"BREAK IN {lineNumber}")
    {
        LineNumber = lineNumber;
    }
}