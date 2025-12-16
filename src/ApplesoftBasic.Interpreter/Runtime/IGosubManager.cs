// <copyright file="IGosubManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Manages GOSUB/RETURN stack.
/// </summary>
public interface IGosubManager
{
    /// <summary>
    /// Pushes a return address onto the stack.
    /// </summary>
    void Push(GosubReturnAddress address);

    /// <summary>
    /// Pops and returns the last return address.
    /// </summary>
    /// <returns></returns>
    GosubReturnAddress Pop();

    /// <summary>
    /// Clears the stack.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the current stack depth.
    /// </summary>
    int Depth { get; }
}