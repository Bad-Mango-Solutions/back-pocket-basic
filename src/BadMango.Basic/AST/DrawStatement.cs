// <copyright file="DrawStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// DRAW statement - draws a shape.
/// </summary>
public class DrawStatement(IExpression shapeNumber) : IStatement
{
    /// <summary>
    /// Gets the shape number expression.
    /// </summary>
    public IExpression ShapeNumber { get; } = shapeNumber;

    /// <summary>
    /// Gets or sets the X coordinate expression.
    /// </summary>
    public IExpression? AtX { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate expression.
    /// </summary>
    public IExpression? AtY { get; set; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDrawStatement(this);
}