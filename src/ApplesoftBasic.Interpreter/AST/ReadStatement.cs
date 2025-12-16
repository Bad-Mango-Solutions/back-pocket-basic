// <copyright file="ReadStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// READ statement.
/// </summary>
public class ReadStatement : IStatement
{
    public List<VariableExpression> Variables { get; } = new();

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitReadStatement(this);
}