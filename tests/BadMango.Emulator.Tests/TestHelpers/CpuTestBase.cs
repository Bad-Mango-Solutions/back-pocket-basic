// <copyright file="CpuTestBase.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests.TestHelpers;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Signaling;
using BadMango.Emulator.Emulation.Cpu;

/// <summary>
/// Base class for CPU tests that provides a bus-based memory infrastructure.
/// </summary>
public abstract class CpuTestBase
{
    /// <summary>
    /// Gets the memory bus.
    /// </summary>
    protected IMemoryBus Bus { get; private set; } = null!;

    /// <summary>
    /// Gets the physical memory for direct access in tests.
    /// </summary>
    protected PhysicalMemory PhysicalMemory { get; private set; } = null!;

    /// <summary>
    /// Gets the CPU.
    /// </summary>
    protected Cpu65C02 Cpu { get; private set; } = null!;

    /// <summary>
    /// Sets up the bus-based test infrastructure.
    /// </summary>
    [SetUp]
    public void SetUpBusInfrastructure()
    {
        // Create bus and physical memory
        var mainBus = new MainBus(16); // 16-bit address space = 64KB
        PhysicalMemory = new(0x10000, "test-ram");
        var target = new RamTarget(PhysicalMemory.Slice(0, 0x10000));
        mainBus.MapPageRange(
            startPage: 0,
            pageCount: 16,
            deviceId: 0,
            regionTag: RegionTag.Ram,
            perms: PagePerms.All,
            caps: target.Capabilities,
            target: target,
            physicalBase: 0);

        Bus = mainBus;

        // Create event context and CPU
        var scheduler = new Scheduler();
        var signalBus = new SignalBus();
        var eventContext = new EventContext(scheduler, signalBus, mainBus);
        Cpu = new(eventContext);
    }

    /// <summary>
    /// Writes a byte to memory using the bus.
    /// </summary>
    /// <param name="address">The address to write to.</param>
    /// <param name="value">The value to write.</param>
    protected void Write(uint address, byte value)
    {
        var access = new BusAccess(
            Address: address,
            Value: value,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataWrite,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
        Bus.TryWrite8(access, value);
    }

    /// <summary>
    /// Writes a 16-bit word to memory using the bus.
    /// </summary>
    /// <param name="address">The address to write to.</param>
    /// <param name="value">The value to write.</param>
    protected void WriteWord(uint address, ushort value)
    {
        var access = new BusAccess(
            Address: address,
            Value: value,
            WidthBits: 16,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataWrite,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
        Bus.TryWrite16(access, value);
    }

    /// <summary>
    /// Reads a byte from memory using the bus.
    /// </summary>
    /// <param name="address">The address to read from.</param>
    /// <returns>The byte value at the address.</returns>
    protected byte Read(uint address)
    {
        var access = new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
        var result = Bus.TryRead8(access);
        return result.Value;
    }
}