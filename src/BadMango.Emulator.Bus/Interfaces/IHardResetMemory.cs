// <copyright file="IHardResetMemory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Represents a RAM backing store that can be explicitly cleared during a hard reset.
/// </summary>
/// <remarks>
/// <para>
/// Hard reset must clear physical RAM stores regardless of their current bus mapping state.
/// Components implementing this interface expose that operation directly so callers do not
/// need to infer coverage by walking the active page table.
/// </para>
/// </remarks>
public interface IHardResetMemory
{
    /// <summary>
    /// Clears the full backing store associated with this component.
    /// </summary>
    void ClearForHardReset();
}