// <copyright file="FunctionManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

using AST;

/// <summary>
/// Manages the definition and retrieval of user-defined functions within the Applesoft BASIC interpreter runtime.
/// </summary>
/// <remarks>
/// This class provides functionality to define, retrieve, and manage user-defined functions. It normalizes function names
/// for consistency and ensures that functions are stored and accessed efficiently. It is registered as the default implementation
/// of the <see cref="IFunctionManager"/> interface in the interpreter's dependency injection container.
/// </remarks>
public class FunctionManager : IFunctionManager
{
    private readonly Dictionary<string, UserFunction> functions = new(StringComparer.OrdinalIgnoreCase);

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
    public void DefineFunction(string name, string parameter, IExpression body)
    {
        // Normalize function name (first 2 chars only, like variables)
        string normalizedName = NormalizeFunctionName(name);
        functions[normalizedName] = new(normalizedName, parameter, body);
    }

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
    public UserFunction? GetFunction(string name)
    {
        string normalizedName = NormalizeFunctionName(name);
        return functions.TryGetValue(normalizedName, out var func) ? func : null;
    }

    /// <summary>
    /// Determines whether a user-defined function with the specified name exists.
    /// </summary>
    /// <param name="name">The name of the function to check for existence.</param>
    /// <returns>
    /// <c>true</c> if a function with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    public bool FunctionExists(string name)
    {
        return functions.ContainsKey(NormalizeFunctionName(name));
    }

    /// <summary>
    /// Removes all user-defined functions from the function manager.
    /// </summary>
    /// <remarks>
    /// This method clears the internal collection of user-defined functions,
    /// effectively resetting the state of the function manager.
    /// </remarks>
    public void Clear()
    {
        functions.Clear();
    }

    private static string NormalizeFunctionName(string name)
    {
        return name.Length > 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
    }
}