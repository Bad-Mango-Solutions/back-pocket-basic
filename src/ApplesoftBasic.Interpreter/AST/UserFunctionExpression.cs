// <copyright file="UserFunctionExpression.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// User-defined function call (FN name).
/// </summary>
public class UserFunctionExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserFunctionExpression"/> class.
    /// </summary>
    /// <param name="functionName">The name of the user-defined function.</param>
    /// <param name="argument">The argument expression for the function.</param>
    public UserFunctionExpression(string functionName, IExpression argument)
    {
        FunctionName = functionName;
        Argument = argument;
    }

    /// <summary>
    /// Gets the name of the user-defined function.
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// Gets the argument expression for the function.
    /// </summary>
    public IExpression Argument { get; }

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUserFunctionExpression(this);
}