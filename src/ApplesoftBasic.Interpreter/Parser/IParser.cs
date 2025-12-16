// <copyright file="IParser.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Parser;

using AST;

/// <summary>
/// Interface for the BASIC parser.
/// </summary>
public interface IParser
{
    /// <summary>
    /// Parses source code into an AST.
    /// </summary>
    /// <param name="source">The BASIC source code.</param>
    /// <returns>The program AST.</returns>
    ProgramNode Parse(string source);
}