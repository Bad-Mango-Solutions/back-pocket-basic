// <copyright file="IfStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// IF-THEN statement.
/// </summary>
public class IfStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IfStatement"/> class.
    /// </summary>
    /// <param name="condition">The condition expression for the IF statement.</param>
    public IfStatement(IExpression condition)
    {
        Condition = condition;
    }

    /// <summary>
    /// Gets the condition expression for the IF statement.
    /// </summary>
    public IExpression Condition { get; }

    /// <summary>
    /// Gets the list of statements to execute if the condition is true.
    /// </summary>
    public List<IStatement> ThenBranch { get; } = [];

    /// <summary>
    /// Gets or sets the line number to GOTO if the condition is true.
    /// </summary>
    public int? GotoLineNumber { get; set; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIfStatement(this);
}