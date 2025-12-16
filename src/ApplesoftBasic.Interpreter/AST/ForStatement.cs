// <copyright file="ForStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// FOR statement.
/// </summary>
public class ForStatement : IStatement
{
    public string Variable { get; }

    public IExpression Start { get; }

    public IExpression End { get; }

    public IExpression? Step { get; set; }

    public ForStatement(string variable, IExpression start, IExpression end)
    {
        Variable = variable;
        Start = start;
        End = end;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitForStatement(this);
}