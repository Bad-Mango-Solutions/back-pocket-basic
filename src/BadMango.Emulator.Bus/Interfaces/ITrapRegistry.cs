// <copyright file="ITrapRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

using BadMango.Emulator.Core.Interfaces.Cpu;

/// <summary>
/// Registry for ROM routine interception handlers.
/// </summary>
/// <remarks>
/// <para>
/// The trap registry enables native implementations of ROM routines and I/O operations
/// for performance optimization. Traps can intercept three types of operations:
/// </para>
/// <list type="bullet">
/// <item><description><b>Read traps</b> - Triggered when the address is read.</description></item>
/// <item><description><b>Write traps</b> - Triggered when the address is written.</description></item>
/// <item><description><b>Call traps</b> - Triggered when execution reaches the address (instruction fetch).</description></item>
/// </list>
/// <para>
/// <b>Call Trap Mechanics:</b>
/// </para>
/// <para>
/// Call traps intercept execution at ROM entry points. The behavior depends on how
/// execution arrived at the trapped address:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>JSR/JSL:</b> Return address is on stack. Handler can return with
/// <see cref="TrapResult.ReturnAddress"/> = null to auto-RTS.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>JMP/JML or linear flow:</b> No return address on stack. Handler must
/// specify where to continue via <see cref="TrapResult.ReturnAddress"/>.
/// </description>
/// </item>
/// </list>
/// <para>
/// Traps can be:
/// </para>
/// <list type="bullet">
/// <item><description>Registered at specific addresses with metadata.</description></item>
/// <item><description>Enabled/disabled individually or by category.</description></item>
/// <item><description>Slot-dependent (only fire when a specific slot's expansion ROM is active).</description></item>
/// </list>
/// <para>
/// The registry provides O(1) lookup performance using an internal array indexed
/// by address, suitable for the hot path in memory access and instruction fetch.
/// </para>
/// </remarks>
public interface ITrapRegistry
{
    /// <summary>
    /// Gets the number of traps currently registered.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Registers a call trap handler at a specific address.
    /// </summary>
    /// <param name="address">The ROM address to intercept on execution.</param>
    /// <param name="name">Human-readable name for the trap (e.g., "HOME", "COUT").</param>
    /// <param name="category">Classification of the trap for filtering.</param>
    /// <param name="handler">The native implementation delegate.</param>
    /// <param name="description">Optional detailed description for tooling.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a call trap is already registered at the specified address.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the most common trap type, used for intercepting ROM routine entry points.
    /// The trap fires when the CPU's Program Counter reaches this address during
    /// instruction fetch.
    /// </para>
    /// </remarks>
    void Register(
        Addr address,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null);

    /// <summary>
    /// Registers a trap handler for a specific operation type at an address.
    /// </summary>
    /// <param name="address">The address to intercept.</param>
    /// <param name="operation">The operation type that triggers the trap.</param>
    /// <param name="name">Human-readable name for the trap.</param>
    /// <param name="category">Classification of the trap for filtering.</param>
    /// <param name="handler">The native implementation delegate.</param>
    /// <param name="description">Optional detailed description for tooling.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a trap for the same operation is already registered at the address.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this method to register read or write traps, or when you need explicit
    /// control over the operation type. For call traps, the simpler
    /// <see cref="Register(Addr, string, TrapCategory, TrapHandler, string?)"/>
    /// overload is preferred.
    /// </para>
    /// </remarks>
    void Register(
        Addr address,
        TrapOperation operation,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null);

    /// <summary>
    /// Registers a slot-dependent call trap handler.
    /// </summary>
    /// <param name="address">The ROM address to intercept (typically in $C800-$CFFF expansion ROM space).</param>
    /// <param name="slot">The slot number (1-7) this trap is associated with.</param>
    /// <param name="name">Human-readable name for the trap.</param>
    /// <param name="category">Classification of the trap for filtering.</param>
    /// <param name="handler">The native implementation delegate.</param>
    /// <param name="description">Optional detailed description for tooling.</param>
    /// <remarks>
    /// <para>
    /// Slot-dependent traps only fire when the specified slot's expansion ROM is active
    /// in the $C800-$CFFF region. The handler should still verify slot state as a
    /// defense-in-depth measure.
    /// </para>
    /// </remarks>
    void RegisterSlotDependent(
        Addr address,
        int slot,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null);

    /// <summary>
    /// Registers a slot-dependent trap handler for a specific operation type.
    /// </summary>
    /// <param name="address">The address to intercept.</param>
    /// <param name="operation">The operation type that triggers the trap.</param>
    /// <param name="slot">The slot number (1-7) this trap is associated with.</param>
    /// <param name="name">Human-readable name for the trap.</param>
    /// <param name="category">Classification of the trap for filtering.</param>
    /// <param name="handler">The native implementation delegate.</param>
    /// <param name="description">Optional detailed description for tooling.</param>
    void RegisterSlotDependent(
        Addr address,
        TrapOperation operation,
        int slot,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null);

    /// <summary>
    /// Unregisters a call trap at the specified address.
    /// </summary>
    /// <param name="address">The ROM address to unregister.</param>
    /// <returns>
    /// <see langword="true"/> if a trap was unregistered;
    /// <see langword="false"/> if no call trap was registered at that address.
    /// </returns>
    bool Unregister(Addr address);

    /// <summary>
    /// Unregisters a trap for a specific operation at the specified address.
    /// </summary>
    /// <param name="address">The address to unregister.</param>
    /// <param name="operation">The operation type to unregister.</param>
    /// <returns>
    /// <see langword="true"/> if a trap was unregistered;
    /// <see langword="false"/> if no trap was registered for that operation at that address.
    /// </returns>
    bool Unregister(Addr address, TrapOperation operation);

    /// <summary>
    /// Attempts to execute a call trap at the specified address.
    /// </summary>
    /// <param name="address">The instruction fetch address.</param>
    /// <param name="cpu">The CPU instance for register access.</param>
    /// <param name="bus">The memory bus for RAM access.</param>
    /// <param name="context">The event context for scheduling and signals.</param>
    /// <returns>
    /// A <see cref="TrapResult"/> with the handler's result, or
    /// <see cref="TrapResult.NotHandled"/> if no call trap is registered or enabled
    /// at the address.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is called from the CPU's instruction fetch hot path. It performs
    /// O(1) lookup and returns immediately if no trap is registered or enabled.
    /// </para>
    /// </remarks>
    TrapResult TryExecute(Addr address, ICpu cpu, IMemoryBus bus, IEventContext context);

    /// <summary>
    /// Attempts to execute a trap for the specified operation at an address.
    /// </summary>
    /// <param name="address">The memory address being accessed.</param>
    /// <param name="operation">The operation type (read, write, or call).</param>
    /// <param name="cpu">The CPU instance for register access.</param>
    /// <param name="bus">The memory bus for RAM access.</param>
    /// <param name="context">The event context for scheduling and signals.</param>
    /// <returns>
    /// A <see cref="TrapResult"/> with the handler's result, or
    /// <see cref="TrapResult.NotHandled"/> if no trap is registered or enabled
    /// for the specified operation at the address.
    /// </returns>
    TrapResult TryExecute(Addr address, TrapOperation operation, ICpu cpu, IMemoryBus bus, IEventContext context);

    /// <summary>
    /// Checks if a call trap is registered at the specified address.
    /// </summary>
    /// <param name="address">The address to check.</param>
    /// <returns>
    /// <see langword="true"/> if a call trap is registered (regardless of enabled state);
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool HasTrap(Addr address);

    /// <summary>
    /// Checks if a trap is registered for a specific operation at the address.
    /// </summary>
    /// <param name="address">The address to check.</param>
    /// <param name="operation">The operation type to check.</param>
    /// <returns>
    /// <see langword="true"/> if a trap is registered for the operation (regardless of enabled state);
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool HasTrap(Addr address, TrapOperation operation);

    /// <summary>
    /// Gets call trap information for the specified address.
    /// </summary>
    /// <param name="address">The address to look up.</param>
    /// <returns>
    /// The trap information if a call trap is registered; otherwise, <see langword="null"/>.
    /// </returns>
    TrapInfo? GetTrapInfo(Addr address);

    /// <summary>
    /// Gets trap information for a specific operation at the address.
    /// </summary>
    /// <param name="address">The address to look up.</param>
    /// <param name="operation">The operation type to look up.</param>
    /// <returns>
    /// The trap information if registered; otherwise, <see langword="null"/>.
    /// </returns>
    TrapInfo? GetTrapInfo(Addr address, TrapOperation operation);

    /// <summary>
    /// Enables or disables a call trap at a specific address.
    /// </summary>
    /// <param name="address">The trap address.</param>
    /// <param name="enabled">
    /// <see langword="true"/> to enable; <see langword="false"/> to disable.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the trap exists and was updated;
    /// <see langword="false"/> if no call trap is registered at that address.
    /// </returns>
    bool SetEnabled(Addr address, bool enabled);

    /// <summary>
    /// Enables or disables a trap for a specific operation at an address.
    /// </summary>
    /// <param name="address">The trap address.</param>
    /// <param name="operation">The operation type.</param>
    /// <param name="enabled">
    /// <see langword="true"/> to enable; <see langword="false"/> to disable.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the trap exists and was updated;
    /// <see langword="false"/> if no trap is registered for that operation at that address.
    /// </returns>
    bool SetEnabled(Addr address, TrapOperation operation, bool enabled);

    /// <summary>
    /// Enables or disables all traps in a category.
    /// </summary>
    /// <param name="category">The category to update.</param>
    /// <param name="enabled">
    /// <see langword="true"/> to enable; <see langword="false"/> to disable.
    /// </param>
    /// <returns>The number of traps that were updated.</returns>
    int SetCategoryEnabled(TrapCategory category, bool enabled);

    /// <summary>
    /// Gets all registered trap addresses.
    /// </summary>
    /// <returns>An enumerable of all registered trap addresses.</returns>
    IEnumerable<Addr> GetRegisteredAddresses();

    /// <summary>
    /// Gets all registered trap information.
    /// </summary>
    /// <returns>An enumerable of all registered trap information.</returns>
    IEnumerable<TrapInfo> GetAllTraps();

    /// <summary>
    /// Clears all registered traps.
    /// </summary>
    void Clear();
}