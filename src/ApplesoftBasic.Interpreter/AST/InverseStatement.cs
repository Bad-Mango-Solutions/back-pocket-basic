// <copyright file="InverseStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// INVERSE statement - sets inverse text mode.
/// </summary>
public class InverseStatement : IStatement
{
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitInverseStatement(this);
}