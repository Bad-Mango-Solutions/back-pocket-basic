// <copyright file="OnGosubStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// ON ... GOSUB statement.
/// </summary>
public class OnGosubStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OnGosubStatement"/> class.
    /// </summary>
    /// <param name="expression">The expression to evaluate for ON ... GOSUB.</param>
    public OnGosubStatement(IExpression expression)
    {
        Expression = expression;
    }

    /// <summary>
    /// Gets the expression to evaluate for ON ... GOSUB.
    /// </summary>
    public IExpression Expression { get; }

    /// <summary>
    /// Gets the list of line numbers for GOSUB targets.
    /// </summary>
    public List<int> LineNumbers { get; } = [];

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitOnGosubStatement(this);
}