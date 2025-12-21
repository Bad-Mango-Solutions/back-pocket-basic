// <copyright file="HomeStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents the HOME statement in Applesoft BASIC, which clears the screen.
/// </summary>
public class HomeStatement : IStatement
{
    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The return type of the visitor's operation.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="HomeStatement"/>.</param>
    /// <returns>The result of the visitor's operation.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHomeStatement(this);
}