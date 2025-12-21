// <copyright file="GosubStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// GOSUB statement.
/// </summary>
public class GosubStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GosubStatement"/> class.
    /// </summary>
    /// <param name="lineNumber">The line number to GOSUB to.</param>
    public GosubStatement(int lineNumber)
    {
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the line number to GOSUB to.
    /// </summary>
    public int LineNumber { get; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitGosubStatement(this);
}