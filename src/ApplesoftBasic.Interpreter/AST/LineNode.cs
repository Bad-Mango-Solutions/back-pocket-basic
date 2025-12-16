// <copyright file="LineNode.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Represents a single numbered line in the program.
/// </summary>
public class LineNode : IAstNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineNode"/> class.
    /// </summary>
    /// <param name="lineNumber">The line number of this program line.</param>
    public LineNode(int lineNumber)
    {
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Gets the line number of this program line.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Gets the list of statements in this program line.
    /// </summary>
    public List<IStatement> Statements { get; } = [];

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLine(this);
}