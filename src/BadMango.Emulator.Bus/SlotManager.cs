// <copyright file="SlotManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Collections.ObjectModel;

using Interfaces;

/// <summary>
/// Manages the 7 expansion slots and expansion ROM selection for an Apple II.
/// </summary>
/// <remarks>
/// <para>
/// The Apple II has 7 expansion slots (1-7) with a specific memory and I/O layout.
/// This class tracks installed cards, handles I/O handler registration with the
/// <see cref="IOPageDispatcher"/>, and manages the expansion ROM selection protocol.
/// </para>
/// <para>
/// Slot 0 is reserved for the Language Card and is not managed by this class.
/// </para>
/// <para>
/// The Extended 80-Column Card provides a default expansion ROM at $C800-$CFFF.
/// Other slot cards' expansion ROMs layer on top when their slot is selected.
/// Reading $CFFF removes the layer, revealing the default ROM again.
/// </para>
/// </remarks>
public sealed class SlotManager : ISlotManager
{
    /// <summary>
    /// The minimum valid peripheral slot number.
    /// </summary>
    private const int MinSlot = 1;

    /// <summary>
    /// The maximum valid peripheral slot number.
    /// </summary>
    private const int MaxSlot = 7;

    /// <summary>
    /// The number of peripheral slots.
    /// </summary>
    private const int SlotCount = 7;

    private readonly IOPageDispatcher dispatcher;
    private readonly ISlotCard?[] slots;
    private readonly IBusTarget?[] internalExpansionRoms;
    private readonly Dictionary<int, ISlotCard> slotsView;
    private readonly IReadOnlyDictionary<int, ISlotCard> slotsReadOnly;
    private IBusTarget? defaultExpansionRom;
    private int? activeExpansionSlot;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlotManager"/> class.
    /// </summary>
    /// <param name="dispatcher">The I/O page dispatcher for registering slot handlers.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dispatcher"/> is <see langword="null"/>.
    /// </exception>
    public SlotManager(IOPageDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        this.dispatcher = dispatcher;
        this.slots = new ISlotCard?[SlotCount];
        this.internalExpansionRoms = new IBusTarget?[SlotCount];
        this.slotsView = new();
        this.slotsReadOnly = new ReadOnlyDictionary<int, ISlotCard>(this.slotsView);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<int, ISlotCard> Slots => slotsReadOnly;

    /// <inheritdoc />
    public int? ActiveExpansionSlot => activeExpansionSlot;

    /// <inheritdoc />
    public void Install(int slot, ISlotCard card)
    {
        ValidateSlotNumber(slot);
        ArgumentNullException.ThrowIfNull(card);

        int index = slot - MinSlot;
        if (slots[index] is not null)
        {
            throw new InvalidOperationException($"Slot {slot} is already occupied.");
        }

        // Store the card reference
        slots[index] = card;
        slotsView[slot] = card;

        // Set the card's slot number
        card.SlotNumber = slot;

        // Register I/O handlers with the dispatcher
        if (card.IOHandlers is not null)
        {
            dispatcher.InstallSlotHandlers(slot, card.IOHandlers);
        }
    }

    /// <inheritdoc />
    public void Remove(int slot)
    {
        ValidateSlotNumber(slot);

        int index = slot - MinSlot;
        var card = slots[index];
        if (card is null)
        {
            return; // No-op for empty slot
        }

        // Clear I/O handlers from dispatcher
        dispatcher.RemoveSlotHandlers(slot);

        // If this slot's expansion ROM was active, deselect it
        if (activeExpansionSlot == slot)
        {
            card.OnExpansionROMDeselected();
            activeExpansionSlot = null;
        }

        // Clear the slot reference
        slots[index] = null;
        slotsView.Remove(slot);
    }

    /// <inheritdoc />
    public ISlotCard? GetCard(int slot)
    {
        ValidateSlotNumber(slot);
        return slots[slot - MinSlot];
    }

    /// <inheritdoc />
    public IBusTarget? GetSlotRomRegion(int slot)
    {
        ValidateSlotNumber(slot);
        return slots[slot - MinSlot]?.ROMRegion;
    }

    /// <inheritdoc />
    public IBusTarget? GetExpansionRomRegion(int slot)
    {
        ValidateSlotNumber(slot);
        int index = slot - MinSlot;

        // Internal expansion ROM takes precedence over physical card
        if (internalExpansionRoms[index] is { } internalRom)
        {
            return internalRom;
        }

        return slots[index]?.ExpansionROMRegion;
    }

    /// <inheritdoc />
    public IBusTarget? GetVisibleExpansionRom()
    {
        // If a slot is selected, return its expansion ROM
        if (activeExpansionSlot is { } slot)
        {
            return GetExpansionRomRegion(slot);
        }

        // Otherwise return the default expansion ROM (from Extended 80-Column Card)
        return defaultExpansionRom;
    }

    /// <inheritdoc />
    public void RegisterInternalExpansionRom(int slot, IBusTarget? expansionRom)
    {
        ValidateSlotNumber(slot);
        internalExpansionRoms[slot - MinSlot] = expansionRom;
    }

    /// <inheritdoc />
    public void SetDefaultExpansionRom(IBusTarget? expansionRom)
    {
        defaultExpansionRom = expansionRom;
    }

    /// <inheritdoc />
    public void SelectExpansionSlot(int slot)
    {
        ValidateSlotNumber(slot);

        // If another slot was selected, notify it of deselection
        if (activeExpansionSlot.HasValue && activeExpansionSlot.Value != slot)
        {
            var previousCard = slots[activeExpansionSlot.Value - MinSlot];
            previousCard?.OnExpansionROMDeselected();
        }

        // Update active slot
        activeExpansionSlot = slot;

        // Notify the new slot's card of selection
        var newCard = slots[slot - MinSlot];
        newCard?.OnExpansionROMSelected();
    }

    /// <inheritdoc />
    public void DeselectExpansionSlot()
    {
        if (activeExpansionSlot.HasValue)
        {
            var card = slots[activeExpansionSlot.Value - MinSlot];
            card?.OnExpansionROMDeselected();
            activeExpansionSlot = null;
        }
    }

    /// <inheritdoc />
    public void HandleSlotROMAccess(Addr address)
    {
        // Extract slot number from address: $C100-$C7FF
        // Slot = (address >> 8) & 0x07
        int slot = (int)((address >> 8) & 0x07);

        // Validate slot is in range 1-7
        if (slot >= MinSlot && slot <= MaxSlot)
        {
            SelectExpansionSlot(slot);
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        // Deselect any active expansion ROM (revealing default)
        DeselectExpansionSlot();

        // Reset all installed cards
        for (int i = 0; i < SlotCount; i++)
        {
            slots[i]?.Reset();
        }
    }

    private static void ValidateSlotNumber(int slot)
    {
        if (slot < MinSlot || slot > MaxSlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                $"Slot number must be {MinSlot}-{MaxSlot}, was {slot}.");
        }
    }
}