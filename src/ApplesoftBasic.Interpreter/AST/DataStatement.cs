// <copyright file="DataStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// DATA statement.
/// </summary>
public class DataStatement : IStatement
{
    public List<object> Values { get; } = new();

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDataStatement(this);
}