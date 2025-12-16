// <copyright file="NextStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// NEXT statement.
/// </summary>
public class NextStatement : IStatement
{
    public List<string> Variables { get; } = new();

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNextStatement(this);
}