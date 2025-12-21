// <copyright file="GotoStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// GOTO statement.
/// </summary>
public class GotoStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GotoStatement"/> class.
    /// </summary>
    /// <param name="lineNumber">The line number to jump to.</param>
    public GotoStatement(int lineNumber) => LineNumber = lineNumber;

    /// <summary>
    /// Gets the line number to jump to.
    /// </summary>
    public int LineNumber { get; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitGotoStatement(this);
}