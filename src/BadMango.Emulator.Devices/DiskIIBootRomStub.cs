// <copyright file="DiskIIBootRomStub.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Stub boot ROM for Disk II controller ($Cn00-$CnFF).
/// </summary>
/// <remarks>
/// <para>
/// This is a minimal implementation for testing slot infrastructure.
/// It provides the standard Apple II peripheral card identification bytes
/// and a simple ROM that returns to the caller.
/// </para>
/// <para>
/// The real Disk II boot ROM contains the boot code that loads the boot sector
/// from the disk into memory and transfers control to it.
/// </para>
/// <para>
/// This implementation uses <see cref="RomTarget"/> backed by <see cref="PhysicalMemory"/>
/// to provide proper ROM semantics including debug write support and atomic wide access.
/// </para>
/// </remarks>
public sealed class DiskIIBootRomStub : IBusTarget
{
    private const int RomSize = 256;
    private readonly PhysicalMemory physicalMemory;
    private readonly RomTarget romTarget;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskIIBootRomStub"/> class.
    /// </summary>
    public DiskIIBootRomStub()
    {
        // Create physical memory backing and initialize with $FF
        var romData = new byte[RomSize];
        Array.Fill(romData, (byte)0xFF);

        // Standard Apple II peripheral card identification bytes
        // These allow ProDOS and other software to identify the card type
        romData[0x05] = 0x38;  // SEC - Standard ID byte 1
        romData[0x07] = 0x18;  // CLC - Standard ID byte 2
        romData[0x0B] = 0x00;  // Device type: 0 = Disk II
        romData[0x0C] = 0x20;  // JSR instruction (identifies as bootable)

        // Simple boot code that just returns
        // In a real implementation, this would load and execute the boot sector
        romData[0x00] = 0x60;  // RTS - Return to caller

        physicalMemory = new PhysicalMemory(romData, "DiskII Boot ROM");
        romTarget = new RomTarget(physicalMemory.Slice(0, RomSize));
    }

    /// <inheritdoc />
    public TargetCaps Capabilities => romTarget.Capabilities;

    /// <inheritdoc />
    public byte Read8(uint offset, in BusAccess context)
    {
        return romTarget.Read8(offset & 0xFF, in context);
    }

    /// <inheritdoc />
    public void Write8(uint offset, byte value, in BusAccess context)
    {
        romTarget.Write8(offset & 0xFF, value, in context);
    }

    /// <inheritdoc />
    public ushort Read16(uint offset, in BusAccess context)
    {
        return romTarget.Read16(offset & 0xFF, in context);
    }

    /// <inheritdoc />
    public void Write16(uint offset, ushort value, in BusAccess context)
    {
        romTarget.Write16(offset & 0xFF, value, in context);
    }

    /// <inheritdoc />
    public uint Read32(uint offset, in BusAccess context)
    {
        return romTarget.Read32(offset & 0xFF, in context);
    }

    /// <inheritdoc />
    public void Write32(uint offset, uint value, in BusAccess context)
    {
        romTarget.Write32(offset & 0xFF, value, in context);
    }
}