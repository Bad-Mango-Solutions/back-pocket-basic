// <copyright file="NumberLiteral.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents a numeric literal in the Abstract Syntax Tree (AST) of the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// This class encapsulates a numeric value and provides functionality to interact with it
/// as part of the AST. It implements the <see cref="IExpression"/> interface, allowing it
/// to be visited by an <see cref="IAstVisitor{T}"/>.
/// </remarks>
public class NumberLiteral : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NumberLiteral"/> class with the specified numeric value.
    /// </summary>
    /// <param name="value">The numeric value represented by this literal in the Abstract Syntax Tree (AST).</param>
    /// <remarks>
    /// This constructor is used to create a numeric literal node in the AST of the Applesoft BASIC interpreter.
    /// The <paramref name="value"/> parameter represents the numeric value encapsulated by this node.
    /// </remarks>
    public NumberLiteral(double value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the numeric value represented by this <see cref="NumberLiteral"/> in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <value>
    /// The numeric value encapsulated by this <see cref="NumberLiteral"/> instance.
    /// </value>
    /// <remarks>
    /// This property provides access to the numeric value that this node represents in the AST of the Applesoft BASIC interpreter.
    /// It is used during interpretation to evaluate expressions involving numeric literals.
    /// </remarks>
    public double Value { get; }

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that processes this <see cref="NumberLiteral"/> node.</param>
    /// <returns>The result of the visitor's operation on this <see cref="NumberLiteral"/> node.</returns>
    /// <remarks>
    /// This method allows the <see cref="NumberLiteral"/> node to participate in the visitor pattern,
    /// enabling external operations to be performed on it without modifying its structure.
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNumberLiteral(this);
}