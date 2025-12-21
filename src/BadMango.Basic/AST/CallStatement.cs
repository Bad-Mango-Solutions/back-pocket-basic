// <copyright file="CallStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// CALL statement.
/// </summary>
public class CallStatement(IExpression address) : IStatement
{
    /// <summary>
    /// Gets the address expression for the CALL statement.
    /// </summary>
    public IExpression Address { get; } = address;

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCallStatement(this);
}