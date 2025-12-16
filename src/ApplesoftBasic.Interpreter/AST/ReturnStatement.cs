// <copyright file="ReturnStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// RETURN statement.
/// </summary>
public class ReturnStatement : IStatement
{
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitReturnStatement(this);
}