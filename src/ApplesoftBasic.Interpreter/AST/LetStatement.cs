// <copyright file="LetStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// LET assignment statement (LET keyword is optional).
/// </summary>
public class LetStatement : IStatement
{
    public VariableExpression Variable { get; }

    public IExpression Value { get; }

    public List<IExpression>? ArrayIndices { get; set; }

    public LetStatement(VariableExpression variable, IExpression value)
    {
        Variable = variable;
        Value = value;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLetStatement(this);
}