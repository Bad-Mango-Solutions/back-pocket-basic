// <copyright file="NormalStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// NORMAL statement - sets normal text mode.
/// </summary>
public class NormalStatement : IStatement
{
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNormalStatement(this);
}