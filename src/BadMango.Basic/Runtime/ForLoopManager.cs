// <copyright file="ForLoopManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

/// <summary>
/// Manages the execution state of loops in the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// This class provides functionality to handle the lifecycle of loops, including
/// adding new loops, retrieving existing loops, and clearing loop states. It ensures
/// proper nesting and error handling for loop-related operations.
/// </remarks>
public class ForLoopManager : ILoopManager
{
    private readonly Stack<ForLoopState> forStack = new();

    /// <summary>
    /// Adds a new FOR loop state to the stack, replacing any existing loop
    /// for the same variable.
    /// </summary>
    /// <param name="state">
    /// The <see cref="ForLoopState"/> representing the state of the FOR loop to be added.
    /// </param>
    /// <remarks>
    /// If a loop for the same variable already exists, it is removed before adding the new state.
    /// This ensures that only the most recent loop for a given variable is tracked.
    /// </remarks>
    public void PushFor(ForLoopState state)
    {
        // Remove any existing loop for the same variable
        var temp = new Stack<ForLoopState>();
        while (forStack.Count > 0)
        {
            var top = forStack.Pop();
            if (top.Variable.Equals(state.Variable, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            temp.Push(top);
        }

        // Restore other loops
        while (temp.Count > 0)
        {
            forStack.Push(temp.Pop());
        }

        forStack.Push(state);
    }

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
    public ForLoopState? GetForLoop(string variable)
    {
        foreach (var state in forStack)
        {
            if (state.Variable.Equals(variable, StringComparison.OrdinalIgnoreCase))
            {
                return state;
            }
        }

        return null;
    }

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
    public ForLoopState? PopFor(string? variable)
    {
        if (forStack.Count == 0)
        {
            throw new BasicRuntimeException("?NEXT WITHOUT FOR ERROR");
        }

        if (string.IsNullOrEmpty(variable))
        {
            return forStack.Pop();
        }

        // Pop until we find the matching variable
        var temp = new Stack<ForLoopState>();
        ForLoopState? found = null;

        while (forStack.Count > 0)
        {
            var top = forStack.Pop();
            if (top.Variable.Equals(variable, StringComparison.OrdinalIgnoreCase))
            {
                found = top;
                break;
            }

            temp.Push(top);
        }

        if (found == null)
        {
            // Restore stack and throw error
            while (temp.Count > 0)
            {
                forStack.Push(temp.Pop());
            }

            throw new BasicRuntimeException("?NEXT WITHOUT FOR ERROR");
        }

        // Don't restore loops that were inside this one
        return found;
    }

    /// <summary>
    /// Clears all FOR-NEXT loop states managed by this instance.
    /// </summary>
    /// <remarks>
    /// This method removes all stored loop states, effectively resetting the loop management system.
    /// It is typically used to ensure a clean state when restarting or terminating execution.
    /// </remarks>
    public void Clear()
    {
        forStack.Clear();
    }
}