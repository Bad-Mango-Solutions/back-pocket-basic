// <copyright file="LomemStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// LOMEM: statement - sets bottom of variable memory.
/// </summary>
public class LomemStatement(IExpression address) : IStatement
{
    /// <summary>
    /// Gets the address expression for the LOMEM statement.
    /// </summary>
    public IExpression Address { get; } = address;

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLomemStatement(this);
}