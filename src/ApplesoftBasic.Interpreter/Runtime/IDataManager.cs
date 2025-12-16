// <copyright file="IDataManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Manages DATA/READ/RESTORE operations.
/// </summary>
public interface IDataManager
{
    /// <summary>
    /// Initializes with data values from the program.
    /// </summary>
    void Initialize(List<object> dataValues);

    /// <summary>
    /// Reads the next data value.
    /// </summary>
    /// <returns></returns>
    BasicValue Read();

    /// <summary>
    /// Restores data pointer to beginning.
    /// </summary>
    void Restore();

    /// <summary>
    /// Restores data pointer to a specific position.
    /// </summary>
    void RestoreToPosition(int position);

    /// <summary>
    /// Clears all data.
    /// </summary>
    void Clear();
}