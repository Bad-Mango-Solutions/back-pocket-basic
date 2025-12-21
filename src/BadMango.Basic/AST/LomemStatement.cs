// <copyright file="LomemStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

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