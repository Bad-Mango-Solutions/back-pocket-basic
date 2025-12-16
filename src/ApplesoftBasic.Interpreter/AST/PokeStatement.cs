// <copyright file="PokeStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// POKE statement.
/// </summary>
public class PokeStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PokeStatement"/> class.
    /// </summary>
    /// <param name="address">The address expression to poke.</param>
    /// <param name="value">The value expression to poke.</param>
    public PokeStatement(IExpression address, IExpression value)
    {
        Address = address;
        Value = value;
    }

    /// <summary>
    /// Gets the address expression.
    /// </summary>
    public IExpression Address { get; }

    /// <summary>
    /// Gets the value expression.
    /// </summary>
    public IExpression Value { get; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPokeStatement(this);
}