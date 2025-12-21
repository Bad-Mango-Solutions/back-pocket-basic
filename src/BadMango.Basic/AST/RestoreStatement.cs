// <copyright file="RestoreStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents a RESTORE statement in the Applesoft BASIC interpreter's Abstract Syntax Tree (AST).
/// The RESTORE statement resets the data pointer to the beginning of the DATA statements
/// in the program or to a specified line number, allowing subsequent READ statements to
/// retrieve data from the reset position.
/// </summary>
public class RestoreStatement : IStatement
{
    /// <summary>
    /// Gets or sets the line number to which the data pointer should be reset.
    /// If the value is <c>null</c>, the data pointer is reset to the beginning
    /// of the DATA statements in the program. If a line number is specified,
    /// the data pointer is reset to the DATA statements starting at that line.
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The return type of the visitor's operation.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="RestoreStatement"/>.</param>
    /// <returns>The result of the visitor's operation.</returns>
    /// <remarks>
    /// This method allows the <see cref="RestoreStatement"/> to participate in the visitor pattern,
    /// enabling external operations to be performed on it without modifying its structure.
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitRestoreStatement(this);
}