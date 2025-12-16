// <copyright file="LineNode.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Represents a single numbered line in the program.
/// </summary>
public class LineNode : IAstNode
{
    public int LineNumber { get; }

    public List<IStatement> Statements { get; } = new();

    public LineNode(int lineNumber)
    {
        LineNumber = lineNumber;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLine(this);
}