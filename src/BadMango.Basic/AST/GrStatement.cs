// <copyright file="GrStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents the GR statement in Applesoft BASIC, which activates the low-resolution graphics mode.
/// </summary>
public class GrStatement : IStatement
{
    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface,
    /// allowing the visitor to process this <see cref="GrStatement"/> node.
    /// </summary>
    /// <typeparam name="T">The return type of the visitor's operation.</typeparam>
    /// <param name="visitor">The visitor instance that processes this node.</param>
    /// <returns>The result of the visitor's operation.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitGrStatement(this);
}