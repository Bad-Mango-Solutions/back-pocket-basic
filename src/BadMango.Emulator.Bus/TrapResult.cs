// <copyright file="TrapResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Core;

/// <summary>
/// Result of a trap handler execution.
/// </summary>
/// <remarks>
/// <para>
/// When the CPU fetches an instruction at a trapped address, the registered handler
/// is invoked. The handler can either handle the operation natively (returning
/// <see cref="Handled"/> = <see langword="true"/>) or decline (returning
/// <see cref="Handled"/> = <see langword="false"/>), allowing the ROM code to execute.
/// </para>
/// <para>
/// If a handler returns <see langword="true"/> for <see cref="Handled"/>, the CPU
/// will charge the <see cref="CyclesConsumed"/> cycles and return based on
/// <see cref="ReturnMethod"/>:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="TrapReturnMethod.Rts"/>: Pull return address from stack (JSR target).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="TrapReturnMethod.Rti"/>: Pull status and return address from stack (interrupt handler).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="TrapReturnMethod.None"/>: Jump to <see cref="ReturnAddress"/> if set,
/// or continue at current PC (JMP target or linear flow).
/// </description>
/// </item>
/// </list>
/// </remarks>
/// <param name="Handled">
/// <see langword="true"/> if the trap was handled natively; <see langword="false"/>
/// to fall through to ROM execution.
/// </param>
/// <param name="CyclesConsumed">
/// The number of cycles to charge for this operation. Only meaningful when
/// <see cref="Handled"/> is <see langword="true"/>.
/// </param>
/// <param name="ReturnMethod">
/// Specifies how the CPU should return after handling the trap. Defaults to
/// <see cref="TrapReturnMethod.Rts"/> for compatibility with JSR-based traps.
/// </param>
/// <param name="ReturnAddress">
/// Override return address. Used when <see cref="ReturnMethod"/> is
/// <see cref="TrapReturnMethod.None"/>. If set, the CPU jumps to this address
/// after trap completion; if <see langword="null"/> with <see cref="TrapReturnMethod.None"/>,
/// execution continues at the current PC.
/// </param>
public readonly record struct TrapResult(
    bool Handled,
    Cycle CyclesConsumed,
    TrapReturnMethod ReturnMethod,
    Addr? ReturnAddress)
{
    /// <summary>
    /// Gets a result indicating the trap was not handled and ROM should execute.
    /// </summary>
    public static TrapResult NotHandled => new(Handled: false, default, TrapReturnMethod.None, null);

    /// <summary>
    /// Creates a result indicating the trap was handled with the specified cycle cost.
    /// Returns via RTS (assumes trap was reached via JSR).
    /// </summary>
    /// <param name="cycles">The number of cycles consumed by the trap handler.</param>
    /// <returns>A <see cref="TrapResult"/> indicating successful handling with RTS return.</returns>
    public static TrapResult Success(Cycle cycles) =>
        new(Handled: true, cycles, TrapReturnMethod.Rts, null);

    /// <summary>
    /// Creates a result indicating the trap was handled with the specified return method.
    /// </summary>
    /// <param name="cycles">The number of cycles consumed by the trap handler.</param>
    /// <param name="returnMethod">How the CPU should return after handling the trap.</param>
    /// <returns>A <see cref="TrapResult"/> indicating successful handling.</returns>
    public static TrapResult Success(Cycle cycles, TrapReturnMethod returnMethod) =>
        new(Handled: true, cycles, returnMethod, null);

    /// <summary>
    /// Creates a result indicating the trap was handled with a redirect to a different address.
    /// Uses <see cref="TrapReturnMethod.None"/> since the return address is explicitly specified.
    /// </summary>
    /// <param name="cycles">The number of cycles consumed by the trap handler.</param>
    /// <param name="returnAddress">The address to jump to after trap completion.</param>
    /// <returns>A <see cref="TrapResult"/> indicating successful handling with redirection.</returns>
    public static TrapResult SuccessWithRedirect(Cycle cycles, Addr returnAddress) =>
        new(Handled: true, cycles, TrapReturnMethod.None, returnAddress);

    /// <summary>
    /// Creates a result for an interrupt handler trap (IRQ, NMI, or BRK).
    /// Returns via RTI.
    /// </summary>
    /// <param name="cycles">The number of cycles consumed by the trap handler.</param>
    /// <returns>A <see cref="TrapResult"/> indicating successful handling with RTI return.</returns>
    public static TrapResult SuccessInterrupt(Cycle cycles) =>
        new(Handled: true, cycles, TrapReturnMethod.Rti, null);
}