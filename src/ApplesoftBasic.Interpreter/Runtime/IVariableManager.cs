// <copyright file="IVariableManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Provides an interface for managing variables and arrays in the Applesoft BASIC runtime environment.
/// </summary>
public interface IVariableManager
{
    /// <summary>
    /// Retrieves the value of a variable by its name.
    /// </summary>
    /// <param name="name">The name of the variable to retrieve. Only the first two characters of the name are significant in Applesoft BASIC.</param>
    /// <returns>
    /// The value of the variable as a <see cref="BasicValue"/>.
    /// If the variable does not exist, a default value is returned:
    /// <see cref="BasicValue.Empty"/> for string variables or <see cref="BasicValue.Zero"/> for numeric variables.
    /// </returns>
    /// <remarks>
    /// Variable names in Applesoft BASIC are case-insensitive and are normalized to use only the first two characters.
    /// </remarks>
    BasicValue GetVariable(string name);

    /// <summary>
    /// Assigns a value to a variable in the Applesoft BASIC runtime environment.
    /// </summary>
    /// <param name="name">The name of the variable to set. Variable names are case-insensitive and may include type suffixes (e.g., "$" for strings).</param>
    /// <param name="value">The <see cref="BasicValue"/> to assign to the variable. The value type must match the variable's expected type.</param>
    /// <remarks>
    /// If the variable does not exist, it will be created. Type mismatches will result in an error.
    /// </remarks>
    void SetVariable(string name, BasicValue value);

    /// <summary>
    /// Retrieves the value of an element from a specified array in the Applesoft BASIC runtime environment.
    /// </summary>
    /// <param name="name">The name of the array from which to retrieve the element.</param>
    /// <param name="indices">An array of integers representing the indices of the element to retrieve.</param>
    /// <returns>The <see cref="BasicValue"/> representing the value of the specified array element.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the specified array does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided indices are invalid for the specified array.</exception>
    BasicValue GetArrayElement(string name, int[] indices);

    /// <summary>
    /// Sets the value of a specific element in an array variable.
    /// </summary>
    /// <param name="name">The name of the array variable.</param>
    /// <param name="indices">The indices specifying the position of the element within the array.</param>
    /// <param name="value">The <see cref="BasicValue"/> to assign to the specified array element.</param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the specified array does not exist and automatic dimensioning is not enabled.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the provided indices are invalid for the specified array or if the value type
    /// does not match the expected type for the array.
    /// </exception>
    /// <remarks>
    /// If the array does not exist, it may be automatically dimensioned depending on the implementation.
    /// The indices must match the dimensionality of the array, and the value must conform to the array's type.
    /// </remarks>
    void SetArrayElement(string name, int[] indices, BasicValue value);

    /// <summary>
    /// Declares a new array with the specified name and dimensions in the Applesoft BASIC runtime environment.
    /// </summary>
    /// <param name="name">
    /// The name of the array to be declared. The name is case-insensitive.
    /// </param>
    /// <param name="dimensions">
    /// An array of integers specifying the maximum indices for each dimension of the array.
    /// The resulting array will have indices ranging from 0 to the specified maximum for each dimension.
    /// </param>
    /// <exception cref="BasicRuntimeException">
    /// Thrown if an array with the specified name already exists.
    /// </exception>
    /// <remarks>
    /// Applesoft BASIC arrays are 0-based, but the <paramref name="dimensions"/> parameter specifies the maximum index for each dimension.
    /// For example, calling <c>DimArray("A", new[] { 10 })</c> creates an array with indices ranging from 0 to 10 (inclusive).
    /// </remarks>
    void DimArray(string name, int[] dimensions);

    /// <summary>
    /// Clears all variables and arrays managed by the <see cref="IVariableManager"/>.
    /// </summary>
    /// <remarks>
    /// This method removes all stored variables and arrays, effectively resetting the state of the
    /// <see cref="IVariableManager"/> implementation.
    /// </remarks>
    void Clear();

    /// <summary>
    /// Determines whether a variable with the specified name exists in the Applesoft BASIC runtime environment.
    /// </summary>
    /// <param name="name">The name of the variable to check for existence.</param>
    /// <returns>
    /// <c>true</c> if a variable with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The variable name is normalized before checking for existence to ensure compatibility
    /// with Applesoft BASIC's case-insensitive naming conventions.
    /// </remarks>
    bool VariableExists(string name);

    /// <summary>
    /// Determines whether an array with the specified name exists in the Applesoft BASIC runtime environment.
    /// </summary>
    /// <param name="name">The name of the array to check for existence. Array names are case-insensitive and normalized according to Applesoft BASIC conventions.</param>
    /// <returns>
    /// <c>true</c> if an array with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method ensures compatibility with Applesoft BASIC's naming conventions by performing a case-insensitive check.
    /// </remarks>
    bool ArrayExists(string name);
}