// <copyright file="RemStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// REM (comment) statement.
/// </summary>
public class RemStatement : IStatement
{
    public string Comment { get; }

    public RemStatement(string comment)
    {
        Comment = comment;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitRemStatement(this);
}