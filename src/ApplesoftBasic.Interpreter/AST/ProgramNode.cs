// <copyright file="ProgramNode.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Represents the entire BASIC program.
/// </summary>
public class ProgramNode : IAstNode
{
    public List<LineNode> Lines { get; } = new();

    public List<object> DataValues { get; } = new();

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitProgram(this);
}