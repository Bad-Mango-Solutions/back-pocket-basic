// <copyright file="ReadStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents a READ statement in the Applesoft BASIC interpreter's Abstract Syntax Tree (AST).
/// The READ statement is used to assign values from DATA statements to specified variables.
/// </summary>
public class ReadStatement : IStatement
{
    /// <summary>
    /// Gets the list of variables that are assigned values from DATA statements
    /// in the READ statement.
    /// </summary>
    /// <remarks>
    /// Each variable in this list corresponds to a variable specified in the READ statement.
    /// The values for these variables are retrieved sequentially from the DATA statements
    /// during program execution.
    /// </remarks>
    public List<VariableExpression> Variables { get; } = [];

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The return type of the visitor's operation.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="ReadStatement"/>.</param>
    /// <returns>The result of the visitor's operation.</returns>
    /// <remarks>
    /// This method allows the <see cref="ReadStatement"/> to participate in the visitor pattern,
    /// enabling external operations to be performed on this node without modifying its structure.
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitReadStatement(this);
}