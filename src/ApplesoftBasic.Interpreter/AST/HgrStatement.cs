// <copyright file="HgrStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// HGR statement - high-resolution graphics mode.
/// </summary>
public class HgrStatement : IStatement
{
    public bool IsHgr2 { get; set; }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHgrStatement(this);
}