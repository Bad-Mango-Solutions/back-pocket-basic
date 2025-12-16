// <copyright file="IFunctionManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

using AST;

/// <summary>
/// Manages user-defined functions (DEF FN).
/// </summary>
public interface IFunctionManager
{
    /// <summary>
    /// Defines a user function.
    /// </summary>
    void DefineFunction(string name, string parameter, IExpression body);

    /// <summary>
    /// Gets a user-defined function.
    /// </summary>
    /// <returns></returns>
    UserFunction? GetFunction(string name);

    /// <summary>
    /// Checks if a function is defined.
    /// </summary>
    /// <returns></returns>
    bool FunctionExists(string name);

    /// <summary>
    /// Clears all user-defined functions.
    /// </summary>
    void Clear();
}