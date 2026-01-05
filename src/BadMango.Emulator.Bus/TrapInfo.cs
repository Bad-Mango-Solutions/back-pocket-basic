// <copyright file="TrapInfo.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Metadata for a registered trap handler.
/// </summary>
/// <remarks>
/// <para>
/// Each trap registration stores metadata alongside the handler, enabling tooling
/// to display trap information in debuggers and profilers. The <see cref="Name"/>
/// and <see cref="Description"/> fields are human-readable for diagnostic display.
/// </para>
/// <para>
/// The <see cref="Category"/> field allows bulk enable/disable of traps by type,
/// useful for compatibility testing where users want to force ROM execution.
/// </para>
/// <para>
/// The <see cref="Operation"/> field specifies which memory operation triggers
/// the trap (read, write, or call/execute).
/// </para>
/// <para>
/// The <see cref="MemoryContext"/> field identifies which memory configuration
/// the trap targets, allowing different handlers for the same address depending
/// on which memory bank is active.
/// </para>
/// </remarks>
/// <param name="Address">The memory address where this trap is registered.</param>
/// <param name="Name">Human-readable name for the trap (e.g., "HOME", "COUT").</param>
/// <param name="Category">Classification of the trap for filtering.</param>
/// <param name="Operation">The type of operation that triggers this trap.</param>
/// <param name="Handler">The native implementation delegate.</param>
/// <param name="Description">Optional detailed description for tooling.</param>
/// <param name="IsEnabled">Whether this trap is currently enabled.</param>
/// <param name="SlotNumber">
/// For slot-dependent traps, the slot number this trap is associated with.
/// <see langword="null"/> for non-slot-dependent traps.
/// </param>
/// <param name="MemoryContext">
/// The memory context this trap targets. Defaults to <see cref="MemoryContexts.Rom"/>.
/// </param>
public readonly record struct TrapInfo(
    Addr Address,
    string Name,
    TrapCategory Category,
    TrapOperation Operation,
    TrapHandler Handler,
    string? Description,
    bool IsEnabled,
    int? SlotNumber,
    MemoryContext MemoryContext)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrapInfo"/> struct with the default ROM context.
    /// </summary>
    /// <param name="address">The memory address where this trap is registered.</param>
    /// <param name="name">Human-readable name for the trap.</param>
    /// <param name="category">Classification of the trap for filtering.</param>
    /// <param name="operation">The type of operation that triggers this trap.</param>
    /// <param name="handler">The native implementation delegate.</param>
    /// <param name="description">Optional detailed description for tooling.</param>
    /// <param name="isEnabled">Whether this trap is currently enabled.</param>
    /// <param name="slotNumber">For slot-dependent traps, the slot number.</param>
    public TrapInfo(
        Addr address,
        string name,
        TrapCategory category,
        TrapOperation operation,
        TrapHandler handler,
        string? description,
        bool isEnabled,
        int? slotNumber)
        : this(address, name, category, operation, handler, description, isEnabled, slotNumber, MemoryContexts.Rom)
    {
    }

    /// <summary>
    /// Gets a value indicating whether this trap is slot-dependent.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this trap should only fire when the specified
    /// slot's expansion ROM is active; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsSlotDependent => SlotNumber.HasValue;

    /// <summary>
    /// Gets a value indicating whether this trap targets Language Card RAM.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this trap targets Language Card RAM;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool TargetsLcRam => MemoryContext.Id == MemoryContexts.LanguageCardRam.Id;
}