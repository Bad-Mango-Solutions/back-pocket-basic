// <copyright file="VariableManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

using Microsoft.Extensions.Logging;

/// <summary>
/// Manages variables and arrays in the Applesoft BASIC runtime environment.
/// </summary>
/// <remarks>
/// This class provides functionality for handling variables and arrays, including
/// operations such as retrieving, setting, and clearing variables, as well as managing
/// array dimensions and elements. It ensures compatibility with Applesoft BASIC's
/// behavior, such as name normalization and type checking.
/// </remarks>
public class VariableManager : IVariableManager
{
    private readonly Dictionary<string, BasicValue> variables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BasicArray> arrays = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<VariableManager> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableManager"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger instance used for logging operations within the variable manager.
    /// </param>
    public VariableManager(ILogger<VariableManager> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Retrieves the value of a variable by its name.
    /// </summary>
    /// <param name="name">The name of the variable to retrieve. Applesoft BASIC variable names are case-insensitive and only the first two characters are significant.</param>
    /// <returns>
    /// The value of the variable if it exists; otherwise, a default value.
    /// For string variables, the default value is <see cref="BasicValue.Empty"/>.
    /// For numeric variables, the default value is <see cref="BasicValue.Zero"/>.
    /// </returns>
    /// <remarks>
    /// This method normalizes the variable name to ensure compatibility with Applesoft BASIC's naming conventions.
    /// If the variable does not exist, a default value is returned based on the variable type.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is <c>null</c>.</exception>
    public BasicValue GetVariable(string name)
    {
        // Normalize variable name (Applesoft only uses first 2 characters)
        string normalizedName = NormalizeVariableName(name);

        if (variables.TryGetValue(normalizedName, out var value))
        {
            return value;
        }

        // Return default value based on type
        return IsStringVariable(name) ? BasicValue.Empty : BasicValue.Zero;
    }

    /// <summary>
    /// Sets the value of a variable in the Applesoft BASIC runtime environment.
    /// </summary>
    /// <param name="name">The name of the variable to set. Variable names are case-insensitive and may include type suffixes such as '$' for strings or '%' for integers.</param>
    /// <param name="value">The <see cref="BasicValue"/> to assign to the variable. The type of the value must match the type implied by the variable name.</param>
    /// <exception cref="BasicRuntimeException">
    /// Thrown if there is a type mismatch between the variable name and the provided value.
    /// </exception>
    /// <remarks>
    /// This method normalizes the variable name, performs type checking to ensure compatibility
    /// between the variable name and the value, and then updates the variable's value in the internal storage.
    /// </remarks>
    public void SetVariable(string name, BasicValue value)
    {
        string normalizedName = NormalizeVariableName(name);

        // Type checking
        if (IsStringVariable(name) && !value.IsString)
        {
            throw new BasicRuntimeException("?TYPE MISMATCH ERROR");
        }

        if (IsIntegerVariable(name) && value.IsString)
        {
            throw new BasicRuntimeException("?TYPE MISMATCH ERROR");
        }

        variables[normalizedName] = value;
        logger.LogTrace("Set variable {Name} = {Value}", normalizedName, value);
    }

    /// <summary>
    /// Retrieves an element from a multidimensional array by its name and indices.
    /// </summary>
    /// <param name="name">The name of the array from which to retrieve the element.</param>
    /// <param name="indices">
    /// An array of integers representing the indices of the element to retrieve.
    /// The number of indices must match the dimensions of the array.
    /// </param>
    /// <returns>
    /// The <see cref="BasicValue"/> stored at the specified indices in the array.
    /// </returns>
    /// <remarks>
    /// If the array with the specified name does not exist, it will be automatically
    /// created with default dimensions of size 10 for each dimension, following Applesoft BASIC behavior.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown if the provided indices are invalid for the array's dimensions.
    /// </exception>
    public BasicValue GetArrayElement(string name, int[] indices)
    {
        string normalizedName = NormalizeVariableName(name);

        // Auto-dimension if not exists (Applesoft behavior - default dimension 10)
        if (!arrays.ContainsKey(normalizedName))
        {
            int[] defaultDims = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                defaultDims[i] = 10;
            }

            DimArray(name, defaultDims);
        }

        var array = arrays[normalizedName];
        ValidateIndices(array, indices);

        return array.GetElement(indices);
    }

    /// <summary>
    /// Sets the value of an element in a specified array at the given indices.
    /// </summary>
    /// <param name="name">The name of the array to modify. This name is case-insensitive.</param>
    /// <param name="indices">An array of integers representing the indices of the element to set.</param>
    /// <param name="value">The <see cref="BasicValue"/> to assign to the specified element.</param>
    /// <exception cref="BasicRuntimeException">
    /// Thrown if the array does not exist and cannot be auto-dimensioned,
    /// or if the value type does not match the expected type for the array.
    /// </exception>
    /// <remarks>
    /// If the array does not exist, it will be automatically dimensioned with default sizes
    /// based on the provided indices. The method also validates the indices and ensures
    /// type compatibility between the array and the value.
    /// </remarks>
    public void SetArrayElement(string name, int[] indices, BasicValue value)
    {
        string normalizedName = NormalizeVariableName(name);

        // Auto-dimension if not exists
        if (!arrays.ContainsKey(normalizedName))
        {
            int[] defaultDims = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                defaultDims[i] = Math.Max(10, indices[i]);
            }

            DimArray(name, defaultDims);
        }

        var array = arrays[normalizedName];
        ValidateIndices(array, indices);

        // Type checking
        if (IsStringVariable(name) && !value.IsString)
        {
            throw new BasicRuntimeException("?TYPE MISMATCH ERROR");
        }

        array.SetElement(indices, value);
        logger.LogTrace(
            "Set array {Name}({Indices}) = {Value}",
            normalizedName,
            string.Join(",", indices),
            value);
    }

    /// <summary>
    /// Dimensions a new array with the specified name and dimensions.
    /// </summary>
    /// <param name="name">The name of the array to be created. The name is case-insensitive.</param>
    /// <param name="dimensions">
    /// An array of integers specifying the maximum indices for each dimension of the array.
    /// The resulting array will have indices ranging from 0 to the specified maximum for each dimension.
    /// </param>
    /// <exception cref="BasicRuntimeException">
    /// Thrown if an array with the specified name already exists.
    /// </exception>
    /// <remarks>
    /// Applesoft BASIC arrays are 0-based, but the <c>dimensions</c> parameter specifies the maximum index for each dimension.
    /// For example, calling <c>DimArray("A", new[] { 10 })</c> creates an array with indices ranging from 0 to 10 (inclusive).
    /// </remarks>
    public void DimArray(string name, int[] dimensions)
    {
        string normalizedName = NormalizeVariableName(name);

        if (arrays.ContainsKey(normalizedName))
        {
            throw new BasicRuntimeException("?REDIM'DP ARRAY ERROR");
        }

        // Applesoft arrays are 0-based but DIM specifies the maximum index
        // So DIM A(10) creates an array with indices 0-10 (11 elements)
        int[] actualDims = new int[dimensions.Length];
        for (int i = 0; i < dimensions.Length; i++)
        {
            actualDims[i] = dimensions[i] + 1;
        }

        bool isString = IsStringVariable(name);
        arrays[normalizedName] = new(actualDims, isString);

        logger.LogTrace(
            "Dimensioned array {Name}({Dims})",
            normalizedName,
            string.Join(",", dimensions));
    }

    /// <summary>
    /// Clears all variables and arrays managed by the <see cref="VariableManager"/>.
    /// </summary>
    /// <remarks>
    /// This method removes all stored variables and arrays, effectively resetting the state of the
    /// <see cref="VariableManager"/>. It also logs a debug message indicating that the operation was performed.
    /// </remarks>
    public void Clear()
    {
        variables.Clear();
        arrays.Clear();
        logger.LogDebug("Cleared all variables and arrays");
    }

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
    public bool VariableExists(string name)
    {
        return variables.ContainsKey(NormalizeVariableName(name));
    }

    /// <summary>
    /// Determines whether an array with the specified name exists in the Applesoft BASIC runtime environment.
    /// </summary>
    /// <param name="name">The name of the array to check for existence.</param>
    /// <returns>
    /// <c>true</c> if an array with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The method performs a case-insensitive check for the existence of the array,
    /// ensuring compatibility with Applesoft BASIC's naming conventions.
    /// </remarks>
    public bool ArrayExists(string name)
    {
        return arrays.ContainsKey(NormalizeVariableName(name));
    }

    private static string NormalizeVariableName(string name)
    {
        // Applesoft BASIC only recognizes the first 2 characters of variable names
        // plus the type suffix ($ or %)
        string baseName = name.TrimEnd('$', '%');
        string suffix = string.Empty;

        if (name.EndsWith('$'))
        {
            suffix = "$";
        }
        else if (name.EndsWith('%'))
        {
            suffix = "%";
        }

        if (baseName.Length > 2)
        {
            baseName = baseName[..2];
        }

        return baseName.ToUpperInvariant() + suffix;
    }

    private static bool IsStringVariable(string name) => name.EndsWith('$');

    private static bool IsIntegerVariable(string name) => name.EndsWith('%');

    private static void ValidateIndices(BasicArray array, int[] indices)
    {
        if (indices.Length != array.Dimensions.Length)
        {
            throw new BasicRuntimeException("?BAD SUBSCRIPT ERROR");
        }

        for (int i = 0; i < indices.Length; i++)
        {
            if (indices[i] < 0 || indices[i] >= array.Dimensions[i])
            {
                throw new BasicRuntimeException("?BAD SUBSCRIPT ERROR");
            }
        }
    }
}