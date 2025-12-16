// <copyright file="BinaryExpression.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

using Tokens;

/// <summary>
/// Binary operation (e.g., 1 + 2, A * B).
/// </summary>
public class BinaryExpression : IExpression
{
    public IExpression Left { get; }

    public TokenType Operator { get; }

    public IExpression Right { get; }

    public BinaryExpression(IExpression left, TokenType op, IExpression right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBinaryExpression(this);
}