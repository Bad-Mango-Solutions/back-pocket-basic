// <copyright file="NextStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents a NEXT statement in an Applesoft BASIC program.
/// </summary>
/// <remarks>
/// The NEXT statement is used to advance the execution of a FOR loop. It can optionally
/// specify one or more variables to indicate which loop(s) to advance.
/// </remarks>
public class NextStatement : IStatement
{
    /// <summary>
    /// Gets the list of variable names associated with the NEXT statement.
    /// </summary>
    /// <remarks>
    /// The variables represent loop control variables for which the NEXT statement applies.
    /// If no variables are specified, the NEXT statement applies to the innermost loop.
    /// </remarks>
    public List<string> Variables { get; } = [];

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="NextStatement"/>.</param>
    /// <returns>The result of the visitor's <see cref="IAstVisitor{T}.VisitNextStatement"/> method.</returns>
    /// <remarks>
    /// This method allows the <see cref="NextStatement"/> to participate in the visitor pattern,
    /// enabling external operations to be performed on it without modifying its structure.
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNextStatement(this);
}