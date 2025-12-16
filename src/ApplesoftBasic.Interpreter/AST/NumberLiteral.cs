// <copyright file="NumberLiteral.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Numeric literal.
/// </summary>
public class NumberLiteral : IExpression
{
    public NumberLiteral(double value)
    {
        Value = value;
    }

    public double Value { get; }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNumberLiteral(this);
}