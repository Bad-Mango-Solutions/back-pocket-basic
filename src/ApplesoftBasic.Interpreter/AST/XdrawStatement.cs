// <copyright file="XdrawStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// XDRAW statement - XOR draws a shape.
/// </summary>
public class XdrawStatement : IStatement
{
    public IExpression ShapeNumber { get; }

    public IExpression? AtX { get; set; }

    public IExpression? AtY { get; set; }

    public XdrawStatement(IExpression shapeNumber)
    {
        ShapeNumber = shapeNumber;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitXdrawStatement(this);
}