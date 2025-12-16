// <copyright file="GrStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// GR statement - low-resolution graphics mode.
/// </summary>
public class GrStatement : IStatement
{
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitGrStatement(this);
}