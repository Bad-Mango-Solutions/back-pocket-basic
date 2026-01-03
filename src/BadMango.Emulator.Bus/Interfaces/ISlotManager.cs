// <copyright file="ISlotManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Manages the 7 expansion slots of an Apple II and the expansion ROM selection logic.
/// </summary>
/// <remarks>
/// <para>
/// The slot manager is responsible for:
/// </para>
/// <list type="bullet">
/// <item><description>Tracking which cards are installed in which slots</description></item>
/// <item><description>Routing I/O accesses ($C0n0-$C0nF) to the correct card</description></item>
/// <item><description>Routing ROM accesses ($Cn00-$CnFF) to the correct card</description></item>
/// <item><description>Managing the expansion ROM bank ($C800-$CFFF) selection</description></item>
/// <item><description>Handling the $CFFF release trigger</description></item>
/// </list>
/// <para>
/// The expansion ROM selection protocol:
/// </para>
/// <list type="number">
/// <item><description>Any access to $Cn00-$CnFF selects slot n's expansion ROM</description></item>
/// <item><description>Any access to $CFFF deselects all expansion ROMs</description></item>
/// <item><description>Only one slot's expansion ROM can be visible at a time</description></item>
/// <item><description>When no slot is selected, $C800-$CFFF returns floating bus</description></item>
/// </list>
/// </remarks>
public interface ISlotManager
{
    /// <summary>
    /// Gets the currently selected slot for expansion ROM ($C800-$CFFF).
    /// </summary>
    /// <value>
    /// The slot number (1-7) whose expansion ROM is currently visible,
    /// or <see langword="null"/> if no slot is selected (floating bus).
    /// </value>
    int? ActiveExpansionSlot { get; }

    /// <summary>
    /// Gets the ROM region for a specific slot.
    /// </summary>
    /// <param name="slot">Slot number (1-7).</param>
    /// <returns>
    /// The slot's ROM region ($Cn00-$CnFF), or <see langword="null"/> if
    /// the slot is empty or has no ROM.
    /// </returns>
    IBusTarget? GetSlotRomRegion(int slot);

    /// <summary>
    /// Gets the expansion ROM region for a specific slot.
    /// </summary>
    /// <param name="slot">Slot number (1-7).</param>
    /// <returns>
    /// The slot's expansion ROM region ($C800-$CFFF), or <see langword="null"/>
    /// if the slot is empty or has no expansion ROM.
    /// </returns>
    IBusTarget? GetExpansionRomRegion(int slot);

    /// <summary>
    /// Selects a slot's expansion ROM for the $C800-$CFFF region.
    /// </summary>
    /// <param name="slot">Slot number (1-7).</param>
    /// <remarks>
    /// Called when CPU accesses $Cn00-$CnFF to make that slot's expansion ROM visible.
    /// Any previously selected slot is implicitly deselected.
    /// </remarks>
    void SelectExpansionSlot(int slot);

    /// <summary>
    /// Deselects expansion ROM (called when CPU accesses $CFFF).
    /// </summary>
    /// <remarks>
    /// Returns $C800-$CFFF to floating bus state where no slot's expansion ROM is visible.
    /// </remarks>
    void DeselectExpansionSlot();
}