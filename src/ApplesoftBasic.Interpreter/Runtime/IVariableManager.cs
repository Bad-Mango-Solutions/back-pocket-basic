// <copyright file="IVariableManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Manages BASIC variables and arrays.
/// </summary>
public interface IVariableManager
{
    /// <summary>
    /// Gets a variable value.
    /// </summary>
    /// <returns></returns>
    BasicValue GetVariable(string name);

    /// <summary>
    /// Sets a variable value.
    /// </summary>
    void SetVariable(string name, BasicValue value);

    /// <summary>
    /// Gets an array element.
    /// </summary>
    /// <returns></returns>
    BasicValue GetArrayElement(string name, int[] indices);

    /// <summary>
    /// Sets an array element.
    /// </summary>
    void SetArrayElement(string name, int[] indices, BasicValue value);

    /// <summary>
    /// Declares an array with specified dimensions.
    /// </summary>
    void DimArray(string name, int[] dimensions);

    /// <summary>
    /// Clears all variables and arrays.
    /// </summary>
    void Clear();

    /// <summary>
    /// Checks if a variable exists.
    /// </summary>
    /// <returns></returns>
    bool VariableExists(string name);

    /// <summary>
    /// Checks if an array exists.
    /// </summary>
    /// <returns></returns>
    bool ArrayExists(string name);
}