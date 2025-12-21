// <copyright file="XdrawStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// XDRAW statement - XOR draws a shape.
/// </summary>
public class XdrawStatement(IExpression shapeNumber) : IStatement
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
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitXdrawStatement(this);
}