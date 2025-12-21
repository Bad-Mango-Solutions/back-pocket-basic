// <copyright file="BasicArray.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

/// <summary>
/// Represents a BASIC array.
/// </summary>
internal class BasicArray
{
    private readonly BasicValue[] elements;
    private readonly bool isStringArray;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicArray"/> class with the specified dimensions and type.
    /// </summary>
    /// <param name="dimensions">
    /// An array of integers representing the dimensions of the array. Each value specifies the size of the array
    /// in the corresponding dimension.
    /// </param>
    /// <param name="isStringArray">
    /// A boolean value indicating whether the array is a string array (<c>true</c>) or a numeric array (<c>false</c>).
    /// </param>
    /// <remarks>
    /// The array is initialized with default values. For string arrays, the default value is <see cref="BasicValue.Empty"/>.
    /// For numeric arrays, the default value is <see cref="BasicValue.Zero"/>.
    /// </remarks>
    public BasicArray(int[] dimensions, bool isStringArray)
    {
        Dimensions = dimensions;
        this.isStringArray = isStringArray;

        int totalElements = 1;
        foreach (int dim in dimensions)
        {
            totalElements *= dim;
        }

        elements = new BasicValue[totalElements];

        // Initialize with default values
        BasicValue defaultValue = isStringArray ? BasicValue.Empty : BasicValue.Zero;
        for (int i = 0; i < elements.Length; i++)
        {
            elements[i] = defaultValue;
        }
    }

    /// <summary>
    /// Gets the dimensions of the array. Each element in the returned array represents
    /// the size of the corresponding dimension of the BASIC array.
    /// </summary>
    public int[] Dimensions { get; }

    /// <summary>
    /// Retrieves an element from the array at the specified indices.
    /// </summary>
    /// <param name="indices">
    /// An array of integers representing the indices of the element to retrieve.
    /// The length of the <paramref name="indices"/> array must match the number of dimensions of the array.
    /// </param>
    /// <returns>
    /// The <see cref="BasicValue"/> stored at the specified indices.
    /// </returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if any of the specified indices are out of the bounds of the array dimensions.
    /// </exception>
    public BasicValue GetElement(int[] indices)
    {
        int index = CalculateIndex(indices);
        return elements[index];
    }

    /// <summary>
    /// Sets the value of an element in the array at the specified indices.
    /// </summary>
    /// <param name="indices">
    /// An array of integers representing the indices of the element to set.
    /// Each index corresponds to a dimension of the array.
    /// </param>
    /// <param name="value">
    /// The <see cref="BasicValue"/> to assign to the specified element.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the provided indices are out of bounds for the array dimensions.
    /// </exception>
    public void SetElement(int[] indices, BasicValue value)
    {
        int index = CalculateIndex(indices);
        elements[index] = value;
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