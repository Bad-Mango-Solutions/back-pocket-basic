// <copyright file="DebugCommandsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Signaling;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Debugging;

/// <summary>
/// Unit tests for the debug command handlers.
/// </summary>
/// <remarks>
/// <para>
/// This is the main partial class containing setup, teardown, and helper methods.
/// Individual command tests are in separate partial class files:
/// </para>
/// <list type="bullet">
/// <item><description><c>DebugCommandsTests.Regs.cs</c> - RegsCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Step.cs</c> - StepCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Run.cs</c> - RunCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Stop.cs</c> - StopCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Reset.cs</c> - ResetCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Pc.cs</c> - PcCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Mem.cs</c> - MemCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Poke.cs</c> - PokeCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Load.cs</c> - LoadCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Save.cs</c> - SaveCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Dasm.cs</c> - DasmCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.DebugContext.cs</c> - DebugContext tests</description></item>
/// <item><description><c>DebugCommandsTests.Regions.cs</c> - RegionsCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Pages.cs</c> - PagesCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Peek.cs</c> - PeekCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Read.cs</c> - ReadCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Write.cs</c> - WriteCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Profile.cs</c> - ProfileCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.DeviceMap.cs</c> - DeviceMapCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Fault.cs</c> - FaultCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.Switches.cs</c> - SwitchesCommand tests</description></item>
/// <item><description><c>DebugCommandsTests.BusLog.cs</c> - BusLogCommand tests</description></item>
/// </list>
/// </remarks>
[TestFixture]
public partial class DebugCommandsTests
{
    private CommandDispatcher dispatcher = null!;
    private MainBus bus = null!;
    private Cpu65C02 cpu = null!;
    private Disassembler disassembler = null!;
    private DebugContext debugContext = null!;
    private StringWriter outputWriter = null!;
    private StringWriter errorWriter = null!;

    /// <summary>
    /// Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        dispatcher = new CommandDispatcher();

        // Create a bus-based memory system
        bus = new MainBus(16); // 16-bit address space = 64KB
        var physical = new PhysicalMemory(0x10000, "test-ram");
        var target = new RamTarget(physical.Slice(0, 0x10000));
        bus.MapPageRange(
            startPage: 0,
            pageCount: 16,
            deviceId: 0,
            regionTag: RegionTag.Ram,
            perms: PagePerms.All,
            caps: target.Capabilities,
            target: target,
            physicalBase: 0);

        // Create event context and CPU directly (no adapter needed)
        var scheduler = new Scheduler();
        var signalBus = new SignalBus();
        var eventContext = new EventContext(scheduler, signalBus, bus);
        cpu = new Cpu65C02(eventContext);

        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
        disassembler = new Disassembler(opcodeTable, bus);

        outputWriter = new StringWriter();
        errorWriter = new StringWriter();
        debugContext = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler);

        // Set up reset vector so CPU can be reset properly
        WriteWord(bus, 0xFFFC, 0x1000);
        cpu.Reset();
    }

    /// <summary>
    /// Cleans up after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        outputWriter.Dispose();
        errorWriter.Dispose();
    }

    /// <summary>
    /// Helper method to write a byte to the bus.
    /// </summary>
    /// <param name="bus">The memory bus to write to.</param>
    /// <param name="address">The address to write to.</param>
    /// <param name="value">The byte value to write.</param>
    private static void WriteByte(IMemoryBus bus, uint address, byte value)
    {
        var access = new BusAccess(
            Address: address,
            Value: value,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugWrite,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
        bus.TryWrite8(access, value);
    }

    /// <summary>
    /// Helper method to read a byte from the bus.
    /// </summary>
    /// <param name="bus">The memory bus to read from.</param>
    /// <param name="address">The address to read from.</param>
    /// <returns>The byte value at the specified address.</returns>
    private static byte ReadByte(IMemoryBus bus, uint address)
    {
        var access = new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);
        return bus.TryRead8(access).Value;
    }

    /// <summary>
    /// Helper method to write a word (16-bit) to the bus.
    /// </summary>
    /// <param name="bus">The memory bus to write to.</param>
    /// <param name="address">The address to write to.</param>
    /// <param name="value">The 16-bit value to write (little-endian).</param>
    private static void WriteWord(IMemoryBus bus, uint address, ushort value)
    {
        WriteByte(bus, address, (byte)(value & 0xFF));
        WriteByte(bus, address + 1, (byte)(value >> 8));
    }

    /// <summary>
    /// Helper method to create a bus with full RAM mapping.
    /// </summary>
    /// <returns>A configured MainBus with 64KB RAM.</returns>
    private static MainBus CreateBusWithRam()
    {
        return CreateBusWithRam(out _);
    }

    /// <summary>
    /// Helper method to create a bus with full RAM mapping, returning the physical memory.
    /// </summary>
    /// <param name="physicalMemory">The physical memory backing store.</param>
    /// <returns>A configured MainBus with 64KB RAM.</returns>
    /// <remarks>
    /// Creates a 64KB address space (16-bit addressing) with 16 pages of 4KB each,
    /// all mapped to RAM with full read/write permissions.
    /// </remarks>
    private static MainBus CreateBusWithRam(out PhysicalMemory physicalMemory)
    {
        // 64KB address space: 16 pages × 4KB = 65536 bytes
        const int TestMemorySize = 0x10000;

        physicalMemory = new PhysicalMemory(TestMemorySize, "TestRam");
        var ramTarget = new RamTarget(physicalMemory.Slice(0, TestMemorySize));
        var bus = new MainBus(16); // 16-bit address space = 64KB

        // Map all pages to RAM with read/write/execute permissions
        bus.MapPageRange(
            startPage: 0,
            pageCount: 16,
            deviceId: 0,
            regionTag: RegionTag.Ram,
            perms: PagePerms.All,
            caps: ramTarget.Capabilities,
            target: ramTarget,
            physicalBase: 0);

        return bus;
    }
}