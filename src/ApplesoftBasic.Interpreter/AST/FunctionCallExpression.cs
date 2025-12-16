// <copyright file="FunctionCallExpression.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

using Tokens;

/// <summary>
/// Built-in function call (e.g., SIN(X), LEN(A$)).
/// </summary>
public class FunctionCallExpression : IExpression
{
    public TokenType Function { get; }

    public List<IExpression> Arguments { get; } = new();

    public FunctionCallExpression(TokenType function)
    {
        Function = function;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFunctionCallExpression(this);
}