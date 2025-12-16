// <copyright file="IfStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// IF-THEN statement.
/// </summary>
public class IfStatement : IStatement
{
    public IExpression Condition { get; }

    public List<IStatement> ThenBranch { get; } = new();

    public int? GotoLineNumber { get; set; }

    public IfStatement(IExpression condition)
    {
        Condition = condition;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIfStatement(this);
}