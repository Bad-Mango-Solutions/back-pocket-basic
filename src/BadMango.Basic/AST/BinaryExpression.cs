// <copyright file="BinaryExpression.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

using Tokens;

/// <summary>
/// Binary operation (e.g., 1 + 2, A * B).
/// </summary>
public class BinaryExpression(IExpression left, TokenType op, IExpression right) : IExpression
{
    /// <summary>
    /// Gets the left operand of the binary expression.
    /// </summary>
    public IExpression Left { get; } = left;

    /// <summary>
    /// Gets the operator of the binary expression.
    /// </summary>
    public TokenType Operator { get; } = op;

    /// <summary>
    /// Gets the right operand of the binary expression.
    /// </summary>
    public IExpression Right { get; } = right;

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBinaryExpression(this);
}