// <copyright file="IFunctionManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

using AST;

/// <summary>
/// Represents a contract for managing user-defined functions (DEF FN) within the Applesoft BASIC interpreter runtime.
/// </summary>
/// <remarks>
/// This interface provides methods to define, retrieve, and manage user-defined functions. It ensures that function names
/// are normalized for consistent storage and lookup. Implementations of this interface are responsible for maintaining
/// the internal collection of functions and providing efficient access to them.
/// </remarks>
public interface IFunctionManager
{
    /// <summary>
    /// Defines a user-defined function with the specified name, parameter, and body.
    /// </summary>
    /// <param name="name">The name of the function to define. Only the first two characters are used for normalization.</param>
    /// <param name="parameter">The name of the parameter for the function.</param>
    /// <param name="body">The body of the function, represented as an <see cref="IExpression"/>.</param>
    /// <remarks>
    /// This method normalizes the function name to ensure consistency and stores the function in the internal collection.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/>, <paramref name="parameter"/>, or <paramref name="body"/> is <c>null</c>.
    /// </exception>
    void DefineFunction(string name, string parameter, IExpression body);

    /// <summary>
    /// Retrieves a user-defined function by its name.
    /// </summary>
    /// <param name="name">The name of the function to retrieve.</param>
    /// <returns>
    /// The <see cref="UserFunction"/> instance if a function with the specified name exists; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// The function name is normalized before lookup to ensure case-insensitive matching.
    /// </remarks>
    UserFunction? GetFunction(string name);

    /// <summary>
    /// Determines whether a user-defined function with the specified name exists.
    /// </summary>
    /// <param name="name">The name of the function to check for existence.</param>
    /// <returns>
    /// <c>true</c> if a function with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The function name is normalized before checking to ensure case-insensitive matching.
    /// </remarks>
    bool FunctionExists(string name);

    /// <summary>
    /// Clears all user-defined functions managed by the function manager.
    /// </summary>
    /// <remarks>
    /// This method removes all previously defined functions, resetting the function manager
    /// to its initial state. Use this method to ensure no residual user-defined functions
    /// remain during program execution or reinitialization.
    /// </remarks>
    void Clear();
}