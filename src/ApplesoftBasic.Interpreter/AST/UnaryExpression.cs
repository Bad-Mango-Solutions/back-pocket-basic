// <copyright file="UnaryExpression.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

using Tokens;

/// <summary>
/// Unary operation (e.g., -X, NOT flag).
/// </summary>
public class UnaryExpression : IExpression
{
    public TokenType Operator { get; }

    public IExpression Operand { get; }

    public UnaryExpression(TokenType op, IExpression operand)
    {
        Operator = op;
        Operand = operand;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUnaryExpression(this);
}