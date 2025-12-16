// <copyright file="UnaryExpression.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

using Tokens;

/// <summary>
/// Unary operation (e.g., -X, NOT flag).
/// </summary>
public class UnaryExpression(TokenType op, IExpression operand) : IExpression
{
    /// <summary>
    /// Gets the unary operator token type.
    /// </summary>
    public TokenType Operator { get; } = op;

    /// <summary>
    /// Gets the operand expression.
    /// </summary>
    public IExpression Operand { get; } = operand;

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUnaryExpression(this);
}