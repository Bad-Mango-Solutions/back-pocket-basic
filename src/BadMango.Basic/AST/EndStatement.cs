// <copyright file="EndStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents the END statement in the Applesoft BASIC language.
/// This statement is used to signify the end of a program's execution.
/// </summary>
public class EndStatement : IStatement
{
    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The return type of the visitor's operation.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="EndStatement"/>.</param>
    /// <returns>The result of the visitor's operation.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitEndStatement(this);
}