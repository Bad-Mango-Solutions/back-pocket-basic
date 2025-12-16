// <copyright file="LomemStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// LOMEM: statement - sets bottom of variable memory.
/// </summary>
public class LomemStatement : IStatement
{
    public IExpression Address { get; }

    public LomemStatement(IExpression address)
    {
        Address = address;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLomemStatement(this);
}