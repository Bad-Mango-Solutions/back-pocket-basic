// <copyright file="HomeStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// HOME statement - clears screen.
/// </summary>
public class HomeStatement : IStatement
{
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHomeStatement(this);
}