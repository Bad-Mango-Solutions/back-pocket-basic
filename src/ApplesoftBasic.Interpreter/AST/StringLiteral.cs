// <copyright file="StringLiteral.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// String literal.
/// </summary>
public class StringLiteral : IExpression
{
    public string Value { get; }

    public StringLiteral(string value)
    {
        Value = value;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStringLiteral(this);
}