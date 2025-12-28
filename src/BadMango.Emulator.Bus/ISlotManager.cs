// <copyright file="ISlotManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core;

/// <summary>
/// Manages the expansion slots of an Apple II system.
/// </summary>
/// <remarks>
/// <para>
/// The Apple II has 7 expansion slots (slots 1-7) where peripheral cards can be installed.
/// Each slot has:
/// </para>
/// <list type="bullet">
/// <item><description>16 bytes of I/O space at $C0n0-$C0nF (where n is the slot number + 8)</description></item>
/// <item><description>256 bytes of slot ROM at $Cn00-$CnFF</description></item>
/// <item><description>Access to the shared expansion ROM space at $C800-$CFFF (one slot at a time)</description></item>
/// </list>
/// <para>
/// The slot manager handles:
/// </para>
/// <list type="bullet">
/// <item><description>Installing and removing peripheral cards</description></item>
/// <item><description>Tracking which slot owns the expansion ROM window</description></item>
/// <item><description>Routing I/O and ROM accesses to the appropriate card</description></item>
/// <item><description>Reset propagation to all installed cards</description></item>
/// </list>
/// </remarks>
public interface ISlotManager
{
    /// <summary>
    /// Gets the installed cards by slot number (1-7).
    /// </summary>
    /// <value>A read-only dictionary mapping slot numbers to installed peripherals.</value>
    IReadOnlyDictionary<int, IPeripheral> Slots { get; }

    /// <summary>
    /// Gets the currently selected slot for the $C800-$CFFF expansion ROM window.
    /// </summary>
    /// <value>
    /// The slot number (1-7) that owns the expansion ROM window,
    /// or <see langword="null"/> if no slot is selected.
    /// </value>
    int? ActiveExpansionSlot { get; }

    /// <summary>
    /// Installs a peripheral card in the specified slot.
    /// </summary>
    /// <param name="slot">The slot number (1-7).</param>
    /// <param name="card">The peripheral card to install.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="slot"/> is not between 1 and 7.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a card is already installed in the specified slot.
    /// </exception>
    void Install(int slot, IPeripheral card);

    /// <summary>
    /// Removes the peripheral card from the specified slot.
    /// </summary>
    /// <param name="slot">The slot number (1-7).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="slot"/> is not between 1 and 7.
    /// </exception>
    void Remove(int slot);

    /// <summary>
    /// Gets the peripheral card installed in the specified slot.
    /// </summary>
    /// <param name="slot">The slot number (1-7).</param>
    /// <returns>
    /// The peripheral card in the slot, or <see langword="null"/> if the slot is empty.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="slot"/> is not between 1 and 7.
    /// </exception>
    IPeripheral? GetCard(int slot);

    /// <summary>
    /// Selects a slot to own the $C800-$CFFF expansion ROM window.
    /// </summary>
    /// <param name="slot">The slot number (1-7) to select.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="slot"/> is not between 1 and 7.
    /// </exception>
    /// <remarks>
    /// <para>
    /// On a real Apple II, accessing $CnFF (where n is a slot number) selects that slot's
    /// expansion ROM into the $C800-$CFFF window. This method allows direct control of
    /// the selection for emulation purposes.
    /// </para>
    /// </remarks>
    void SelectExpansionSlot(int slot);

    /// <summary>
    /// Deselects the expansion ROM window, returning it to floating bus.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On a real Apple II, accessing $CFFF deselects all expansion ROMs.
    /// After deselection, reads from $C800-$CFFF return floating bus values.
    /// </para>
    /// </remarks>
    void DeselectExpansionSlot();

    /// <summary>
    /// Resets all installed peripheral cards.
    /// </summary>
    /// <remarks>
    /// Called during system reset. Each installed card's <see cref="IPeripheral.Reset"/>
    /// method is called, and the expansion ROM selection is cleared.
    /// </remarks>
    void Reset();
}