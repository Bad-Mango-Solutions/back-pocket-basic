// <copyright file="IGosubManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

/// <summary>
/// Defines the contract for managing the GOSUB/RETURN stack in the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// This interface provides methods for pushing, popping, and clearing return addresses associated with GOSUB statements.
/// Implementations of this interface ensure proper stack management and handle invalid stack operations appropriately.
/// </remarks>
public interface IGosubManager
{
    /// <summary>
    /// Gets the current depth of the GOSUB/RETURN stack.
    /// </summary>
    /// <value>
    /// An integer representing the number of return addresses currently stored in the stack.
    /// </value>
    /// <remarks>
    /// This property indicates how many GOSUB statements are currently active and awaiting RETURN.
    /// It is useful for debugging and ensuring proper stack management.
    /// </remarks>
    int Depth { get; }

    /// <summary>
    /// Pushes a return address onto the GOSUB/RETURN stack.
    /// </summary>
    /// <param name="address">
    /// The <see cref="GosubReturnAddress"/> representing the return point for a GOSUB statement.
    /// </param>
    /// <remarks>
    /// This method is invoked when a GOSUB statement is executed to store the return address.
    /// Ensure that the stack is managed properly to avoid exceeding memory limits or causing stack corruption.
    /// </remarks>
    void Push(GosubReturnAddress address);

    /// <summary>
    /// Removes and returns the most recently pushed <see cref="GosubReturnAddress"/> from the GOSUB/RETURN stack.
    /// </summary>
    /// <returns>The <see cref="GosubReturnAddress"/> at the top of the stack.</returns>
    /// <exception cref="BasicRuntimeException">
    /// Thrown when the stack is empty, indicating a "?RETURN WITHOUT GOSUB ERROR".
    /// </exception>
    GosubReturnAddress Pop();

    /// <summary>
    /// Clears all entries in the GOSUB/RETURN stack.
    /// </summary>
    /// <remarks>
    /// This method removes all stored return addresses, effectively resetting the state of the GOSUB manager.
    /// It is commonly used to ensure a clean state before starting a new sequence of GOSUB operations.
    /// </remarks>
    void Clear();
}