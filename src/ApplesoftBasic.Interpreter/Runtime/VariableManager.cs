// <copyright file="VariableManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

using Microsoft.Extensions.Logging;

/// <summary>
/// Default implementation of variable manager.
/// </summary>
public class VariableManager : IVariableManager
{
    private readonly Dictionary<string, BasicValue> _variables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, BasicArray> _arrays = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<VariableManager> _logger;

    public VariableManager(ILogger<VariableManager> logger)
    {
        _logger = logger;
    }

    public BasicValue GetVariable(string name)
    {
        // Normalize variable name (Applesoft only uses first 2 characters)
        string normalizedName = NormalizeVariableName(name);

        if (_variables.TryGetValue(normalizedName, out var value))
        {
            return value;
        }

        // Return default value based on type
        return IsStringVariable(name) ? BasicValue.Empty : BasicValue.Zero;
    }

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

        _variables[normalizedName] = value;
        _logger.LogTrace("Set variable {Name} = {Value}", normalizedName, value);
    }

    public BasicValue GetArrayElement(string name, int[] indices)
    {
        string normalizedName = NormalizeVariableName(name);

        // Auto-dimension if not exists (Applesoft behavior - default dimension 10)
        if (!_arrays.ContainsKey(normalizedName))
        {
            int[] defaultDims = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                defaultDims[i] = 10;
            }
            DimArray(name, defaultDims);
        }

        var array = _arrays[normalizedName];
        ValidateIndices(array, indices);

        return array.GetElement(indices);
    }

    public void SetArrayElement(string name, int[] indices, BasicValue value)
    {
        string normalizedName = NormalizeVariableName(name);

        // Auto-dimension if not exists
        if (!_arrays.ContainsKey(normalizedName))
        {
            int[] defaultDims = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                defaultDims[i] = Math.Max(10, indices[i]);
            }
            DimArray(name, defaultDims);
        }

        var array = _arrays[normalizedName];
        ValidateIndices(array, indices);

        // Type checking
        if (IsStringVariable(name) && !value.IsString)
        {
            throw new BasicRuntimeException("?TYPE MISMATCH ERROR");
        }

        array.SetElement(indices, value);
        _logger.LogTrace(
            "Set array {Name}({Indices}) = {Value}",
            normalizedName,
            string.Join(",", indices),
            value);
    }

    public void DimArray(string name, int[] dimensions)
    {
        string normalizedName = NormalizeVariableName(name);

        if (_arrays.ContainsKey(normalizedName))
        {
            throw new BasicRuntimeException("?REDIM'D ARRAY ERROR");
        }

        // Applesoft arrays are 0-based but DIM specifies the maximum index
        // So DIM A(10) creates an array with indices 0-10 (11 elements)
        int[] actualDims = new int[dimensions.Length];
        for (int i = 0; i < dimensions.Length; i++)
        {
            actualDims[i] = dimensions[i] + 1;
        }

        bool isString = IsStringVariable(name);
        _arrays[normalizedName] = new BasicArray(actualDims, isString);

        _logger.LogTrace(
            "Dimensioned array {Name}({Dims})",
            normalizedName,
            string.Join(",", dimensions));
    }

    public void Clear()
    {
        _variables.Clear();
        _arrays.Clear();
        _logger.LogDebug("Cleared all variables and arrays");
    }

    public bool VariableExists(string name)
    {
        return _variables.ContainsKey(NormalizeVariableName(name));
    }

    public bool ArrayExists(string name)
    {
        return _arrays.ContainsKey(NormalizeVariableName(name));
    }

    private static string NormalizeVariableName(string name)
    {
        // Applesoft BASIC only recognizes the first 2 characters of variable names
        // plus the type suffix ($ or %)
        string baseName = name.TrimEnd('$', '%');
        string suffix = "";

        if (name.EndsWith('$')) suffix = "$";
        else if (name.EndsWith('%')) suffix = "%";

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