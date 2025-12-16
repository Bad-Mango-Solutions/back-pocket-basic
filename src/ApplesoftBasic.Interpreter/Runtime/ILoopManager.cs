// <copyright file="ILoopManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Manages FOR-NEXT loop state.
/// </summary>
public interface ILoopManager
{
    /// <summary>
    /// Pushes a new FOR loop onto the stack.
    /// </summary>
    void PushFor(ForLoopState state);

    /// <summary>
    /// Gets the current FOR loop for a variable.
    /// </summary>
    /// <returns></returns>
    ForLoopState? GetForLoop(string variable);

    /// <summary>
    /// Pops FOR loop(s) for NEXT.
    /// </summary>
    /// <returns></returns>
    ForLoopState? PopFor(string? variable);

    /// <summary>
    /// Clears all loops.
    /// </summary>
    void Clear();
}