// <copyright file="SleepStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// SLEEP statement (custom extension) - pauses execution.
/// </summary>
public class SleepStatement(IExpression milliseconds) : IStatement
{
    /// <summary>
    /// Gets the expression representing the number of milliseconds to sleep.
    /// </summary>
    public IExpression Milliseconds { get; } = milliseconds;

    /// <summary>
    /// Accepts a visitor that processes this sleep statement.
    /// </summary>
    /// <typeparam name="T">The return type of the visitor.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The result of visiting this node.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSleepStatement(this);
}