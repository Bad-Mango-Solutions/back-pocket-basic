// <copyright file="EndStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// END statement.
/// </summary>
public class EndStatement : IStatement
{
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitEndStatement(this);
}