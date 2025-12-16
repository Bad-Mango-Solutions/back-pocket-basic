// <copyright file="GosubReturnAddress.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Represents a GOSUB return address.
/// </summary>
public class GosubReturnAddress
{
    public int LineIndex { get; }

    public int StatementIndex { get; }

    public GosubReturnAddress(int lineIndex, int statementIndex)
    {
        LineIndex = lineIndex;
        StatementIndex = statementIndex;
    }
}