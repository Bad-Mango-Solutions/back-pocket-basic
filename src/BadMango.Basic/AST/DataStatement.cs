// <copyright file="DataStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents a DATA statement in the Applesoft BASIC interpreter's abstract syntax tree (AST).
/// </summary>
/// <remarks>
/// The DATA statement is used to define a list of values that can be read by subsequent READ statements.
/// These values are collected during parsing and stored for later use in program execution.
/// </remarks>
public class DataStatement : IStatement
{
    /// <summary>
    /// Gets the list of values defined in the DATA statement.
    /// </summary>
    /// <remarks>
    /// The <see cref="Values"/> property contains the collection of values specified in the DATA statement.
    /// These values can include numbers, strings, or identifiers and are used by subsequent READ statements
    /// during program execution. The values are collected during parsing and stored in this property for
    /// later retrieval.
    /// </remarks>
    public List<object> Values { get; } = [];

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="DataStatement"/>.</param>
    /// <returns>The result of the visitor's operation on this <see cref="DataStatement"/>.</returns>
    /// <remarks>
    /// This method allows the <see cref="DataStatement"/> to participate in the visitor pattern,
    /// enabling external operations to be performed on it without modifying its structure.
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDataStatement(this);
}