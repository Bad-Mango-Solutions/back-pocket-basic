// <copyright file="IBasicRuntimeContext.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

/// <summary>
/// Provides an interface for managing the BASIC language runtime state, including variables,
/// functions, data, loops, and subroutine call stacks.
/// </summary>
/// <remarks>
/// This context aggregates all BASIC language runtime state managers and provides a single
/// point of access for managing the interpreter's execution state. It enables clean separation
/// between BASIC language concerns and system/hardware services.
/// </remarks>
public interface IBasicRuntimeContext
{
    /// <summary>
    /// Gets the variable manager responsible for storing and retrieving BASIC variables and arrays.
    /// </summary>
    /// <value>
    /// An instance of <see cref="IVariableManager"/> that manages all variables and arrays in the BASIC program.
    /// </value>
    IVariableManager Variables { get; }

    /// <summary>
    /// Gets the function manager responsible for managing user-defined functions (DEF FN).
    /// </summary>
    /// <value>
    /// An instance of <see cref="IFunctionManager"/> that handles definition and evaluation of user-defined functions.
    /// </value>
    IFunctionManager Functions { get; }

    /// <summary>
    /// Gets the data manager responsible for managing DATA, READ, and RESTORE operations.
    /// </summary>
    /// <value>
    /// An instance of <see cref="IDataManager"/> that manages data values and the data pointer.
    /// </value>
    IDataManager Data { get; }

    /// <summary>
    /// Gets the loop manager responsible for managing FOR-NEXT loop state.
    /// </summary>
    /// <value>
    /// An instance of <see cref="ILoopManager"/> that tracks active FOR-NEXT loops.
    /// </value>
    ILoopManager Loops { get; }

    /// <summary>
    /// Gets the GOSUB manager responsible for managing the GOSUB/RETURN call stack.
    /// </summary>
    /// <value>
    /// An instance of <see cref="IGosubManager"/> that manages subroutine return addresses.
    /// </value>
    IGosubManager Gosub { get; }

    /// <summary>
    /// Clears BASIC runtime state, resetting variables, functions, loops, and the GOSUB stack.
    /// </summary>
    /// <remarks>
    /// This method is typically called when executing a CLEAR statement in BASIC code.
    /// It resets variables, functions, loops, and the GOSUB stack to their initial state.
    /// Note: This does NOT clear the DATA pointer - that is only reset when a new program runs.
    /// System services are also unaffected by this method.
    /// </remarks>
    void Clear();
}