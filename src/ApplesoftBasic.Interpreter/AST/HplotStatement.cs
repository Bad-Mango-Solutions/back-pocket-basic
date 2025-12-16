// <copyright file="HplotStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// HPLOT statement - plots in hi-res.
/// </summary>
public class HplotStatement : IStatement
{
    public List<(IExpression X, IExpression Y)> Points { get; } = new();

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHplotStatement(this);
}