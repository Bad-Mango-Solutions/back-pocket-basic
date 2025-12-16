// <copyright file="HimemStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// HIMEM: statement - sets top of memory.
/// </summary>
public class HimemStatement : IStatement
{
    public IExpression Address { get; }

    public HimemStatement(IExpression address)
    {
        Address = address;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHimemStatement(this);
}