// <copyright file="IAstNode.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Base interface for all AST nodes.
/// </summary>
public interface IAstNode
{
    /// <summary>
    /// Accept a visitor for the visitor pattern.
    /// </summary>
    /// <typeparam name="T">The return type of the visitor's operation.</typeparam>
    /// <param name="visitor">The visitor instance to accept.</param>
    /// <returns>The result of the visitor's operation.</returns>
    T Accept<T>(IAstVisitor<T> visitor);
}