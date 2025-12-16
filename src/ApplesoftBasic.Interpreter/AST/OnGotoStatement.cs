// <copyright file="OnGotoStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// ON ... GOTO statement.
/// </summary>
public class OnGotoStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OnGotoStatement"/> class.
    /// </summary>
    /// <param name="expression">The expression to evaluate for the ON ... GOTO statement.</param>
    public OnGotoStatement(IExpression expression)
    {
        Expression = expression;
    }

    /// <summary>
    /// Gets the expression to evaluate for the ON ... GOTO statement.
    /// </summary>
    public IExpression Expression { get; }

    /// <summary>
    /// Gets the list of line numbers to jump to.
    /// </summary>
    public List<int> LineNumbers { get; } = [];

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitOnGotoStatement(this);
}