// <copyright file="ColorStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// COLOR= statement - sets lo-res color.
/// </summary>
public class ColorStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColorStatement"/> class.
    /// </summary>
    /// <param name="color">The expression representing the color to be set for the lo-res graphics.</param>
    public ColorStatement(IExpression color)
    {
        Color = color;
    }

    /// <summary>
    /// Gets the expression representing the color to be set for the lo-res graphics.
    /// </summary>
    public IExpression Color { get; }

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="ColorStatement"/>.</param>
    /// <returns>The result produced by the visitor after processing this <see cref="ColorStatement"/>.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitColorStatement(this);
}