// <copyright file="PokeStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// POKE statement.
/// </summary>
public class PokeStatement : IStatement
{
    public IExpression Address { get; }

    public IExpression Value { get; }

    public PokeStatement(IExpression address, IExpression value)
    {
        Address = address;
        Value = value;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPokeStatement(this);
}