// <copyright file="GotoStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// GOTO statement.
/// </summary>
public class GotoStatement : IStatement
{
    public int LineNumber { get; }

    public GotoStatement(int lineNumber)
    {
        LineNumber = lineNumber;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitGotoStatement(this);
}