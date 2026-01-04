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
    /// Gets installed cards by slot number (1-7).
    /// </summary>
    /// <value>A read-only dictionary mapping slot numbers to installed slot cards.</value>
    IReadOnlyDictionary<int, ISlotCard> Slots { get; }

    /// <summary>
    /// Gets the currently selected slot for expansion ROM ($C800-$CFFF).
    /// </summary>
    /// <value>
    /// The slot number (1-7) whose expansion ROM is currently visible,
    /// or <see langword="null"/> if no slot is selected (floating bus).
    /// </value>
    int? ActiveExpansionSlot { get; }

    /// <summary>
    /// Installs a slot card in the specified slot.
    /// </summary>
    /// <param name="slot">Slot number (1-7).</param>
    /// <param name="card">The slot card to install.</param>
    /// <exception cref="ArgumentOutOfRangeException">Slot not in range 1-7.</exception>
    /// <exception cref="ArgumentNullException">Card is null.</exception>
    /// <exception cref="InvalidOperationException">Slot already occupied.</exception>
    /// <remarks>
    /// <para>
    /// Installing a card:
    /// </para>
    /// <list type="number">
    /// <item><description>Stores the card reference</description></item>
    /// <item><description>Sets the card's <see cref="ISlotCard.SlotNumber"/> property</description></item>
    /// <item><description>Registers the card's I/O handlers with the dispatcher</description></item>
    /// </list>
    /// </remarks>
    void Install(int slot, ISlotCard card);

    /// <summary>
    /// Removes a slot card from the specified slot.
    /// </summary>
    /// <param name="slot">Slot number (1-7).</param>
    /// <exception cref="ArgumentOutOfRangeException">Slot not in range 1-7.</exception>
    /// <remarks>
    /// <para>
    /// Removing a card:
    /// </para>
    /// <list type="number">
    /// <item><description>Clears the card's I/O handlers from the dispatcher</description></item>
    /// <item><description>If this slot's expansion ROM was active, deselects it</description></item>
    /// <item><description>Clears the slot reference</description></item>
    /// </list>
    /// <para>
    /// Calling Remove on an empty slot is a no-op.
    /// </para>
    /// </remarks>
    void Remove(int slot);

    /// <summary>
    /// Gets the card installed in a slot, or null if empty.
    /// </summary>
    /// <param name="slot">Slot number (1-7).</param>
    /// <returns>The installed slot card, or <see langword="null"/> if empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Slot not in range 1-7.</exception>
    ISlotCard? GetCard(int slot);

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
    /// <para>
    /// Called when CPU accesses $Cn00-$CnFF to make that slot's expansion ROM visible.
    /// Any previously selected slot is implicitly deselected.
    /// </para>
    /// <para>
    /// Selection process:
    /// </para>
    /// <list type="number">
    /// <item><description>If another slot was selected, notifies that card via
    /// <see cref="ISlotCard.OnExpansionROMDeselected"/></description></item>
    /// <item><description>Updates <see cref="ActiveExpansionSlot"/></description></item>
    /// <item><description>Notifies the new slot's card via
    /// <see cref="ISlotCard.OnExpansionROMSelected"/></description></item>
    /// </list>
    /// </remarks>
    void SelectExpansionSlot(int slot);

    /// <summary>
    /// Deselects expansion ROM (called when CPU accesses $CFFF).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns $C800-$CFFF to floating bus state where no slot's expansion ROM is visible.
    /// </para>
    /// <para>
    /// Deselection process:
    /// </para>
    /// <list type="number">
    /// <item><description>If a slot was selected, notifies that card via
    /// <see cref="ISlotCard.OnExpansionROMDeselected"/></description></item>
    /// <item><description>Sets <see cref="ActiveExpansionSlot"/> to <see langword="null"/></description></item>
    /// </list>
    /// </remarks>
    void DeselectExpansionSlot();

    /// <summary>
    /// Called when the bus handles an access to $C100-$C7FF.
    /// </summary>
    /// <param name="address">Address in range $C100-$C7FF.</param>
    /// <remarks>
    /// <para>
    /// Determines the slot from the address and triggers expansion ROM selection.
    /// The slot number is derived from the high nibble: slot = (address >> 8) &amp; 0x07.
    /// </para>
    /// <para>
    /// For example:
    /// </para>
    /// <list type="bullet">
    /// <item><description>$C100-$C1FF selects slot 1</description></item>
    /// <item><description>$C600-$C6FF selects slot 6</description></item>
    /// </list>
    /// </remarks>
    void HandleSlotROMAccess(Addr address);

    /// <summary>
    /// Resets all slot cards and clears expansion ROM selection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reset process:
    /// </para>
    /// <list type="number">
    /// <item><description>Deselects any active expansion ROM</description></item>
    /// <item><description>Calls <see cref="IPeripheral.Reset"/> on all installed cards</description></item>
    /// </list>
    /// </remarks>
    void Reset();
}