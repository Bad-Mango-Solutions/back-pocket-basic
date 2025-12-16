// <copyright file="GotoException.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Execution;

/// <summary>
/// Exception used to signal GOTO.
/// </summary>
internal class GotoException : Exception
{
    public int LineNumber { get; }

    public GotoException(int lineNumber) => LineNumber = lineNumber;
}