// <copyright file="GetStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// GET statement (single character input).
/// </summary>
public class GetStatement(VariableExpression variable) : IStatement
{
    /// <summary>
    /// Gets the variable to store the input character.
    /// </summary>
    public VariableExpression Variable { get; } = variable;

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitGetStatement(this);
}