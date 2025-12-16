// <copyright file="VtabStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// VTAB statement - vertical tab.
/// </summary>
public class VtabStatement(IExpression row) : IStatement
{
    /// <summary>
    /// Gets the row expression for the VTAB statement.
    /// </summary>
    public IExpression Row { get; } = row;

    /// <summary>
    /// Accepts a visitor for this AST node.
    /// </summary>
    /// <typeparam name="T">The return type of the visitor.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The result of visiting this node.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVtabStatement(this);
}