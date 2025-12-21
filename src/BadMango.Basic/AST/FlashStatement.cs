// <copyright file="FlashStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

using Execution;

/// <summary>
/// Represents the FLASH statement in Applesoft BASIC, which enables flashing text mode.
/// </summary>
/// <remarks>
/// The FLASH statement is used to set the text display mode to flashing.
/// It is part of the Applesoft BASIC syntax and is interpreted by the <see cref="BasicInterpreter"/>.
/// </remarks>
public class FlashStatement : IStatement
{
    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="FlashStatement"/>.</param>
    /// <returns>The result of the visitor's <see cref="IAstVisitor{T}.VisitFlashStatement(FlashStatement)"/> method.</returns>
    /// <remarks>
    /// This method enables the double-dispatch mechanism for processing the <see cref="FlashStatement"/> node
    /// in the Abstract Syntax Tree (AST).
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFlashStatement(this);
}