// <copyright file="IInternalRomHandler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Interface for managing internal ROM overlays in the I/O region ($C100-$CFFF).
/// </summary>
/// <remarks>
/// <para>
/// The Apple IIe provides soft switches to control internal ROM overlays:
/// </para>
/// <list type="bullet">
/// <item><description>INTCXROM ($C006/$C007): When ON, internal ROM overlays all slot ROMs ($C100-$CFFF)</description></item>
/// <item><description>INTC3ROM ($C00A/$C00B): When ON, internal 80-column firmware overlays slot 3 ($C300 region)</description></item>
/// </list>
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
    /// The internal ROM bus target that provides motherboard firmware at $C100-$CFFF.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rom"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// When INTCXROM is enabled, accesses to $C100-$CFFF read from this internal ROM
    /// instead of slot ROMs or expansion ROMs.
    /// </para>
    /// </remarks>
    void SetInternalRom(IBusTarget rom);

    /// <summary>
    /// Sets the INTCXROM state (internal ROM overlay for $C100-$CFFF).
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable internal ROM overlay for all slot ROMs;
    /// <see langword="false"/> to allow slot ROMs to be visible.
    /// </param>
    void SetIntCxRom(bool enabled);

    /// <summary>
    /// Sets the INTC3ROM state (internal ROM overlay for $C300 region).
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable internal 80-column firmware at $C300;
    /// <see langword="false"/> to allow slot 3 ROM to be visible.
    /// </param>
    /// <remarks>
    /// This setting typically defaults to ON, providing the internal 80-column firmware.
    /// When OFF, slot 3 can assert its own ROM at $C300.
    /// </remarks>
    void SetIntC3Rom(bool enabled);
}