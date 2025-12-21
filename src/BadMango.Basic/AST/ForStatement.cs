// <copyright file="ForStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// FOR statement.
/// </summary>
public class ForStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForStatement"/> class.
    /// </summary>
    /// <param name="variable">The loop variable name.</param>
    /// <param name="start">The start expression.</param>
    /// <param name="end">The end expression.</param>
    public ForStatement(string variable, IExpression start, IExpression end)
    {
        Variable = variable;
        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the loop variable name.
    /// </summary>
    public string Variable { get; }

    /// <summary>
    /// Gets the start expression.
    /// </summary>
    public IExpression Start { get; }

    /// <summary>
    /// Gets the end expression.
    /// </summary>
    public IExpression End { get; }

    /// <summary>
    /// Gets or sets the step expression.
    /// </summary>
    public IExpression? Step { get; set; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitForStatement(this);
}