// <copyright file="HimemStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// HIMEM: statement - sets top of memory.
/// </summary>
public class HimemStatement(IExpression address) : IStatement
{
    /// <summary>
    /// Gets the address expression for the HIMEM statement.
    /// </summary>
    public IExpression Address { get; } = address;

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHimemStatement(this);
}