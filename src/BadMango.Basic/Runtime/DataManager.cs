// <copyright file="DataManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

/// <summary>
/// Default implementation of data manager.
/// </summary>
public class DataManager : IDataManager
{
    private List<object> dataValues = [];
    private int dataPointer;

    /// <summary>
    /// Initializes the data manager with a specified list of data values.
    /// </summary>
    /// <param name="dataValuesList">
    /// A list of objects representing the data values to be managed.
    /// </param>
    /// <remarks>
    /// This method resets the internal state of the data manager, clearing any existing data
    /// and setting the data pointer to the initial position.
    /// </remarks>
    public void Initialize(List<object> dataValuesList)
    {
        dataValues = new(dataValuesList);
        dataPointer = 0;
    }

    /// <summary>
    /// Reads the next value from the data list and advances the data pointer.
    /// </summary>
    /// <returns>
    /// A <see cref="BasicValue"/> representing the next value in the data list.
    /// The value can be a number or a string, depending on the data.
    /// </returns>
    /// <exception cref="BasicRuntimeException">
    /// Thrown when there are no more values to read in the data list.
    /// </exception>
    public BasicValue Read()
    {
        if (dataPointer >= dataValues.Count)
        {
            throw new BasicRuntimeException("?OUT OF DATA ERROR");
        }

        object value = dataValues[dataPointer++];

        return value switch
        {
            double d => BasicValue.FromNumber(d),
            string s => BasicValue.FromString(s),
            _ => BasicValue.FromString(value.ToString() ?? string.Empty),
        };
    }

    /// <summary>
    /// Resets the internal data pointer to the beginning of the data list.
    /// </summary>
    /// <remarks>
    /// This method is typically used to restart reading data values from the start.
    /// </remarks>
    public void Restore()
    {
        dataPointer = 0;
    }

    /// <summary>
    /// Restores the data pointer to the specified position within the data values.
    /// </summary>
    /// <param name="position">
    /// The zero-based index to which the data pointer should be restored.
    /// If the position is out of range, the data pointer is reset to the beginning.
    /// </param>
    public void RestoreToPosition(int position)
    {
        dataPointer = (position < 0 || position >= dataValues.Count) ? 0 : position;
    }

    /// <summary>
    /// Clears all stored data values and resets the data pointer to its initial position.
    /// </summary>
    /// <remarks>
    /// This method removes all elements from the internal data storage and sets the pointer
    /// used for reading data back to the starting position. It is typically used to reset
    /// the state of the data manager.
    /// </remarks>
    public void Clear()
    {
        dataValues.Clear();
        dataPointer = 0;
    }
}