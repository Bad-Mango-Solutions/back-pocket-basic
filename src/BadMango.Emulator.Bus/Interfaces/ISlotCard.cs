// <copyright file="ISlotCard.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// A peripheral that occupies an Apple II expansion slot (1-7).
/// </summary>
/// <remarks>
/// <para>
/// Slot cards have up to three memory-mapped regions:
/// </para>
/// <list type="bullet">
/// <item><description>I/O handlers ($C0n0-$C0nF): 16 bytes of device registers</description></item>
/// <item><description>Slot ROM ($Cn00-$CnFF): 256 bytes of identification/boot code</description></item>
/// <item><description>Expansion ROM ($C800-$CFFF): 2KB shared ROM, bank-selected</description></item>
/// </list>
/// </remarks>
public interface ISlotCard : IPeripheral
{
    /// <summary>
    /// Gets or sets the slot number this card is installed in (1-7).
    /// Set by the slot manager during installation.
    /// </summary>
    /// <value>The slot number, set by the slot manager during installation.</value>
    int SlotNumber { get; set; }

    /// <summary>
    /// Gets the I/O handlers for this card's 16-byte region ($C0n0-$C0nF).
    /// </summary>
    /// <value>
    /// The slot I/O handlers for read and write operations,
    /// or <see langword="null"/> if this card has no device I/O registers.
    /// </value>
    SlotIOHandlers? IOHandlers { get; }

    /// <summary>
    /// Gets the firmware ROM region handler ($Cn00-$CnFF).
    /// </summary>
    /// <value>
    /// The bus target for the slot ROM,
    /// or <see langword="null"/> if this card has no slot ROM.
    /// </value>
    IBusTarget? ROMRegion { get; }

    /// <summary>
    /// Gets the expansion ROM region handler ($C800-$CFFF when selected).
    /// </summary>
    /// <value>
    /// The bus target for the expansion ROM,
    /// or <see langword="null"/> if this card has no expansion ROM.
    /// </value>
    IBusTarget? ExpansionROMRegion { get; }

    /// <summary>
    /// Called when this card's expansion ROM becomes active.
    /// </summary>
    /// <remarks>
    /// This is called when another slot was deselected, or when this slot's
    /// ROM region ($Cn00-$CnFF) was accessed, making this card's expansion
    /// ROM visible at $C800-$CFFF.
    /// </remarks>
    void OnExpansionROMSelected();

    /// <summary>
    /// Called when this card's expansion ROM becomes inactive.
    /// </summary>
    /// <remarks>
    /// This is called when $CFFF is accessed (deselecting all expansion ROMs)
    /// or when another slot's ROM region is accessed.
    /// </remarks>
    void OnExpansionROMDeselected();
}