// <copyright file="RemStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// REM (comment) statement.
/// </summary>
public class RemStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemStatement"/> class.
    /// </summary>
    /// <param name="comment">The comment text for the REM statement.</param>
    public RemStatement(string comment)
    {
        Comment = comment;
    }

    /// <summary>
    /// Gets the comment text for the REM statement.
    /// </summary>
    public string Comment { get; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitRemStatement(this);
}