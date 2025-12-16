// <copyright file="TextStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// TEXT statement - switches to text mode.
/// </summary>
public class TextStatement : IStatement
{
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTextStatement(this);
}