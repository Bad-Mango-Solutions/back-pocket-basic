// <copyright file="HcolorStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// HCOLOR= statement - sets hi-res color.
/// </summary>
public class HcolorStatement : IStatement
{
    public IExpression Color { get; }

    public HcolorStatement(IExpression color)
    {
        Color = color;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHcolorStatement(this);
}