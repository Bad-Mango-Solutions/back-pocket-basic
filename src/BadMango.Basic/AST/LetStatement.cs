// <copyright file="LetStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// LET assignment statement (LET keyword is optional).
/// </summary>
public class LetStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LetStatement"/> class.
    /// </summary>
    /// <param name="variable">The variable to assign to.</param>
    /// <param name="value">The value to assign.</param>
    public LetStatement(VariableExpression variable, IExpression value)
    {
        Variable = variable;
        Value = value;
    }

    /// <summary>
    /// Gets the variable to assign to.
    /// </summary>
    public VariableExpression Variable { get; }

    /// <summary>
    /// Gets the value to assign.
    /// </summary>
    public IExpression Value { get; }

    /// <summary>
    /// Gets or sets the array indices if the variable is an array.
    /// </summary>
    public List<IExpression>? ArrayIndices { get; set; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLetStatement(this);
}