// <copyright file="StopStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents a STOP statement in the Abstract Syntax Tree (AST) of the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// The STOP statement is used to halt the execution of a program. It is typically used for debugging
/// or to intentionally pause program execution at a specific point.
/// </remarks>
public class StopStatement : IStatement
{
    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="StopStatement"/>.</param>
    /// <returns>The result produced by the visitor after processing this <see cref="StopStatement"/>.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStopStatement(this);
}