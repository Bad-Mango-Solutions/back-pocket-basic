// <copyright file="UserFunction.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

using AST;

/// <summary>
/// Represents a user-defined function.
/// </summary>
public class UserFunction
{
    public string Name { get; }

    public string Parameter { get; }

    public IExpression Body { get; }

    public UserFunction(string name, string parameter, IExpression body)
    {
        Name = name;
        Parameter = parameter;
        Body = body;
    }
}