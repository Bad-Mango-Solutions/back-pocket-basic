// <copyright file="CallStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// CALL statement.
/// </summary>
public class CallStatement : IStatement
{
    public IExpression Address { get; }

    public CallStatement(IExpression address)
    {
        Address = address;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCallStatement(this);
}