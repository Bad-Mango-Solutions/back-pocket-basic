// <copyright file="CompositeIOTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using Interfaces;

/// <summary>
/// A composite I/O page handler that manages soft switches, slot ROM, and expansion ROM.
/// </summary>
/// <remarks>
/// <para>
/// This composite target handles a 4KB I/O page containing three distinct sub-regions:
/// </para>
/// <list type="bullet">
/// <item><description>$x000-$x0FF: Soft switches (handled by <see cref="IOPageDispatcher"/>)</description></item>
/// <item><description>$x100-$x7FF: Slot ROM (256 bytes per slot, access triggers expansion ROM selection)</description></item>
/// <item><description>$x800-$xFFF: Expansion ROM (2KB, banked from selected slot)</description></item>
/// </list>
/// <para>
/// This composite target routes accesses to the appropriate handler and manages the
/// expansion ROM selection protocol. It extends <see cref="CompositeTargetBase"/> to
/// inherit standard subregion management while providing custom I/O dispatch logic.
/// </para>
/// <para>
/// The Apple IIe provides INTCXROM and INTC3ROM soft switches to control internal ROM overlay:
/// </para>
/// <list type="bullet">
/// <item><description>INTCXROM: When ON, internal ROM overlays all slot ROMs ($x100-$xFFF)</description></item>
/// <item><description>INTC3ROM: When ON, internal 80-column firmware overlays slot 3 ($x300 region)</description></item>
/// </list>
/// <para>
/// Use the handler type "composite-io" in profile JSON to instantiate this target.
/// </para>
/// </remarks>
public sealed class CompositeIOTarget : CompositeTargetBase, IScheduledDevice
{
    /// <summary>
    /// Offset boundary for soft switch region ($x000-$x0FF).
    /// </summary>
    private const int SoftSwitchRegionEnd = 0x100;

    /// <summary>
    /// Offset boundary for slot ROM region ($x100-$x7FF).
    /// </summary>
    private const int SlotRomRegionEnd = 0x800;

    /// <summary>
    /// Offset within the 4KB page that triggers expansion ROM deselection.
    /// </summary>
    private const int ExpansionRomDeselectOffset = 0xFFF;

    /// <summary>
    /// Base offset for expansion ROM ($x800).
    /// </summary>
    private const int ExpansionRomBaseOffset = 0x800;

    private readonly IOPageDispatcher softSwitches;
    private readonly ISlotManager slotManager;

    private IBusTarget? internalRom;
    private bool intCxRomEnabled;
    private bool intC3RomEnabled = true; // Defaults to ON

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeIOTarget"/> class.
    /// </summary>
    /// <param name="name">The name of this composite I/O target.</param>
    /// <param name="softSwitches">The soft switch dispatcher for $x000-$x0FF.</param>
    /// <param name="slotManager">The slot manager for slot ROM and expansion ROM access.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/>, <paramref name="softSwitches"/>,
    /// or <paramref name="slotManager"/> is <see langword="null"/>.
    /// </exception>
    public CompositeIOTarget(string name, IOPageDispatcher softSwitches, ISlotManager slotManager)
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(softSwitches);
        ArgumentNullException.ThrowIfNull(slotManager);

        this.softSwitches = softSwitches;
        this.slotManager = slotManager;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The I/O page has side effects on most accesses and is timing-sensitive for
    /// accurate emulation of video and other peripherals. This combines the base
    /// class capabilities with I/O-specific flags.
    /// </remarks>
    public override TargetCaps Capabilities =>
        base.Capabilities | TargetCaps.HasSideEffects | TargetCaps.TimingSensitive;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte Read8(Addr physicalAddress, in BusAccess access)
    {
        ushort offset = (ushort)(physicalAddress & 0x0FFF);

        return offset switch
        {
            < SoftSwitchRegionEnd => softSwitches.Read((byte)offset, in access),
            < SlotRomRegionEnd => ReadSlotRom(offset, in access),
            _ => ReadExpansionRom(offset, in access),
        };
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        ushort offset = (ushort)(physicalAddress & 0x0FFF);

        switch (offset)
        {
            case < SoftSwitchRegionEnd:
                softSwitches.Write((byte)offset, value, in access);
                break;
            case < SlotRomRegionEnd:
                // Slot ROM: Ignore write, but still trigger expansion ROM selection!
                WriteSlotRom(offset);
                break;
            default:
                // Expansion ROM: Ignore write, but check for $xFFF deselection
                WriteExpansionRom(offset);
                break;
        }
    }

    /// <inheritdoc />
    public override IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
    {
        return offset switch
        {
            // Soft switches are handled directly by this target's Read8/Write8
            < SoftSwitchRegionEnd => this,
            < SlotRomRegionEnd => ResolveSlotRomTarget((ushort)offset),
            _ => ResolveExpansionRomTarget((ushort)offset),
        };
    }

    /// <inheritdoc />
    public override RegionTag GetSubRegionTag(Addr offset)
    {
        return offset switch
        {
            < SoftSwitchRegionEnd => RegionTag.Io,
            < SlotRomRegionEnd => RegionTag.Slot,
            _ => RegionTag.Rom,
        };
    }

    /// <inheritdoc />
    public override IEnumerable<(Addr StartOffset, Addr Size, RegionTag Tag, string TargetName)> EnumerateSubRegions()
    {
        // Return the fixed logical subregions of the I/O page
        yield return (0x000, 0x100, RegionTag.Io, "Soft Switches");

        // Slot ROM regions ($x100-$x7FF) - one per slot
        for (int slot = 1; slot <= 7; slot++)
        {
            Addr slotOffset = (Addr)(slot * 0x100);
            var slotRom = slotManager.GetSlotRomRegion(slot);
            string slotName = slotRom?.Name ?? $"Slot {slot} (empty)";
            yield return (slotOffset, 0x100, RegionTag.Slot, slotName);
        }

        // Expansion ROM region ($x800-$xFFF)
        int? activeSlot = slotManager.ActiveExpansionSlot;
        string expRomName = activeSlot.HasValue
            ? $"Expansion ROM (Slot {activeSlot.Value})"
            : "Expansion ROM (none selected)";
        yield return (0x800, 0x800, RegionTag.Rom, expRomName);
    }

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        // No initialization required for scheduled events.
        // This device doesn't schedule any recurring events.
    }

    /// <summary>
    /// Sets the INTCXROM state (internal ROM overlay for $x100-$xFFF).
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable internal ROM overlay for all slot ROMs;
    /// <see langword="false"/> to allow slot ROMs to be visible.
    /// </param>
    public void SetIntCxRom(bool enabled)
    {
        intCxRomEnabled = enabled;
    }

    /// <summary>
    /// Sets the INTC3ROM state (internal ROM overlay for $x300 region).
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable internal 80-column firmware at $x300;
    /// <see langword="false"/> to allow slot 3 ROM to be visible.
    /// </param>
    /// <remarks>
    /// This setting defaults to ON, providing the internal 80-column firmware.
    /// When OFF, slot 3 can assert its own ROM at $x300.
    /// </remarks>
    public void SetIntC3Rom(bool enabled)
    {
        intC3RomEnabled = enabled;
    }

    /// <summary>
    /// Sets the internal ROM target for INTCXROM/INTC3ROM switching.
    /// </summary>
    /// <param name="rom">
    /// The internal ROM bus target that provides motherboard firmware.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rom"/> is <see langword="null"/>.
    /// </exception>
    public void SetInternalRom(IBusTarget rom)
    {
        ArgumentNullException.ThrowIfNull(rom);
        internalRom = rom;
    }

    /// <summary>
    /// Reads from the slot ROM region ($x100-$x7FF).
    /// </summary>
    /// <param name="offset">Offset within the 4KB I/O page.</param>
    /// <param name="access">The bus access context.</param>
    /// <returns>The byte value at the specified offset.</returns>
    private byte ReadSlotRom(ushort offset, in BusAccess access)
    {
        // Extract slot number from address: $xn00 ? slot n
        int slot = (offset >> 8) & 0x07;

        // Check for INTCXROM override (internal ROM overlays all slot ROMs)
        if (intCxRomEnabled && internalRom is not null)
        {
            return internalRom.Read8(offset, in access);
        }

        // Check for INTC3ROM independent control of $x300 region
        if (slot == 3 && intC3RomEnabled && internalRom is not null)
        {
            return internalRom.Read8(offset, in access);
        }

        // Trigger expansion ROM selection for this slot (happens even if slot is empty)
        if (slot >= 1)
        {
            slotManager.SelectExpansionSlot(slot);
        }

        // Return ROM data if card has ROM, otherwise floating bus
        var rom = slotManager.GetSlotRomRegion(slot);
        if (rom is not null)
        {
            ushort romOffset = (ushort)(offset & 0x00FF);
            return rom.Read8(romOffset, in access);
        }

        return FloatingBusValue;
    }

    /// <summary>
    /// Handles writes to the slot ROM region ($x100-$x7FF).
    /// </summary>
    /// <param name="offset">Offset within the 4KB I/O page.</param>
    /// <remarks>
    /// Writes to ROM are ignored, but the access still triggers expansion ROM selection.
    /// </remarks>
    private void WriteSlotRom(ushort offset)
    {
        // Check for INTCXROM override - when enabled, no slot selection occurs
        if (intCxRomEnabled)
        {
            return;
        }

        // Extract slot number from address: $xn00 ? slot n
        int slot = (offset >> 8) & 0x07;

        // Check for INTC3ROM independent control of $x300 region
        if (slot == 3 && intC3RomEnabled)
        {
            return;
        }

        // Trigger expansion ROM selection for this slot
        if (slot >= 1)
        {
            slotManager.SelectExpansionSlot(slot);
        }
    }

    /// <summary>
    /// Reads from the expansion ROM region ($x800-$xFFF).
    /// </summary>
    /// <param name="offset">Offset within the 4KB I/O page.</param>
    /// <param name="access">The bus access context.</param>
    /// <returns>The byte value at the specified offset.</returns>
    private byte ReadExpansionRom(ushort offset, in BusAccess access)
    {
        // Special case: $xFFF deselects expansion ROM
        if (offset == ExpansionRomDeselectOffset)
        {
            slotManager.DeselectExpansionSlot();
            return FloatingBusValue;
        }

        // Check for INTCXROM override (internal ROM overlays expansion ROM region too)
        if (intCxRomEnabled && internalRom is not null)
        {
            return internalRom.Read8(offset, in access);
        }

        // Return data from selected slot's expansion ROM
        int? activeSlot = slotManager.ActiveExpansionSlot;
        if (activeSlot is { } slot)
        {
            var expRom = slotManager.GetExpansionRomRegion(slot);
            if (expRom is not null)
            {
                ushort expOffset = (ushort)(offset - ExpansionRomBaseOffset);
                return expRom.Read8(expOffset, in access);
            }
        }

        // No expansion ROM selected or slot has no expansion ROM
        return FloatingBusValue;
    }

    /// <summary>
    /// Handles writes to the expansion ROM region ($x800-$xFFF).
    /// </summary>
    /// <param name="offset">Offset within the 4KB I/O page.</param>
    /// <remarks>
    /// Writes to ROM are ignored, but $xFFF still triggers deselection.
    /// </remarks>
    private void WriteExpansionRom(ushort offset)
    {
        // Special case: $xFFF deselects expansion ROM even on writes
        if (offset == ExpansionRomDeselectOffset)
        {
            slotManager.DeselectExpansionSlot();
        }
    }

    /// <summary>
    /// Resolves the target for slot ROM accesses.
    /// </summary>
    /// <param name="offset">Offset within the 4KB I/O page.</param>
    /// <returns>The bus target for the slot ROM, or <see langword="null"/> for floating bus.</returns>
    private IBusTarget? ResolveSlotRomTarget(ushort offset)
    {
        // Check for INTCXROM override
        if (intCxRomEnabled && internalRom is not null)
        {
            return internalRom;
        }

        int slot = (offset >> 8) & 0x07;

        // Check for INTC3ROM independent control of $x300 region
        if (slot == 3 && intC3RomEnabled && internalRom is not null)
        {
            return internalRom;
        }

        return slotManager.GetSlotRomRegion(slot);
    }

    /// <summary>
    /// Resolves the target for expansion ROM accesses.
    /// </summary>
    /// <param name="offset">Offset within the 4KB I/O page.</param>
    /// <returns>The bus target for the expansion ROM, or <see langword="null"/> for floating bus.</returns>
    private IBusTarget? ResolveExpansionRomTarget(ushort offset)
    {
        // $xFFF always returns null (floating bus after deselection)
        if (offset == ExpansionRomDeselectOffset)
        {
            return null;
        }

        // Check for INTCXROM override
        if (intCxRomEnabled && internalRom is not null)
        {
            return internalRom;
        }

        int? activeSlot = slotManager.ActiveExpansionSlot;
        if (activeSlot is { } slot)
        {
            return slotManager.GetExpansionRomRegion(slot);
        }

        return null;
    }
}