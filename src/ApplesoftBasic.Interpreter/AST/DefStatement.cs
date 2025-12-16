// <copyright file="DefStatement.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// DEF FN statement for user-defined functions.
/// </summary>
public class DefStatement : IStatement
{
    public string FunctionName { get; }

    public string Parameter { get; }

    public IExpression Body { get; }

    public DefStatement(string functionName, string parameter, IExpression body)
    {
        FunctionName = functionName;
        Parameter = parameter;
        Body = body;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDefStatement(this);
}