// <copyright file="AmpersandStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Represents the ampersand ('&amp;') statement in Applesoft BASIC.
/// </summary>
/// <remarks>
/// The ampersand operator is used to call machine language routines in Applesoft BASIC.
/// When executed, it performs a JSR instruction to memory location $03F5 (1013 decimal).
/// User-provided machine language routines can be loaded at this address to extend
/// the BASIC interpreter's functionality.
/// </remarks>
public class AmpersandStatement : IStatement
{
    /// <summary>
    /// Accepts a visitor that processes this ampersand statement.
    /// </summary>
    /// <typeparam name="T">The return type of the visitor.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The result of visiting this node.</returns>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAmpersandStatement(this);
}

