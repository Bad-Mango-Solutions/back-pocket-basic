// <copyright file="ILoopManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Provides an interface for managing the state of FOR-NEXT loops in the Applesoft BASIC interpreter runtime.
/// </summary>
/// <remarks>
/// This interface is responsible for tracking and manipulating the state of active FOR-NEXT loops,
/// ensuring proper execution and management of loop constructs within the interpreter.
/// </remarks>
public interface ILoopManager
{
    /// <summary>
    /// Pushes a new FOR loop state onto the stack.
    /// </summary>
    /// <param name="state">
    /// The <see cref="ForLoopState"/> representing the state of the FOR loop to be added.
    /// </param>
    /// <remarks>
    /// If a loop for the same variable already exists, it will be removed before adding the new state.
    /// This ensures that only the most recent loop for a given variable is tracked.
    /// </remarks>
    void PushFor(ForLoopState state);

    /// <summary>
    /// Retrieves the state of a FOR loop associated with the specified variable.
    /// </summary>
    /// <param name="variable">The name of the variable associated with the desired FOR loop.</param>
    /// <returns>
    /// The <see cref="ForLoopState"/> object representing the state of the FOR loop if found;
    /// otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method searches the stack of active FOR loops for a loop associated with the specified variable.
    /// The search is case-insensitive.
    /// </remarks>
    ForLoopState? GetForLoop(string variable);

    /// <summary>
    /// Removes the most recently added FOR loop state from the stack or a specific FOR loop state
    /// associated with the given variable.
    /// </summary>
    /// <param name="variable">
    /// The name of the variable associated with the FOR loop to be removed. If <c>null</c> or empty,
    /// the most recently added FOR loop state is removed.
    /// </param>
    /// <returns>
    /// The <see cref="ForLoopState"/> that was removed from the stack, or <c>null</c> if no matching
    /// FOR loop state was found.
    /// </returns>
    /// <exception cref="BasicRuntimeException">
    /// Thrown if the stack is empty or if no matching FOR loop state is found for the specified variable.
    /// </exception>
    /// <remarks>
    /// If a variable is specified, the method searches for the corresponding FOR loop state in the stack.
    /// If found, it removes and returns it. If not found, the stack is restored to its original state,
    /// and an exception is thrown. If no variable is specified, the method simply removes and returns
    /// the most recently added FOR loop state.
    /// </remarks>
    ForLoopState? PopFor(string? variable);

    /// <summary>
    /// Clears all managed FOR-NEXT loop states.
    /// </summary>
    /// <remarks>
    /// This method removes all loop states currently managed by the implementation,
    /// effectively resetting the loop management system. It is typically used to
    /// ensure a clean state during program initialization, termination, or when
    /// executing a CLEAR statement in the BASIC program.
    /// </remarks>
    void Clear();
}