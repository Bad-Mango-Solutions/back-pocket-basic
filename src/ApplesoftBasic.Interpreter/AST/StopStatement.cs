// <copyright file="StopStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// STOP statement.
/// </summary>
public class StopStatement : IStatement
{
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStopStatement(this);
}