// <copyright file="IDataManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Provides an interface for managing DATA, READ, and RESTORE operations in the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// This interface defines methods for initializing, reading, restoring, and clearing data values.
/// It is used to handle the runtime management of data statements in Applesoft BASIC programs.
/// </remarks>
public interface IDataManager
{
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
    void Initialize(List<object> dataValuesList);

    /// <summary>
    /// Reads the next value from the data list and advances the data pointer.
    /// </summary>
    /// <returns>
    /// A <see cref="BasicValue"/> representing the next value in the data list.
    /// The value can be either a number or a string, depending on the data.
    /// </returns>
    /// <exception cref="BasicRuntimeException">
    /// Thrown when there are no more values to read in the data list.
    /// </exception>
    BasicValue Read();

    /// <summary>
    /// Resets the internal data pointer to the beginning of the data list.
    /// </summary>
    /// <remarks>
    /// This method is used to restart reading data values from the start of the list.
    /// It is typically invoked when a RESTORE statement is executed in an Applesoft BASIC program.
    /// </remarks>
    void Restore();

    /// <summary>
    /// Restores the data pointer to the specified position within the data values.
    /// </summary>
    /// <param name="position">
    /// The zero-based index to which the data pointer should be restored.
    /// If the position is out of range, the data pointer is reset to the beginning.
    /// </param>
    /// <remarks>
    /// This method allows precise control over the data pointer's position, enabling
    /// advanced scenarios such as re-reading specific data values or skipping ahead.
    /// It is typically used in conjunction with Applesoft BASIC's RESTORE statement.
    /// </remarks>
    void RestoreToPosition(int position);

    /// <summary>
    /// Clears all stored data values and resets the data pointer to its initial position.
    /// </summary>
    /// <remarks>
    /// This method removes all elements from the internal data storage and resets the pointer
    /// used for reading data back to the starting position. It is typically used to reset
    /// the state of the data manager in the Applesoft BASIC interpreter.
    /// </remarks>
    void Clear();
}