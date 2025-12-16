// <copyright file="GetStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// GET statement (single character input).
/// </summary>
public class GetStatement : IStatement
{
    public VariableExpression Variable { get; }

    public GetStatement(VariableExpression variable)
    {
        Variable = variable;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitGetStatement(this);
}