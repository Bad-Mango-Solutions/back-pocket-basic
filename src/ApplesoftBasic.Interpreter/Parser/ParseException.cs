// <copyright file="ParseException.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Parser;

/// <summary>
/// Exception thrown when parsing fails.
/// </summary>
public class ParseException : Exception
{
    public int Line { get; }

    public int Column { get; }

    public ParseException(string message, int line, int column)
        : base($"Parse error at line {line}, column {column}: {message}")
    {
        Line = line;
        Column = column;
    }
}
