// <copyright file="DimStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// DIM statement.
/// </summary>
public class DimStatement : IStatement
{
    public List<ArrayDeclaration> Arrays { get; } = new();

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDimStatement(this);
}