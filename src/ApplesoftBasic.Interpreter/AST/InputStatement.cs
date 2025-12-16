// <copyright file="InputStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// INPUT statement.
/// </summary>
public class InputStatement : IStatement
{
    public string? Prompt { get; set; }

    public List<VariableExpression> Variables { get; } = new();

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitInputStatement(this);
}