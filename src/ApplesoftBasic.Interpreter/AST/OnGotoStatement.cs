// <copyright file="OnGotoStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// ON ... GOTO statement.
/// </summary>
public class OnGotoStatement : IStatement
{
    public IExpression Expression { get; }

    public List<int> LineNumbers { get; } = new();

    public OnGotoStatement(IExpression expression)
    {
        Expression = expression;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitOnGotoStatement(this);
}