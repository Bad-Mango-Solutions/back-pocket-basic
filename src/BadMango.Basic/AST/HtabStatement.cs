// <copyright file="HtabStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// HTAB statement - horizontal tab.
/// </summary>
public class HtabStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtabStatement"/> class.
    /// </summary>
    /// <param name="column">The column expression for the horizontal tab.</param>
    public HtabStatement(IExpression column) => Column = column;

    /// <summary>
    /// Gets the column expression for the horizontal tab.
    /// </summary>
    public IExpression Column { get; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHtabStatement(this);
}