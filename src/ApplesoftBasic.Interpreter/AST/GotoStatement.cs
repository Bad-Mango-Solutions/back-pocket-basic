// <copyright file="GotoStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

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