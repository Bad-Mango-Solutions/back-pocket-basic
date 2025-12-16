// <copyright file="UserFunctionExpression.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// User-defined function call (FN name).
/// </summary>
public class UserFunctionExpression : IExpression
{
    public string FunctionName { get; }

    public IExpression Argument { get; }

    public UserFunctionExpression(string functionName, IExpression argument)
    {
        FunctionName = functionName;
        Argument = argument;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUserFunctionExpression(this);
}