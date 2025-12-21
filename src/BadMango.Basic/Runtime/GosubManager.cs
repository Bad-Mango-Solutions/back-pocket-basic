// <copyright file="GosubManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

/// <summary>
/// Manages the GOSUB stack for the Applesoft BASIC interpreter, allowing for the storage and retrieval
/// of return addresses during the execution of GOSUB statements.
/// </summary>
/// <remarks>
/// This class provides functionality to push, pop, and clear return addresses associated with GOSUB statements.
/// It ensures proper stack management and throws appropriate exceptions when stack operations are invalid.
/// </remarks>
public class GosubManager : IGosubManager
{
    private readonly Stack<GosubReturnAddress> stack = new();

    /// <summary>
    /// Gets the current depth of the GOSUB/RETURN stack.
    /// </summary>
    /// <value>
    /// The number of return addresses currently stored in the stack.
    /// </value>
    public int Depth => stack.Count;

    /// <summary>
    /// Pushes a GOSUB return address onto the stack.
    /// </summary>
    /// <param name="address">
    /// The <see cref="GosubReturnAddress"/> to be pushed onto the stack. Represents the return point for a GOSUB statement.
    /// </param>
    /// <remarks>
    /// This method is used to store the return address when a GOSUB statement is executed.
    /// Ensure that the stack does not exceed the memory limits of the application.
    /// </remarks>
    public void Push(GosubReturnAddress address)
    {
        stack.Push(address);
    }

    /// <summary>
    /// Removes and returns the most recently pushed <see cref="GosubReturnAddress"/> from the stack.
    /// </summary>
    /// <returns>The <see cref="GosubReturnAddress"/> at the top of the stack.</returns>
    /// <exception cref="BasicRuntimeException">
    /// Thrown when attempting to pop an address from an empty stack, indicating a "?RETURN WITHOUT GOSUB ERROR".
    /// </exception>
    public GosubReturnAddress Pop()
    {
        return stack.Count == 0 ? throw new BasicRuntimeException("?RETURN WITHOUT GOSUB ERROR") : stack.Pop();
    }

    /// <summary>
    /// Clears the GOSUB/RETURN stack, removing all stored return addresses.
    /// </summary>
    /// <remarks>
    /// This method resets the state of the GOSUB manager by clearing all entries in the stack.
    /// It is typically used to ensure a clean state before starting a new sequence of GOSUB operations.
    /// </remarks>
    public void Clear()
    {
        stack.Clear();
    }
}