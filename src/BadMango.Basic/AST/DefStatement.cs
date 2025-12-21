// <copyright file="DefStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// DEF FN statement for user-defined functions.
/// </summary>
public class DefStatement(string functionName, string parameter, IExpression body) : IStatement
{
    /// <summary>
    /// Gets the name of the user-defined function.
    /// </summary>
    public string FunctionName { get; } = functionName;

    /// <summary>
    /// Gets the parameter name for the user-defined function.
    /// </summary>
    public string Parameter { get; } = parameter;

    /// <summary>
    /// Gets the body expression of the user-defined function.
    /// </summary>
    public IExpression Body { get; } = body;

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDefStatement(this);
}