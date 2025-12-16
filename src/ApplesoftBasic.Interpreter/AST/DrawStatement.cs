// <copyright file="DrawStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// DRAW statement - draws a shape.
/// </summary>
public class DrawStatement : IStatement
{
    public IExpression ShapeNumber { get; }

    public IExpression? AtX { get; set; }

    public IExpression? AtY { get; set; }

    public DrawStatement(IExpression shapeNumber)
    {
        ShapeNumber = shapeNumber;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDrawStatement(this);
}