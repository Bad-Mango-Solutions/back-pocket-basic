// <copyright file="ArrayAccessExpression.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Array element access (e.g., A(1), B$(I,J)).
/// </summary>
public class ArrayAccessExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayAccessExpression"/> class.
    /// </summary>
    /// <param name="arrayName">The name of the array being accessed.</param>
    public ArrayAccessExpression(string arrayName)
    {
        ArrayName = arrayName;
    }

    /// <summary> Gets the name of the array being accessed. </summary>
    public string ArrayName { get; }

    /// <summary>
    /// Gets the list of index expressions used to access the array element.
    /// </summary>
    public List<IExpression> Indices { get; } = [];

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitArrayAccessExpression(this);
}