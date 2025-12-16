// <copyright file="ColorStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// COLOR= statement - sets lo-res color.
/// </summary>
public class ColorStatement : IStatement
{
    public IExpression Color { get; }

    public ColorStatement(IExpression color)
    {
        Color = color;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitColorStatement(this);
}