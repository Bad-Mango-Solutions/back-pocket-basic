// <copyright file="IInternalRomHandler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Interface for managing internal ROM overlays in the slot ROM region ($C100-$C7FF).
/// </summary>
/// <remarks>
/// <para>
/// The Apple IIe provides soft switches to control internal ROM overlays:
/// </para>
/// <list type="bullet">
/// <item><description>INTCXROM ($C006/$C007): When ON, internal ROM overlays slot ROMs ($C100-$C7FF only)</description></item>
/// <item><description>INTC3ROM ($C00A/$C00B): When ON, internal 80-column firmware overlays slot 3 ($C300 region)</description></item>
/// </list>
/// <para>
/// IMPORTANT: These switches do NOT affect the expansion ROM region ($C800-$CFFF). The expansion
/// ROM is always controlled by slot selection. When INTC3ROM is enabled and $C300 is accessed,
/// the internal ROM provides the data BUT slot 3's expansion ROM is still selected at $C800-$CFFF.
/// </para>
/// <para>
/// This interface allows motherboard devices (like the Extended 80-Column Card) to register
/// their expansion ROM as the internal ROM and control the overlay switches.
/// </para>
/// </remarks>
public interface IInternalRomHandler
{
    /// <summary>
    /// Sets the internal ROM target for INTCXROM/INTC3ROM switching.
    /// </summary>
    /// <param name="rom">
    /// The internal ROM bus target that provides motherboard firmware at $C100-$C7FF.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rom"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// When INTCXROM is enabled, accesses to $C100-$C7FF read from this internal ROM
    /// instead of slot ROMs. The expansion ROM region ($C800-$CFFF) is NOT affected.
    /// </para>
    /// </remarks>
    void SetInternalRom(IBusTarget rom);

    /// <summary>
    /// Sets the INTCXROM state (internal ROM overlay for $C100-$C7FF).
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable internal ROM overlay for slot ROMs;
    /// <see langword="false"/> to allow slot ROMs to be visible.
    /// </param>
    /// <remarks>
    /// INTCXROM only affects the slot ROM region ($C100-$C7FF), not the expansion ROM ($C800-$CFFF).
    /// </remarks>
    void SetIntCxRom(bool enabled);

    /// <summary>
    /// Sets the INTC3ROM state (internal ROM overlay for $C300 region).
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable internal 80-column firmware at $C300;
    /// <see langword="false"/> to allow slot 3 ROM to be visible.
    /// </param>
    /// <remarks>
    /// <para>
    /// This setting typically defaults to ON, providing the internal 80-column firmware.
    /// When OFF, slot 3 can assert its own ROM at $C300.
    /// </para>
    /// <para>
    /// When INTC3ROM is enabled and $C300-$C3FF is accessed, the internal ROM provides
    /// the data, but expansion ROM selection for slot 3 still occurs, making the
    /// 80-column card's expansion ROM visible at $C800-$CFFF.
    /// </para>
    /// </remarks>
    void SetIntC3Rom(bool enabled);
}