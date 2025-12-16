// <copyright file="ClearStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// CLEAR statement - clears variables.
/// </summary>
public class ClearStatement : IStatement
{
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitClearStatement(this);
}