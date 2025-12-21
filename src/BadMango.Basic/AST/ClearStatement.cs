// <copyright file="ClearStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents the CLEAR statement in Applesoft BASIC, which is used to clear all variables, functions, loops,
/// and other runtime state in the interpreter.
/// </summary>
public class ClearStatement : IStatement
{
    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="ClearStatement"/>.</param>
    /// <returns>The result of the visitor's <see cref="IAstVisitor{T}.VisitClearStatement(ClearStatement)"/> method.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitClearStatement(this);
}