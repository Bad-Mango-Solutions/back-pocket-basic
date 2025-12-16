// <copyright file="ArrayDeclaration.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Represents an array declaration in Applesoft BASIC.
/// </summary>
public class ArrayDeclaration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayDeclaration"/> class.
    /// </summary>
    /// <param name="name">The name of the array.</param>
    public ArrayDeclaration(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the array.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the list of dimension expressions for the array.
    /// </summary>
    public List<IExpression> Dimensions { get; } = new();
}
