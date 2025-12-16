// <copyright file="VtabStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// VTAB statement - vertical tab.
/// </summary>
public class VtabStatement : IStatement
{
    public IExpression Row { get; }

    public VtabStatement(IExpression row)
    {
        Row = row;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVtabStatement(this);
}