// <copyright file="DimStatement.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Represents a DIM statement in Applesoft BASIC, which is used to declare arrays with specified dimensions.
/// </summary>
/// <remarks>
/// The <c>DimStatement</c> class is part of the Abstract Syntax Tree (AST) and encapsulates the array declarations
/// defined by a DIM statement in Applesoft BASIC. It implements the <see cref="IStatement"/> interface, allowing it
/// to be visited by an <see cref="IAstVisitor{T}"/> for interpretation or compilation.
/// </remarks>
/// <example>
/// For example, the Applesoft BASIC statement <c>10 DIM A(10), B(5, 5)</c> would be represented as a <c>DimStatement</c>
/// containing two <see cref="ArrayDeclaration"/> objects: one for array <c>A</c> with a single dimension of size 10,
/// and another for array <c>B</c> with two dimensions of sizes 5 and 5.
/// </example>
public class DimStatement : IStatement
{
    /// <summary>
    /// Gets the list of arrays declared by the DIM statement.
    /// </summary>
    /// <value>
    /// A list of <see cref="ArrayDeclaration"/> objects, each representing an array declared in the DIM statement,
    /// including its name and dimensions.
    /// </value>
    /// <remarks>
    /// This property contains all the arrays specified in the DIM statement, with each array's dimensions
    /// represented as a collection of expressions.
    /// </remarks>
    /// <example>
    /// For example, the DIM statement <c>10 DIM A(10), B(5, 5)</c> would result in this property containing
    /// two <see cref="ArrayDeclaration"/> objects: one for array <c>A</c> with a single dimension of size 10,
    /// and another for array <c>B</c> with two dimensions of sizes 5 and 5.
    /// </example>
    public List<ArrayDeclaration> Arrays { get; } = [];

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface and allows the visitor
    /// to process this <see cref="DimStatement"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor that processes this <see cref="DimStatement"/> instance.</param>
    /// <returns>The result of the visitor's processing of this <see cref="DimStatement"/>.</returns>
    /// <remarks>
    /// This method is part of the Visitor design pattern, enabling external operations to be performed
    /// on the <see cref="DimStatement"/> without modifying its structure.
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDimStatement(this);
}