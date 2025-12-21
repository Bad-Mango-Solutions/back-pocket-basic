// <copyright file="TextStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents a TEXT statement in the Applesoft BASIC interpreter's Abstract Syntax Tree (AST).
/// The TEXT statement is used to switch the display mode to text mode.
/// </summary>
public class TextStatement : IStatement
{
    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="TextStatement"/>.</param>
    /// <returns>The result produced by the visitor after processing this <see cref="TextStatement"/>.</returns>
    /// <remarks>
    /// This method allows the <see cref="TextStatement"/> to participate in the visitor pattern,
    /// enabling external operations to be performed on it without modifying its structure.
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTextStatement(this);
}