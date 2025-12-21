// <copyright file="NormalStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents the NORMAL statement in Applesoft BASIC, which sets the text mode to normal.
/// </summary>
/// <remarks>
/// The NORMAL statement is used to switch the text mode to its default (normal) state.
/// This class is part of the Abstract Syntax Tree (AST) and implements the <see cref="IStatement"/> interface.
/// </remarks>
public class NormalStatement : IStatement
{
    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface and processes this <see cref="NormalStatement"/> node.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this node.</param>
    /// <returns>The result produced by the visitor after processing this node.</returns>
    /// <remarks>
    /// This method enables the visitor pattern, allowing external operations to be performed on this <see cref="NormalStatement"/> node
    /// without modifying its structure.
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNormalStatement(this);
}