// <copyright file="GosubStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// GOSUB statement.
/// </summary>
public class GosubStatement : IStatement
{
    public int LineNumber { get; }

    public GosubStatement(int lineNumber)
    {
        LineNumber = lineNumber;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitGosubStatement(this);
}