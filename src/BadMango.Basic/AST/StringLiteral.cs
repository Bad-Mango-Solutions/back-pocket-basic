// <copyright file="StringLiteral.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// String literal.
/// </summary>
public class StringLiteral : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringLiteral"/> class.
    /// </summary>
    /// <param name="value">The string value.</param>
    public StringLiteral(string value) => Value = value;

    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStringLiteral(this);
}