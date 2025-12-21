// <copyright file="HcolorStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// HCOLOR= statement - sets hi-res color.
/// </summary>
public class HcolorStatement(IExpression color) : IStatement
{
    /// <summary>
    /// Gets the color expression for the HCOLOR statement.
    /// </summary>
    public IExpression Color { get; } = color;

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHcolorStatement(this);
}