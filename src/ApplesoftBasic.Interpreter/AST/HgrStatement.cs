// <copyright file="HgrStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Represents the HGR statement in Applesoft BASIC, which is used to activate
/// high-resolution graphics mode. This statement can operate in two modes:
/// HGR (standard high-resolution graphics) and HGR2 (extended high-resolution graphics).
/// </summary>
public class HgrStatement : IStatement
{
    /// <summary>
    /// Gets or sets a value indicating whether the HGR2 mode is activated.
    /// </summary>
    /// <value>
    /// <c>true</c> if HGR2 mode is activated; otherwise, <c>false</c>.
    /// </value>
    public bool IsHgr2 { get; set; }

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface, enabling
    /// traversal or processing of the <see cref="HgrStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="HgrStatement"/>.</param>
    /// <returns>The result of the visitor's operation on this <see cref="HgrStatement"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="visitor"/> is <c>null</c>.</exception>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitHgrStatement(this);
}