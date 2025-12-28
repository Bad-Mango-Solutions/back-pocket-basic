// <copyright file="TrapResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core;

/// <summary>
/// Result of a trap handler execution.
/// </summary>
/// <remarks>
/// <para>
/// When a trap handler executes, it returns this result to indicate:
/// </para>
/// <list type="bullet">
/// <item><description>Whether the trap was handled (or should fall through to ROM)</description></item>
/// <item><description>How many cycles the operation consumed</description></item>
/// <item><description>Optionally, a return address override</description></item>
/// </list>
/// </remarks>
/// <param name="Handled">
/// <see langword="true"/> if the trap was handled; <see langword="false"/> to fall through to ROM.
/// </param>
/// <param name="CyclesConsumed">
/// The number of cycles to charge for this operation.
/// </param>
/// <param name="ReturnAddress">
/// Override return address, or <see langword="null"/> to use normal RTS (pop from stack).
/// </param>
public readonly record struct TrapResult(
    bool Handled,
    ulong CyclesConsumed,
    Addr? ReturnAddress = null)
{
    /// <summary>
    /// Creates a result indicating the trap was not handled.
    /// </summary>
    /// <returns>A <see cref="TrapResult"/> with <see cref="Handled"/> set to <see langword="false"/>.</returns>
    public static TrapResult NotHandled() => new(Handled: false, CyclesConsumed: 0);

    /// <summary>
    /// Creates a result indicating the trap was handled with the specified cycles.
    /// </summary>
    /// <param name="cyclesConsumed">The number of cycles consumed by the trap handler.</param>
    /// <returns>A <see cref="TrapResult"/> with <see cref="Handled"/> set to <see langword="true"/>.</returns>
    public static TrapResult Success(ulong cyclesConsumed) =>
        new(Handled: true, CyclesConsumed: cyclesConsumed);

    /// <summary>
    /// Creates a result indicating the trap was handled with the specified cycles and return address.
    /// </summary>
    /// <param name="cyclesConsumed">The number of cycles consumed by the trap handler.</param>
    /// <param name="returnAddress">The address to return to instead of using RTS.</param>
    /// <returns>A <see cref="TrapResult"/> with <see cref="Handled"/> set to <see langword="true"/>.</returns>
    public static TrapResult SuccessWithReturn(ulong cyclesConsumed, Addr returnAddress) =>
        new(Handled: true, CyclesConsumed: cyclesConsumed, ReturnAddress: returnAddress);
}