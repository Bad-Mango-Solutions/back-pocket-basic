// <copyright file="InputStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// INPUT statement.
/// </summary>
public class InputStatement : IStatement
{
    /// <summary>
    /// Gets or sets the optional prompt string displayed to the user
    /// when the INPUT statement is executed.
    /// </summary>
    /// <remarks>
    /// If the prompt is not specified, a default prompt ("?") will be used.
    /// The prompt string is automatically appended with a question mark ("?")
    /// if it does not already end with one.
    /// </remarks>
    public string? Prompt { get; set; }

    /// <summary>
    /// Gets the list of variables that are part of the INPUT statement.
    /// </summary>
    /// <remarks>
    /// Each variable in the list is represented as a <see cref="VariableExpression"/>.
    /// These variables are populated during parsing and are used to store the user-provided input values.
    /// </remarks>
    public List<VariableExpression> Variables { get; } = [];

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="InputStatement"/>.</param>
    /// <returns>The result produced by the visitor after processing this <see cref="InputStatement"/>.</returns>
    /// <remarks>
    /// This method allows the <see cref="InputStatement"/> to participate in the visitor pattern,
    /// enabling external operations to be performed on it without modifying its structure.
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitInputStatement(this);
}