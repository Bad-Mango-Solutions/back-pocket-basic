// <copyright file="ProgramNode.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Represents the root node of the Abstract Syntax Tree (AST) for an Applesoft BASIC program.
/// </summary>
/// <remarks>
/// This class serves as a container for the entire program, including its lines and associated data values.
/// It implements the <see cref="IAstNode"/> interface, allowing it to be visited by an <see cref="IAstVisitor{T}"/>.
/// </remarks>
public class ProgramNode : IAstNode
{
    /// <summary>
    /// Gets the collection of lines that make up the Applesoft BASIC program.
    /// </summary>
    /// <remarks>
    /// Each line in the program is represented as a <see cref="LineNode"/>, which contains the line number
    /// and the statements associated with that line. The lines are stored in the order they are parsed
    /// and can be sorted by line number during parsing.
    /// </remarks>
    /// <value>
    /// A list of <see cref="LineNode"/> objects representing the lines of the program.
    /// </value>
    public List<LineNode> Lines { get; } = [];

    /// <summary>
    /// Gets the collection of data values defined in the Applesoft BASIC program.
    /// </summary>
    /// <remarks>
    /// This property stores the values specified in DATA statements within the program.
    /// These values are used during program execution and can include numbers, strings, or other objects.
    /// </remarks>
    public List<object> DataValues { get; } = [];

    /// <summary>
    /// Accepts a visitor that implements the <see cref="IAstVisitor{T}"/> interface and allows it to process this <see cref="ProgramNode"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that will process this node.</param>
    /// <returns>The result of the visitor's processing of this node.</returns>
    /// <remarks>
    /// This method enables the implementation of the Visitor design pattern, allowing external operations to be performed
    /// on the <see cref="ProgramNode"/> without modifying its structure.
    /// </remarks>
    public T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitProgram(this);
}