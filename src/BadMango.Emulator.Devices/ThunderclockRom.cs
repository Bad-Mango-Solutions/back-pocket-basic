// <copyright file="ThunderclockRom.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Thunderclock slot ROM with identification bytes.
/// </summary>
/// <remarks>
/// <para>
/// This ROM provides the standard ProDOS-compatible identification bytes
/// that allow the operating system to detect and use the Thunderclock card.
/// </para>
/// <para>
/// The ROM contains minimal code - primarily identification signatures at
/// standard offsets that ProDOS checks when scanning for clock devices.
/// </para>
/// </remarks>
internal sealed class ThunderclockRom : IBusTarget
{
    private const int RomSize = 256;
    private readonly byte[] rom = new byte[RomSize];

    /// <summary>
    /// Initializes a new instance of the <see cref="ThunderclockRom"/> class.
    /// </summary>
    public ThunderclockRom()
    {
        // Standard slot identification bytes
        // ProDOS looks for specific signatures
        rom[0x05] = 0x38;  // SEC
        rom[0x07] = 0x18;  // CLC
        rom[0x0B] = 0x01;  // Device type (clock)
        rom[0x0C] = 0x20;  // JSR

        // Fill rest with RTS for safety
        for (int i = 0; i < RomSize; i++)
        {
            if (rom[i] == 0)
            {
                rom[i] = 0x60;  // RTS
            }
        }
    }

    /// <inheritdoc />
    public TargetCaps Capabilities => TargetCaps.SupportsPeek;

    /// <inheritdoc />
    public byte Read8(uint physicalAddress, in BusAccess access)
        => rom[physicalAddress & 0xFF];

    /// <inheritdoc />
    public void Write8(uint physicalAddress, byte value, in BusAccess access)
    {
        // ROM ignores writes
    }
}