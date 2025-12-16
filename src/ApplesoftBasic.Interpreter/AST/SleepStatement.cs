// <copyright file="SleepStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// SLEEP statement (custom extension) - pauses execution.
/// </summary>
public class SleepStatement : IStatement
{
    public IExpression Milliseconds { get; }

    public SleepStatement(IExpression milliseconds)
    {
        Milliseconds = milliseconds;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSleepStatement(this);
}