// <copyright file="PlotStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// PLOT statement - plots a point in lo-res.
/// </summary>
public class PlotStatement : IStatement
{
    public IExpression X { get; }

    public IExpression Y { get; }

    public PlotStatement(IExpression x, IExpression y)
    {
        X = x;
        Y = y;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPlotStatement(this);
}