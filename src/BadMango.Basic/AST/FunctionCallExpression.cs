// <copyright file="FunctionCallExpression.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

using Tokens;

/// <summary>
/// Built-in function call (e.g., SIN(X), LEN(A$)).
/// </summary>
public class FunctionCallExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionCallExpression"/> class.
    /// </summary>
    /// <param name="function">The function token type.</param>
    public FunctionCallExpression(TokenType function)
    {
        Function = function;
    }

    /// <summary>
    /// Gets the function token type.
    /// </summary>
    public TokenType Function { get; }

    /// <summary>
    /// Gets the list of argument expressions.
    /// </summary>
    public List<IExpression> Arguments { get; } = [];

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFunctionCallExpression(this);
}