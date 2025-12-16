// <copyright file="VariableExpression.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Variable reference.
/// </summary>
public class VariableExpression : IExpression
{
    public string Name { get; }

    public bool IsString => Name.EndsWith('$');

    public bool IsInteger => Name.EndsWith('%');

    public VariableExpression(string name)
    {
        Name = name;
    }

    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVariableExpression(this);
}