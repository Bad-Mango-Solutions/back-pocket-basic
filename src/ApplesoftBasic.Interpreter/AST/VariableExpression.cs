// <copyright file="VariableExpression.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Variable reference.
/// </summary>
public class VariableExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VariableExpression"/> class.
    /// </summary>
    /// <param name="name">The variable name.</param>
    public VariableExpression(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the variable name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the variable is a string.
    /// </summary>
    public bool IsString => Name.EndsWith('$');

    /// <summary>
    /// Gets a value indicating whether the variable is an integer.
    /// </summary>
    public bool IsInteger => Name.EndsWith('%');

    /// <inheritdoc/>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVariableExpression(this);
}