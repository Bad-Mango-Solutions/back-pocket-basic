// <copyright file="BasicArray.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Represents a BASIC array.
/// </summary>
internal class BasicArray
{
    public int[] Dimensions { get; }

    private readonly BasicValue[] _elements;
    private readonly bool _isStringArray;

    public BasicArray(int[] dimensions, bool isStringArray)
    {
        Dimensions = dimensions;
        _isStringArray = isStringArray;

        int totalElements = 1;
        foreach (int dim in dimensions)
        {
            totalElements *= dim;
        }

        _elements = new BasicValue[totalElements];

        // Initialize with default values
        BasicValue defaultValue = isStringArray ? BasicValue.Empty : BasicValue.Zero;
        for (int i = 0; i < _elements.Length; i++)
        {
            _elements[i] = defaultValue;
        }
    }

    public BasicValue GetElement(int[] indices)
    {
        int index = CalculateIndex(indices);
        return _elements[index];
    }

    public void SetElement(int[] indices, BasicValue value)
    {
        int index = CalculateIndex(indices);
        _elements[index] = value;
    }

    private int CalculateIndex(int[] indices)
    {
        int index = 0;
        int multiplier = 1;

        for (int i = indices.Length - 1; i >= 0; i--)
        {
            index += indices[i] * multiplier;
            multiplier *= Dimensions[i];
        }

        return index;
    }
}