// <copyright file="PlotStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// PLOT statement - plots a point in lo-res.
/// </summary>
public class PlotStatement : IStatement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlotStatement"/> class.
    /// </summary>
    /// <param name="x">The X coordinate expression.</param>
    /// <param name="y">The Y coordinate expression.</param>
    public PlotStatement(IExpression x, IExpression y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets the X coordinate expression.
    /// </summary>
    public IExpression X { get; }

    /// <summary>
    /// Gets the Y coordinate expression.
    /// </summary>
    public IExpression Y { get; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPlotStatement(this);
}