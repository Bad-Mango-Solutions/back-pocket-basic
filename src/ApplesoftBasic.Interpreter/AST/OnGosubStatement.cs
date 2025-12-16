// <copyright file="OnGosubStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// ON ... GOSUB statement.
/// </summary>
public class OnGosubStatement : IStatement
{
    public IExpression Expression { get; }

    public List<int> LineNumbers { get; } = new();

    public OnGosubStatement(IExpression expression)
    {
        Expression = expression;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitOnGosubStatement(this);
}