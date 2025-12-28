// <copyright file="ITrapRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core;

/// <summary>
/// Registry for ROM routine interception handlers.
/// </summary>
/// <remarks>
/// <para>
/// The trap registry allows native implementations of ROM routines to be registered
/// at specific addresses. When the CPU fetches an instruction from a trapped address,
/// the registered handler executes instead of (or in addition to) the ROM code.
/// </para>
/// <para>
/// This serves multiple purposes:
/// </para>
/// <list type="bullet">
/// <item><description><b>Legal compliance:</b> Avoid distributing or requiring copyrighted ROM images</description></item>
/// <item><description><b>Performance:</b> Native implementations can be faster than cycle-accurate emulation</description></item>
/// <item><description><b>Enhanced functionality:</b> Add features not in original ROMs</description></item>
/// <item><description><b>Debugging:</b> Trap handlers can provide instrumentation hooks</description></item>
/// </list>
/// <para>
/// The CPU checks for traps on instruction fetch. When a trap fires:
/// </para>
/// <list type="number">
/// <item><description>The trap handler executes instead of the ROM code</description></item>
/// <item><description>The handler performs the equivalent operation natively</description></item>
/// <item><description>The handler returns control as if the ROM routine had executed</description></item>
/// <item><description>Cycle counts are adjusted to approximate original timing</description></item>
/// </list>
/// </remarks>
public interface ITrapRegistry
{
    /// <summary>
    /// Registers a trap handler at a specific address.
    /// </summary>
    /// <param name="address">The ROM address to intercept.</param>
    /// <param name="name">Human-readable name for the trap.</param>
    /// <param name="category">Classification of the trap.</param>
    /// <param name="handler">The native implementation.</param>
    /// <param name="description">Optional description for tooling.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when a trap is already registered at the specified address.
    /// </exception>
    void Register(
        Addr address,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null);

    /// <summary>
    /// Unregisters a trap at the specified address.
    /// </summary>
    /// <param name="address">The address to unregister.</param>
    /// <returns>
    /// <see langword="true"/> if a trap was unregistered;
    /// <see langword="false"/> if no trap was registered at the address.
    /// </returns>
    bool Unregister(Addr address);

    /// <summary>
    /// Checks if an address has a trap and executes it if so.
    /// </summary>
    /// <param name="address">The fetch address.</param>
    /// <param name="cpu">CPU for register access.</param>
    /// <param name="bus">Memory bus.</param>
    /// <param name="context">Event context.</param>
    /// <returns>
    /// Trap result with <see cref="TrapResult.Handled"/> set to <see langword="true"/> if the trap executed,
    /// or <see cref="TrapResult.Handled"/> set to <see langword="false"/> if no trap exists or it's disabled.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is called by the CPU on instruction fetch. If an enabled trap exists
    /// at the address, the handler is invoked. If the handler returns
    /// <see cref="TrapResult.Handled"/> = <see langword="true"/>, the CPU skips normal
    /// instruction execution and uses the returned cycles and return address.
    /// </para>
    /// </remarks>
    TrapResult TryExecute(Addr address, ICpu cpu, IMemoryBus bus, IEventContext context);

    /// <summary>
    /// Enables or disables a trap without removing it.
    /// </summary>
    /// <param name="address">The address of the trap to modify.</param>
    /// <param name="enabled"><see langword="true"/> to enable; <see langword="false"/> to disable.</param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no trap is registered at the specified address.
    /// </exception>
    void SetEnabled(Addr address, bool enabled);

    /// <summary>
    /// Enables or disables all traps in a category.
    /// </summary>
    /// <param name="category">The category of traps to modify.</param>
    /// <param name="enabled"><see langword="true"/> to enable; <see langword="false"/> to disable.</param>
    void SetCategoryEnabled(TrapCategory category, bool enabled);

    /// <summary>
    /// Gets information about all registered traps.
    /// </summary>
    /// <returns>An enumerable of <see cref="TrapInfo"/> for all registered traps.</returns>
    IEnumerable<TrapInfo> GetAll();

    /// <summary>
    /// Gets information about a specific trap.
    /// </summary>
    /// <param name="address">The address to query.</param>
    /// <returns>
    /// The <see cref="TrapInfo"/> for the trap, or <see langword="null"/> if no trap exists.
    /// </returns>
    TrapInfo? GetInfo(Addr address);

    /// <summary>
    /// Checks if any trap is registered at the address (for fast-path skip).
    /// </summary>
    /// <param name="address">The address to check.</param>
    /// <returns>
    /// <see langword="true"/> if a trap is registered at the address (regardless of enabled state);
    /// <see langword="false"/> otherwise.
    /// </returns>
    /// <remarks>
    /// This method is designed for fast-path checking. It should be O(1) using a hash set
    /// or bitmap to minimize overhead on every instruction fetch.
    /// </remarks>
    bool HasTrap(Addr address);
}