// <copyright file="InverseStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Represents the INVERSE statement in Applesoft BASIC, which sets the text mode to inverse.
/// </summary>
public class InverseStatement : IStatement
{
    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface and processes this <see cref="InverseStatement"/> node.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this node.</param>
    /// <returns>The result of the visitor's processing of this <see cref="InverseStatement"/> node.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitInverseStatement(this);
}