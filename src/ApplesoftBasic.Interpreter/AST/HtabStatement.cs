// <copyright file="HtabStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// HTAB statement - horizontal tab.
/// </summary>
public class HtabStatement : IStatement
{
    public IExpression Column { get; }

    public HtabStatement(IExpression column)
    {
        Column = column;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHtabStatement(this);
}