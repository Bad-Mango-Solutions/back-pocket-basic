// <copyright file="BasicRuntimeContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

/// <summary>
/// Provides a concrete implementation of <see cref="IBasicRuntimeContext"/> that aggregates
/// BASIC language runtime state managers.
/// </summary>
/// <remarks>
/// This class serves as a container for all BASIC runtime state, including variables, functions,
/// data, loops, and the GOSUB stack. It provides a unified interface for managing and resetting
/// BASIC program state.
/// </remarks>
public sealed class BasicRuntimeContext : IBasicRuntimeContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BasicRuntimeContext"/> class.
    /// </summary>
    /// <param name="variables">The variable manager for BASIC variables and arrays.</param>
    /// <param name="functions">The function manager for user-defined functions.</param>
    /// <param name="data">The data manager for DATA/READ/RESTORE operations.</param>
    /// <param name="loops">The loop manager for FOR-NEXT loops.</param>
    /// <param name="gosub">The GOSUB manager for subroutine calls.</param>
    public BasicRuntimeContext(
        IVariableManager variables,
        IFunctionManager functions,
        IDataManager data,
        ILoopManager loops,
        IGosubManager gosub)
    {
        Variables = variables;
        Functions = functions;
        Data = data;
        Loops = loops;
        Gosub = gosub;
    }

    /// <inheritdoc/>
    public IVariableManager Variables { get; }

    /// <inheritdoc/>
    public IFunctionManager Functions { get; }

    /// <inheritdoc/>
    public IDataManager Data { get; }

    /// <inheritdoc/>
    public ILoopManager Loops { get; }

    /// <inheritdoc/>
    public IGosubManager Gosub { get; }

    /// <inheritdoc/>
    public void Clear()
    {
        Variables.Clear();
        Functions.Clear();
        Loops.Clear();
        Gosub.Clear();
    }
}