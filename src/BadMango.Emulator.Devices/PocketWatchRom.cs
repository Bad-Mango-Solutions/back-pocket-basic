// <copyright file="PocketWatchRom.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// ROM for the PocketWatch card providing Thunderclock-compatible identification.
/// </summary>
/// <remarks>
/// <para>
/// The PocketWatch ROM provides identification bytes and minimal boot code
/// for ProDOS and other software to recognize the card as a Thunderclock-compatible
/// real-time clock.
/// </para>
/// <para>
/// The 256-byte slot ROM is located at $Cn00-$CnFF (where n is the slot number).
/// </para>
/// </remarks>
internal sealed class PocketWatchRom : IBusTarget
{
    private readonly byte[] romData;

    /// <summary>
    /// Initializes a new instance of the <see cref="PocketWatchRom"/> class.
    /// </summary>
    public PocketWatchRom()
    {
        romData = new byte[256];

        // Fill with NOP instructions
        Array.Fill(romData, (byte)0xEA);

        // Thunderclock identification bytes
        // These identify the card to ProDOS and other software
        romData[0x00] = 0x08; // ProDOS clock driver signature byte 1
        romData[0x02] = 0x28; // ProDOS clock driver signature byte 2
        romData[0x04] = 0x58; // ProDOS clock driver signature byte 3
        romData[0x06] = 0x70; // ProDOS clock driver signature byte 4

        // Card type identification
        romData[0x05] = 0x00; // Card type (0 = Thunderclock Plus compatible)
        romData[0x07] = 0x00; // Revision

        // Simple RTS at common entry points
        romData[0xFB] = 0x60; // RTS at $CnFB
        romData[0xFC] = 0x60; // RTS at $CnFC
        romData[0xFD] = 0x60; // RTS at $CnFD
        romData[0xFE] = 0x60; // RTS at $CnFE
        romData[0xFF] = 0x60; // RTS at $CnFF
    }

    /// <inheritdoc />
    public TargetCaps Capabilities => TargetCaps.SupportsPeek;

    /// <inheritdoc />
    public byte Read8(uint physicalAddress, in BusAccess access)
        => romData[physicalAddress & 0xFF];

    /// <inheritdoc />
    public void Write8(uint physicalAddress, byte value, in BusAccess access)
    {
        // ROM is read-only
    }
}