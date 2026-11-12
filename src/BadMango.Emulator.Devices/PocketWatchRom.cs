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
    /// <summary>
    /// NOP instruction opcode (0xEA).
    /// </summary>
    private const byte NopInstruction = 0xEA;

    /// <summary>
    /// RTS instruction opcode (0x60).
    /// </summary>
    private const byte RtsInstruction = 0x60;

    private readonly byte[] romData;

    /// <summary>
    /// Initializes a new instance of the <see cref="PocketWatchRom"/> class.
    /// </summary>
    public PocketWatchRom()
    {
        romData = new byte[256];

        // Fill with NOP instructions
        Array.Fill(romData, NopInstruction);

        // Thunderclock identification bytes
        // These identify the card to ProDOS and other software
        romData[0x00] = 0x08; // ProDOS clock driver signature byte 1
        romData[0x02] = 0x28; // ProDOS clock driver signature byte 2
        romData[0x04] = 0x58; // ProDOS clock driver signature byte 3
        romData[0x06] = 0x70; // ProDOS clock driver signature byte 4

        // Card type identification
        romData[0x05] = 0x00; // Card type (0 = Thunderclock Plus compatible)
        romData[0x07] = 0x00; // Revision

        // Do not put RTS at end (would be executed on JMP to entry causing stack pop corruption to data bytes like DB).
        // Set boot indicator $CnFF=0 so slot scan treats as non-bootable.
        // Traps will be installed only at RTS offsets to skip them safely if PC ever lands here.
        romData[0xFF] = 0x00;
    }

    /// <inheritdoc />
    public TargetCaps Capabilities => TargetCaps.SupportsPeek;

    /// <inheritdoc />
    public byte Read8(uint offset, in BusAccess access)
        => romData[offset & 0xFF];

    /// <inheritdoc />
    public void Write8(uint offset, byte value, in BusAccess access)
    {
        // ROM is read-only
    }
}