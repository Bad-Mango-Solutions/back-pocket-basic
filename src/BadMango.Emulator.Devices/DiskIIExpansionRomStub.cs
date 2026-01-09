// <copyright file="DiskIIExpansionRomStub.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Stub expansion ROM for Disk II controller ($C800-$CFFF when selected).
/// </summary>
/// <remarks>
/// <para>
/// This is a minimal implementation for testing slot infrastructure.
/// The Disk II expansion ROM contains the disk controller firmware including
/// read/write routines and format code.
/// </para>
/// <para>
/// The real expansion ROM is 2KB and contains the RWTS (Read/Write Track/Sector)
/// routines used by DOS 3.3 and ProDOS for disk access.
/// </para>
/// <para>
/// This implementation uses <see cref="RomTarget"/> backed by <see cref="PhysicalMemory"/>
/// to provide proper ROM semantics including debug write support and atomic wide access.
/// </para>
/// </remarks>
public sealed class DiskIIExpansionRomStub : IBusTarget
{
    private const int RomSize = 2048;
    private readonly PhysicalMemory physicalMemory;
    private readonly RomTarget romTarget;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskIIExpansionRomStub"/> class.
    /// </summary>
    public DiskIIExpansionRomStub()
    {
        // Create physical memory backing and initialize with $FF
        var romData = new byte[RomSize];
        Array.Fill(romData, (byte)0xFF);

        // In a real implementation, this would contain the P5/P6 PROM data
        // for disk read/write operations. For the stub, we just fill with
        // a simple pattern that can be detected in tests.
        romData[0x00] = 0xA9;  // LDA immediate
        romData[0x01] = 0x00;  // #$00
        romData[0x02] = 0x60;  // RTS

        physicalMemory = new(romData, "DiskII Expansion ROM");
        romTarget = new(physicalMemory.Slice(0, RomSize));
    }

    /// <inheritdoc />
    public TargetCaps Capabilities => romTarget.Capabilities;

    /// <inheritdoc />
    public byte Read8(uint offset, in BusAccess context)
    {
        return romTarget.Read8(offset & 0x7FF, in context);
    }

    /// <inheritdoc />
    public void Write8(uint offset, byte value, in BusAccess context)
    {
        romTarget.Write8(offset & 0x7FF, value, in context);
    }

    /// <inheritdoc />
    public ushort Read16(uint offset, in BusAccess context)
    {
        return romTarget.Read16(offset & 0x7FF, in context);
    }

    /// <inheritdoc />
    public void Write16(uint offset, ushort value, in BusAccess context)
    {
        romTarget.Write16(offset & 0x7FF, value, in context);
    }

    /// <inheritdoc />
    public uint Read32(uint offset, in BusAccess context)
    {
        return romTarget.Read32(offset & 0x7FF, in context);
    }

    /// <inheritdoc />
    public void Write32(uint offset, uint value, in BusAccess context)
    {
        romTarget.Write32(offset & 0x7FF, value, in context);
    }
}