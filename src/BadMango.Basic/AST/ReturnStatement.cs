// <copyright file="ReturnStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents a RETURN statement in the Abstract Syntax Tree (AST) of the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// The RETURN statement is used to return control to the statement following the most recent GOSUB statement.
/// </remarks>
public class ReturnStatement : IStatement
{
    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="ReturnStatement"/>.</param>
    /// <returns>The result of the visitor's <see cref="IAstVisitor{T}.VisitReturnStatement"/> method.</returns>
    /// <remarks>
    /// This method enables the double-dispatch mechanism, allowing the visitor to process
    /// the <see cref="ReturnStatement"/> node in the Abstract Syntax Tree (AST).
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitReturnStatement(this);
}