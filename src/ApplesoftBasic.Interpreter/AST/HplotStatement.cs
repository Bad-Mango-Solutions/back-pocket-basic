// <copyright file="HplotStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Represents the HPLOT statement in Applesoft BASIC, which is used to plot points or draw lines in high-resolution graphics mode.
/// </summary>
/// <remarks>
/// The HPLOT statement allows specifying a series of points to plot or draw lines between them.
/// It supports the use of the TO keyword to connect multiple points in a sequence.
/// </remarks>
public class HplotStatement : IStatement
{
    /// <summary>
    /// Gets the list of points to be plotted or connected in the HPLOT statement.
    /// </summary>
    /// <remarks>
    /// Each point is represented as a tuple containing X and Y coordinates,
    /// both of which are expressions that evaluate to numeric values.
    /// The HPLOT statement supports plotting individual points or drawing lines
    /// between multiple points specified in sequence.
    /// </remarks>
    public List<(IExpression X, IExpression Y)> Points { get; } = [];

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface, allowing the visitor
    /// to process this <see cref="HplotStatement"/> node.
    /// </summary>
    /// <typeparam name="T">The return type of the visitor's operation.</typeparam>
    /// <param name="visitor">The visitor instance that processes this node.</param>
    /// <returns>The result of the visitor's operation.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHplotStatement(this);
}