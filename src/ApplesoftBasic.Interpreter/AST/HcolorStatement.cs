// <copyright file="HcolorStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

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