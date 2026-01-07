// <copyright file="DebugCommandsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Core.Signaling;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Debugging;

using Bus.Interfaces;

using Moq;

/// <summary>
/// Unit tests for the debug command handlers.
/// </summary>
[TestFixture]
public class DebugCommandsTests
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
    private static void WriteWord(IMemoryBus bus, uint address, ushort value)
    {
        WriteByte(bus, address, (byte)(value & 0xFF));
        WriteByte(bus, address + 1, (byte)(value >> 8));
    }

    // =====================
    // RegsCommand Tests
    // =====================

    /// <summary>
    /// Verifies that RegsCommand has correct name.
    /// </summary>
    [Test]
    public void RegsCommand_HasCorrectName()
    {
        var command = new RegsCommand();
        Assert.That(command.Name, Is.EqualTo("regs"));
    }

    /// <summary>
    /// Verifies that RegsCommand has correct aliases.
    /// </summary>
    [Test]
    public void RegsCommand_HasCorrectAliases()
    {
        var command = new RegsCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "r", "registers" }));
    }

    /// <summary>
    /// Verifies that RegsCommand displays registers when CPU is attached.
    /// </summary>
    [Test]
    public void RegsCommand_DisplaysRegisters_WhenCpuAttached()
    {
        var command = new RegsCommand();

        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("CPU Registers"));
            Assert.That(outputWriter.ToString(), Does.Contain("PC"));
            Assert.That(outputWriter.ToString(), Does.Contain("SP"));
        });
    }

    /// <summary>
    /// Verifies that RegsCommand returns error when CPU is not attached.
    /// </summary>
    [Test]
    public void RegsCommand_ReturnsError_WhenNoCpuAttached()
    {
        var contextWithoutCpu = new DebugContext(dispatcher, outputWriter, errorWriter);
        var command = new RegsCommand();

        var result = command.Execute(contextWithoutCpu, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("No CPU attached"));
        });
    }

    // =====================
    // StepCommand Tests
    // =====================

    /// <summary>
    /// Verifies that StepCommand has correct name.
    /// </summary>
    [Test]
    public void StepCommand_HasCorrectName()
    {
        var command = new StepCommand();
        Assert.That(command.Name, Is.EqualTo("step"));
    }

    /// <summary>
    /// Verifies that StepCommand has correct aliases.
    /// </summary>
    [Test]
    public void StepCommand_HasCorrectAliases()
    {
        var command = new StepCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "s", "si" }));
    }

    /// <summary>
    /// Verifies that StepCommand executes single instruction by default.
    /// </summary>
    [Test]
    public void StepCommand_ExecutesSingleInstruction_ByDefault()
    {
        // Write a NOP instruction at PC
        WriteByte(bus, 0x1000, 0xEA); // NOP
        cpu.Reset();

        var command = new StepCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Executed 1 instruction"));
            Assert.That(cpu.GetPC(), Is.EqualTo(0x1001u));
        });
    }

    /// <summary>
    /// Verifies that StepCommand executes multiple instructions when count specified.
    /// </summary>
    [Test]
    public void StepCommand_ExecutesMultipleInstructions_WhenCountSpecified()
    {
        // Write NOP instructions at PC
        WriteByte(bus, 0x1000, 0xEA); // NOP
        WriteByte(bus, 0x1001, 0xEA); // NOP
        WriteByte(bus, 0x1002, 0xEA); // NOP
        cpu.Reset();

        var command = new StepCommand();
        var result = command.Execute(debugContext, ["3"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Executed 3 instruction"));
            Assert.That(cpu.GetPC(), Is.EqualTo(0x1003u));
        });
    }

    /// <summary>
    /// Verifies that StepCommand returns error when CPU is halted.
    /// </summary>
    [Test]
    public void StepCommand_ReturnsError_WhenCpuHalted()
    {
        // Write STP instruction which halts the CPU
        WriteByte(bus, 0x1000, 0xDB); // STP
        cpu.Reset();
        cpu.Step(); // Execute STP to halt CPU

        var command = new StepCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("halted"));
        });
    }

    // =====================
    // RunCommand Tests
    // =====================

    /// <summary>
    /// Verifies that RunCommand has correct name.
    /// </summary>
    [Test]
    public void RunCommand_HasCorrectName()
    {
        var command = new RunCommand();
        Assert.That(command.Name, Is.EqualTo("run"));
    }

    /// <summary>
    /// Verifies that RunCommand has correct aliases.
    /// </summary>
    [Test]
    public void RunCommand_HasCorrectAliases()
    {
        var command = new RunCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "g", "go" }));
    }

    /// <summary>
    /// Verifies that RunCommand runs until CPU halts.
    /// </summary>
    [Test]
    public void RunCommand_RunsUntilHalt()
    {
        // Write a few NOPs then STP
        WriteByte(bus, 0x1000, 0xEA); // NOP
        WriteByte(bus, 0x1001, 0xEA); // NOP
        WriteByte(bus, 0x1002, 0xDB); // STP
        cpu.Reset();

        var command = new RunCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("CPU halted"));
            Assert.That(cpu.Halted, Is.True);
        });
    }

    /// <summary>
    /// Verifies that RunCommand respects instruction limit.
    /// </summary>
    [Test]
    public void RunCommand_RespectsInstructionLimit()
    {
        // Write infinite NOP loop
        WriteByte(bus, 0x1000, 0xEA); // NOP
        WriteByte(bus, 0x1001, 0x4C); // JMP $1000
        WriteByte(bus, 0x1002, 0x00);
        WriteByte(bus, 0x1003, 0x10);
        cpu.Reset();

        var command = new RunCommand();
        var result = command.Execute(debugContext, ["10"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("instruction limit"));
        });
    }

    // =====================
    // StopCommand Tests
    // =====================

    /// <summary>
    /// Verifies that StopCommand has correct name.
    /// </summary>
    [Test]
    public void StopCommand_HasCorrectName()
    {
        var command = new StopCommand();
        Assert.That(command.Name, Is.EqualTo("stop"));
    }

    /// <summary>
    /// Verifies that StopCommand requests CPU to stop.
    /// </summary>
    [Test]
    public void StopCommand_RequestsStop()
    {
        var command = new StopCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(cpu.IsStopRequested, Is.True);
        });
    }

    // =====================
    // ResetCommand Tests
    // =====================

    /// <summary>
    /// Verifies that ResetCommand has correct name.
    /// </summary>
    [Test]
    public void ResetCommand_HasCorrectName()
    {
        var command = new ResetCommand();
        Assert.That(command.Name, Is.EqualTo("reset"));
    }

    /// <summary>
    /// Verifies that ResetCommand performs soft reset by default.
    /// </summary>
    [Test]
    public void ResetCommand_PerformsSoftReset_ByDefault()
    {
        // Change PC to different value
        cpu.SetPC(0x5000);

        var command = new ResetCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(cpu.GetPC(), Is.EqualTo(0x1000u)); // Reset vector
            Assert.That(outputWriter.ToString(), Does.Contain("Soft reset"));
        });
    }

    /// <summary>
    /// Verifies that ResetCommand performs hard reset when flag specified.
    /// </summary>
    [Test]
    public void ResetCommand_PerformsHardReset_WhenFlagSpecified()
    {
        // Write some data to memory
        WriteByte(bus, 0x0200, 0xFF);

        // Need to re-set reset vector after clear
        WriteWord(bus, 0xFFFC, 0x1000);

        var command = new ResetCommand();
        var result = command.Execute(debugContext, ["--hard"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Hard reset"));
        });
    }

    // =====================
    // PcCommand Tests
    // =====================

    /// <summary>
    /// Verifies that PcCommand has correct name.
    /// </summary>
    [Test]
    public void PcCommand_HasCorrectName()
    {
        var command = new PcCommand();
        Assert.That(command.Name, Is.EqualTo("pc"));
    }

    /// <summary>
    /// Verifies that PcCommand displays current PC when called without arguments.
    /// </summary>
    [Test]
    public void PcCommand_DisplaysCurrentPc_WithoutArguments()
    {
        var command = new PcCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("PC = $1000"));
        });
    }

    /// <summary>
    /// Verifies that PcCommand sets PC when address specified.
    /// </summary>
    [Test]
    public void PcCommand_SetsPc_WhenAddressSpecified()
    {
        var command = new PcCommand();
        var result = command.Execute(debugContext, ["$2000"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(cpu.GetPC(), Is.EqualTo(0x2000u));
            Assert.That(outputWriter.ToString(), Does.Contain("PC set to $2000"));
        });
    }

    /// <summary>
    /// Verifies that PcCommand accepts 0x hex format.
    /// </summary>
    [Test]
    public void PcCommand_AcceptsHexFormat()
    {
        var command = new PcCommand();
        var result = command.Execute(debugContext, ["0x3000"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(cpu.GetPC(), Is.EqualTo(0x3000u));
        });
    }

    // =====================
    // MemCommand Tests
    // =====================

    /// <summary>
    /// Verifies that MemCommand has correct name.
    /// </summary>
    [Test]
    public void MemCommand_HasCorrectName()
    {
        var command = new MemCommand();
        Assert.That(command.Name, Is.EqualTo("mem"));
    }

    /// <summary>
    /// Verifies that MemCommand has correct aliases.
    /// </summary>
    [Test]
    public void MemCommand_HasCorrectAliases()
    {
        var command = new MemCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "m", "dump", "hexdump" }));
    }

    /// <summary>
    /// Verifies that MemCommand displays memory contents.
    /// </summary>
    [Test]
    public void MemCommand_DisplaysMemoryContents()
    {
        // Write some known values
        WriteByte(bus, 0x0200, 0x41); // 'A'
        WriteByte(bus, 0x0201, 0x42); // 'B'
        WriteByte(bus, 0x0202, 0x43); // 'C'

        var command = new MemCommand();
        var result = command.Execute(debugContext, ["$0200", "16"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("41"));
            Assert.That(outputWriter.ToString(), Does.Contain("42"));
            Assert.That(outputWriter.ToString(), Does.Contain("43"));
            Assert.That(outputWriter.ToString(), Does.Contain("ABC")); // ASCII
        });
    }

    /// <summary>
    /// Verifies that MemCommand returns error when address missing.
    /// </summary>
    [Test]
    public void MemCommand_ReturnsError_WhenAddressMissing()
    {
        var command = new MemCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Address required"));
        });
    }

    // =====================
    // PokeCommand Tests
    // =====================

    /// <summary>
    /// Verifies that PokeCommand has correct name.
    /// </summary>
    [Test]
    public void PokeCommand_HasCorrectName()
    {
        var command = new PokeCommand();
        Assert.That(command.Name, Is.EqualTo("poke"));
    }

    /// <summary>
    /// Verifies that PokeCommand writes single byte.
    /// </summary>
    [Test]
    public void PokeCommand_WritesSingleByte()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0300", "$AB"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0300), Is.EqualTo(0xAB));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand writes multiple bytes.
    /// </summary>
    [Test]
    public void PokeCommand_WritesMultipleBytes()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0400", "$11", "$22", "$33"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0400), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0401), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0402), Is.EqualTo(0x33));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand returns error when address missing.
    /// </summary>
    [Test]
    public void PokeCommand_ReturnsError_WhenAddressMissing()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Address required"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand accepts unprefixed hex bytes.
    /// </summary>
    [Test]
    public void PokeCommand_AcceptsUnprefixedHexBytes()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0900", "ab", "cd", "ef"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0900), Is.EqualTo(0xAB));
            Assert.That(ReadByte(bus, 0x0901), Is.EqualTo(0xCD));
            Assert.That(ReadByte(bus, 0x0902), Is.EqualTo(0xEF));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand accepts mixed prefixed and unprefixed bytes.
    /// </summary>
    [Test]
    public void PokeCommand_AcceptsMixedPrefixedAndUnprefixedBytes()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0950", "$11", "22", "0x33"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0950), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0951), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0952), Is.EqualTo(0x33));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode writes bytes from input.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_WritesBytesFromInput()
    {
        // Set up input with some hex bytes and blank line to finish
        var inputText = "AA BB CC\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0500", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0500), Is.EqualTo(0xAA));
            Assert.That(ReadByte(bus, 0x0501), Is.EqualTo(0xBB));
            Assert.That(ReadByte(bus, 0x0502), Is.EqualTo(0xCC));
            Assert.That(outputWriter.ToString(), Does.Contain("Interactive poke mode"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode handles multiple lines.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_HandlesMultipleLines()
    {
        var inputText = "11 22\n33 44\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0600", "--interactive"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0600), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0601), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0602), Is.EqualTo(0x33));
            Assert.That(ReadByte(bus, 0x0603), Is.EqualTo(0x44));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode exits on empty line.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_ExitsOnEmptyLine()
    {
        var inputText = "55\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0700", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0700), Is.EqualTo(0x55));
            Assert.That(outputWriter.ToString(), Does.Contain("Interactive mode complete"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode returns error when no input available.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_ReturnsError_WhenNoInputAvailable()
    {
        // Create context without input reader
        var contextWithoutInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, null);

        var command = new PokeCommand();
        var result = command.Execute(contextWithoutInput, ["$0800", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Interactive mode not available"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode supports address prefix to change write location.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_SupportsAddressPrefix()
    {
        // Start at $0A00, then change to $0B00
        var inputText = "11 22\n$0B00: 33 44\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0A00", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0A00), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0A01), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0B00), Is.EqualTo(0x33));
            Assert.That(ReadByte(bus, 0x0B01), Is.EqualTo(0x44));
            Assert.That(outputWriter.ToString(), Does.Contain("Address changed to $0B00"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode supports address-only line to change location.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_SupportsAddressOnlyLine()
    {
        // Start at $0C00, change to $0D00, then write
        var inputText = "$0D00:\n55 66\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0C00", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0D00), Is.EqualTo(0x55));
            Assert.That(ReadByte(bus, 0x0D01), Is.EqualTo(0x66));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode with 0x address prefix works.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_Supports0xAddressPrefix()
    {
        var inputText = "0x0E00: 77 88\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0100", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0E00), Is.EqualTo(0x77));
            Assert.That(ReadByte(bus, 0x0E01), Is.EqualTo(0x88));
        });
    }

    // =====================
    // LoadCommand Tests
    // =====================

    /// <summary>
    /// Verifies that LoadCommand has correct name.
    /// </summary>
    [Test]
    public void LoadCommand_HasCorrectName()
    {
        var command = new LoadCommand();
        Assert.That(command.Name, Is.EqualTo("load"));
    }

    /// <summary>
    /// Verifies that LoadCommand returns error when file not found.
    /// </summary>
    [Test]
    public void LoadCommand_ReturnsError_WhenFileNotFound()
    {
        var command = new LoadCommand();
        var result = command.Execute(debugContext, ["nonexistent.bin"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("File not found"));
        });
    }

    /// <summary>
    /// Verifies that LoadCommand returns error when filename missing.
    /// </summary>
    [Test]
    public void LoadCommand_ReturnsError_WhenFilenameMissing()
    {
        var command = new LoadCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Filename required"));
        });
    }

    // =====================
    // SaveCommand Tests
    // =====================

    /// <summary>
    /// Verifies that SaveCommand has correct name.
    /// </summary>
    [Test]
    public void SaveCommand_HasCorrectName()
    {
        var command = new SaveCommand();
        Assert.That(command.Name, Is.EqualTo("save"));
    }

    /// <summary>
    /// Verifies that SaveCommand returns error when arguments missing.
    /// </summary>
    [Test]
    public void SaveCommand_ReturnsError_WhenArgumentsMissing()
    {
        var command = new SaveCommand();
        var result = command.Execute(debugContext, ["test.bin"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Filename, address, and length required"));
        });
    }

    // =====================
    // DasmCommand Tests
    // =====================

    /// <summary>
    /// Verifies that DasmCommand has correct name.
    /// </summary>
    [Test]
    public void DasmCommand_HasCorrectName()
    {
        var command = new DasmCommand();
        Assert.That(command.Name, Is.EqualTo("dasm"));
    }

    /// <summary>
    /// Verifies that DasmCommand has correct aliases.
    /// </summary>
    [Test]
    public void DasmCommand_HasCorrectAliases()
    {
        var command = new DasmCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "d", "disasm", "u", "unassemble" }));
    }

    /// <summary>
    /// Verifies that DasmCommand disassembles memory at current PC by default.
    /// </summary>
    [Test]
    public void DasmCommand_DisassemblesAtCurrentPc_ByDefault()
    {
        // Write NOP instruction at PC
        WriteByte(bus, 0x1000, 0xEA); // NOP

        var command = new DasmCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("$1000"));
            Assert.That(outputWriter.ToString(), Does.Contain("NOP"));
        });
    }

    /// <summary>
    /// Verifies that DasmCommand disassembles at specified address.
    /// </summary>
    [Test]
    public void DasmCommand_DisassemblesAtSpecifiedAddress()
    {
        // Write LDA #$42 at $2000
        WriteByte(bus, 0x2000, 0xA9); // LDA immediate
        WriteByte(bus, 0x2001, 0x42); // #$42

        var command = new DasmCommand();
        var result = command.Execute(debugContext, ["$2000"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("$2000"));
            Assert.That(outputWriter.ToString(), Does.Contain("LDA"));
        });
    }

    /// <summary>
    /// Verifies that DasmCommand returns error when no disassembler attached.
    /// </summary>
    [Test]
    public void DasmCommand_ReturnsError_WhenNoDisassemblerAttached()
    {
        var contextWithoutDisasm = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, null);
        var command = new DasmCommand();

        var result = command.Execute(contextWithoutDisasm, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("No disassembler attached"));
        });
    }

    // =====================
    // DebugContext Tests
    // =====================

    /// <summary>
    /// Verifies that DebugContext reports system attached correctly.
    /// </summary>
    [Test]
    public void DebugContext_ReportsSystemAttached_WhenAllComponentsPresent()
    {
        Assert.That(debugContext.IsSystemAttached, Is.True);
    }

    /// <summary>
    /// Verifies that DebugContext reports system not attached when CPU missing.
    /// </summary>
    [Test]
    public void DebugContext_ReportsNotAttached_WhenCpuMissing()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.That(context.IsSystemAttached, Is.False);
    }

    /// <summary>
    /// Verifies that DebugContext can attach components dynamically.
    /// </summary>
    [Test]
    public void DebugContext_CanAttachComponentsDynamically()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.That(context.IsSystemAttached, Is.False);

        context.AttachSystem(cpu, bus, disassembler);
        Assert.That(context.IsSystemAttached, Is.True);
    }

    /// <summary>
    /// Verifies that DebugContext can detach components.
    /// </summary>
    [Test]
    public void DebugContext_CanDetachComponents()
    {
        debugContext.DetachSystem();
        Assert.That(debugContext.IsSystemAttached, Is.False);
    }

    /// <summary>
    /// Verifies that IsBusAttached is false when no bus is attached.
    /// </summary>
    [Test]
    public void DebugContext_IsBusAttached_IsFalse_WhenNoBusAttached()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.That(context.IsBusAttached, Is.False);
    }

    /// <summary>
    /// Verifies that IsBusAttached is true when a bus is attached.
    /// </summary>
    [Test]
    public void DebugContext_IsBusAttached_IsTrue_WhenBusAttached()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var mockBus = new Mock<IMemoryBus>();
        context.AttachBus(mockBus.Object);
        Assert.That(context.IsBusAttached, Is.True);
    }

    /// <summary>
    /// Verifies that Bus property is null when no bus is attached.
    /// </summary>
    [Test]
    public void DebugContext_Bus_IsNull_WhenNoBusAttached()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.That(context.Bus, Is.Null);
    }

    /// <summary>
    /// Verifies that AttachBus correctly sets the bus property.
    /// </summary>
    [Test]
    public void DebugContext_AttachBus_SetsBusProperty()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var mockBus = new Mock<IMemoryBus>();
        context.AttachBus(mockBus.Object);
        Assert.That(context.Bus, Is.SameAs(mockBus.Object));
    }

    /// <summary>
    /// Verifies that AttachBus throws ArgumentNullException when bus is null.
    /// </summary>
    [Test]
    public void DebugContext_AttachBus_ThrowsArgumentNullException_WhenBusIsNull()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.Throws<ArgumentNullException>(() => context.AttachBus(null!));
    }

    /// <summary>
    /// Verifies that Machine property is null when no machine is attached.
    /// </summary>
    [Test]
    public void DebugContext_Machine_IsNull_WhenNoMachineAttached()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.That(context.Machine, Is.Null);
    }

    /// <summary>
    /// Verifies that AttachMachine correctly sets the machine property.
    /// </summary>
    [Test]
    public void DebugContext_AttachMachine_SetsMachineProperty()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var mockBus = new Mock<IMemoryBus>();
        var mockMachine = new Mock<IMachine>();
        mockMachine.Setup(m => m.Cpu).Returns(cpu);
        mockMachine.Setup(m => m.Bus).Returns(mockBus.Object);
        context.AttachMachine(mockMachine.Object);
        Assert.That(context.Machine, Is.SameAs(mockMachine.Object));
    }

    /// <summary>
    /// Verifies that AttachMachine also sets CPU and Bus properties from the machine.
    /// </summary>
    [Test]
    public void DebugContext_AttachMachine_SetsCpuAndBusFromMachine()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var mockBus = new Mock<IMemoryBus>();
        var mockMachine = new Mock<IMachine>();
        mockMachine.Setup(m => m.Cpu).Returns(cpu);
        mockMachine.Setup(m => m.Bus).Returns(mockBus.Object);
        context.AttachMachine(mockMachine.Object);

        Assert.Multiple(() =>
        {
            Assert.That(context.Cpu, Is.SameAs(cpu));
            Assert.That(context.Bus, Is.SameAs(mockBus.Object));
            Assert.That(context.IsBusAttached, Is.True);
        });
    }

    /// <summary>
    /// Verifies that AttachMachine throws ArgumentNullException when machine is null.
    /// </summary>
    [Test]
    public void DebugContext_AttachMachine_ThrowsArgumentNullException_WhenMachineIsNull()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        Assert.Throws<ArgumentNullException>(() => context.AttachMachine(null!));
    }

    /// <summary>
    /// Verifies that DetachSystem clears bus and machine properties.
    /// </summary>
    [Test]
    public void DebugContext_DetachSystem_ClearsBusAndMachine()
    {
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        var mockBus = new Mock<IMemoryBus>();
        var mockMachine = new Mock<IMachine>();
        mockMachine.Setup(m => m.Cpu).Returns(cpu);
        mockMachine.Setup(m => m.Bus).Returns(mockBus.Object);
        context.AttachMachine(mockMachine.Object);

        context.DetachSystem();

        Assert.Multiple(() =>
        {
            Assert.That(context.Bus, Is.Null);
            Assert.That(context.Machine, Is.Null);
            Assert.That(context.IsBusAttached, Is.False);
        });
    }

    // =====================
    // DebugContext Bus Adapter Tests (Phase D2)
    // =====================

    /// <summary>
    /// Verifies that AttachBus creates MemoryBusAdapter as Memory property for backward compatibility.
    /// </summary>
    [Test]
    public void DebugContext_AttachBus_SetsUpBusProperty()
    {
        var testBus = CreateBusWithRam();
        debugContext.AttachBus(testBus);

        Assert.Multiple(() =>
        {
            Assert.That(debugContext.Bus, Is.Not.Null);
            Assert.That(debugContext.Bus, Is.SameAs(testBus));
            Assert.That(debugContext.IsBusAttached, Is.True);
        });
    }

    /// <summary>
    /// Verifies that AttachSystem with bus creates correct setup.
    /// </summary>
    [Test]
    public void DebugContext_AttachSystemWithBus_SetsUpCorrectly()
    {
        var testBus = CreateBusWithRam();
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);

        context.AttachSystem(cpu, testBus, disassembler);

        Assert.Multiple(() =>
        {
            Assert.That(context.Cpu, Is.SameAs(cpu));
            Assert.That(context.Bus, Is.SameAs(testBus));
            Assert.That(context.Disassembler, Is.SameAs(disassembler));
            Assert.That(context.IsSystemAttached, Is.True);
            Assert.That(context.IsBusAttached, Is.True);
        });
    }

    /// <summary>
    /// Verifies that AttachSystem with bus and machine info works correctly.
    /// </summary>
    [Test]
    public void DebugContext_AttachSystemWithBusAndMachineInfo_WorksCorrectly()
    {
        var testBus = CreateBusWithRam();
        var machineInfo = new MachineInfo("TestMachine", "Test Machine", "65C02", 65536);
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);

        context.AttachSystem(cpu, testBus, disassembler, machineInfo);

        Assert.Multiple(() =>
        {
            Assert.That(context.Cpu, Is.SameAs(cpu));
            Assert.That(context.Bus, Is.SameAs(testBus));
            Assert.That(context.Disassembler, Is.SameAs(disassembler));
            Assert.That(context.MachineInfo, Is.SameAs(machineInfo));
            Assert.That(context.IsSystemAttached, Is.True);
            Assert.That(context.IsBusAttached, Is.True);
        });
    }

    /// <summary>
    /// Verifies that AttachSystem with bus, machine info, and tracing listener works correctly.
    /// </summary>
    [Test]
    public void DebugContext_AttachSystemWithBusAndTracingListener_WorksCorrectly()
    {
        var testBus = CreateBusWithRam();
        var machineInfo = new MachineInfo("TestMachine", "Test Machine", "65C02", 65536);
        var tracingListener = new TracingDebugListener();
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);

        context.AttachSystem(cpu, testBus, disassembler, machineInfo, tracingListener);

        Assert.Multiple(() =>
        {
            Assert.That(context.Cpu, Is.SameAs(cpu));
            Assert.That(context.Bus, Is.SameAs(testBus));
            Assert.That(context.Disassembler, Is.SameAs(disassembler));
            Assert.That(context.MachineInfo, Is.SameAs(machineInfo));
            Assert.That(context.TracingListener, Is.SameAs(tracingListener));
            Assert.That(context.IsSystemAttached, Is.True);
            Assert.That(context.IsBusAttached, Is.True);
        });
    }

    /// <summary>
    /// Verifies that bus-based memory access patterns work.
    /// </summary>
    [Test]
    public void DebugContext_WithBus_DirectBusAccessWorks()
    {
        // Write to physical memory
        var testBus = CreateBusWithRam(out var physicalMemory);
        physicalMemory.AsSpan()[0x100] = 0x42;

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        context.AttachSystem(cpu, testBus, disassembler);

        // Read through the Bus interface
        byte value = ReadByte(context.Bus!, 0x100);
        Assert.That(value, Is.EqualTo(0x42));

        // Write through the Bus interface
        WriteByte(context.Bus!, 0x200, 0xAB);
        Assert.That(physicalMemory.AsSpan()[0x200], Is.EqualTo(0xAB));
    }

    /// <summary>
    /// Verifies that MemCommand works with bus-based system.
    /// </summary>
    [Test]
    public void MemCommand_WorksWithBusBasedSystem()
    {
        var bus = CreateBusWithRam(out var physicalMemory);
        physicalMemory.AsSpan()[0x200] = 0x41; // 'A'
        physicalMemory.AsSpan()[0x201] = 0x42; // 'B'
        physicalMemory.AsSpan()[0x202] = 0x43; // 'C'

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        context.AttachSystem(cpu, bus, disassembler);

        var command = new MemCommand();
        var result = command.Execute(context, ["$0200", "16"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("41"));
            Assert.That(outputWriter.ToString(), Does.Contain("42"));
            Assert.That(outputWriter.ToString(), Does.Contain("43"));
            Assert.That(outputWriter.ToString(), Does.Contain("ABC")); // ASCII
        });
    }

    /// <summary>
    /// Verifies that PokeCommand works with bus-based system.
    /// </summary>
    [Test]
    public void PokeCommand_WorksWithBusBasedSystem()
    {
        var bus = CreateBusWithRam(out var physicalMemory);

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        context.AttachSystem(cpu, bus, disassembler);

        var command = new PokeCommand();
        var result = command.Execute(context, ["$0300", "$AB", "$CD", "$EF"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(physicalMemory.AsSpan()[0x300], Is.EqualTo(0xAB));
            Assert.That(physicalMemory.AsSpan()[0x301], Is.EqualTo(0xCD));
            Assert.That(physicalMemory.AsSpan()[0x302], Is.EqualTo(0xEF));
        });
    }

    // =====================
    // Non-Debug Context Tests
    // =====================

    /// <summary>
    /// Verifies that debug commands return error with non-debug context.
    /// </summary>
    [Test]
    public void DebugCommands_ReturnError_WithNonDebugContext()
    {
        var normalContext = new CommandContext(dispatcher, outputWriter, errorWriter);

        var commands = new ICommandHandler[]
        {
            new RegsCommand(),
            new StepCommand(),
            new RunCommand(),
            new StopCommand(),
            new ResetCommand(),
            new PcCommand(),
            new MemCommand(),
            new PokeCommand(),
            new LoadCommand(),
            new SaveCommand(),
            new DasmCommand(),
        };

        foreach (var command in commands)
        {
            // Use empty args - the check for debug context should happen before argument validation
            var result = command.Execute(normalContext, []);
            Assert.That(result.Success, Is.False, $"Command {command.Name} should fail with non-debug context");
            Assert.That(result.Message, Does.Contain("Debug context required"), $"Command {command.Name} should mention debug context");
        }
    }

    // =====================
    // RegionsCommand Tests
    // =====================

    /// <summary>
    /// Verifies that RegionsCommand has correct name.
    /// </summary>
    [Test]
    public void RegionsCommand_HasCorrectName()
    {
        var command = new RegionsCommand();
        Assert.That(command.Name, Is.EqualTo("regions"));
    }

    /// <summary>
    /// Verifies that RegionsCommand displays memory regions.
    /// </summary>
    [Test]
    public void RegionsCommand_DisplaysMemoryRegions()
    {
        var command = new RegionsCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Memory Regions"));
            Assert.That(outputWriter.ToString(), Does.Contain("Ram"));
        });
    }

    /// <summary>
    /// Verifies that RegionsCommand returns error when no bus attached.
    /// </summary>
    [Test]
    public void RegionsCommand_ReturnsError_WhenNoBusAttached()
    {
        var contextWithoutBus = new DebugContext(dispatcher, outputWriter, errorWriter);
        var command = new RegionsCommand();

        var result = command.Execute(contextWithoutBus, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("No bus attached"));
        });
    }

    // =====================
    // PagesCommand Tests
    // =====================

    /// <summary>
    /// Verifies that PagesCommand has correct name.
    /// </summary>
    [Test]
    public void PagesCommand_HasCorrectName()
    {
        var command = new PagesCommand();
        Assert.That(command.Name, Is.EqualTo("pages"));
    }

    /// <summary>
    /// Verifies that PagesCommand displays page table.
    /// </summary>
    [Test]
    public void PagesCommand_DisplaysPageTable()
    {
        var command = new PagesCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Page Table"));
            Assert.That(outputWriter.ToString(), Does.Contain("VirtAddr"));
        });
    }

    /// <summary>
    /// Verifies that PagesCommand accepts start page argument.
    /// </summary>
    [Test]
    public void PagesCommand_AcceptsStartPage()
    {
        var command = new PagesCommand();
        var result = command.Execute(debugContext, ["$04"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("$04"));
        });
    }

    // =====================
    // PeekCommand Tests
    // =====================

    /// <summary>
    /// Verifies that PeekCommand has correct name.
    /// </summary>
    [Test]
    public void PeekCommand_HasCorrectName()
    {
        var command = new PeekCommand();
        Assert.That(command.Name, Is.EqualTo("peek"));
    }

    /// <summary>
    /// Verifies that PeekCommand reads single byte.
    /// </summary>
    [Test]
    public void PeekCommand_ReadsSingleByte()
    {
        WriteByte(bus, 0x1234, 0xAB);
        var command = new PeekCommand();
        var result = command.Execute(debugContext, ["$1234"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("$1234"));
            Assert.That(outputWriter.ToString(), Does.Contain("AB"));
        });
    }

    /// <summary>
    /// Verifies that PeekCommand reads multiple bytes.
    /// </summary>
    [Test]
    public void PeekCommand_ReadsMultipleBytes()
    {
        WriteByte(bus, 0x1000, 0x11);
        WriteByte(bus, 0x1001, 0x22);
        WriteByte(bus, 0x1002, 0x33);
        var command = new PeekCommand();
        var result = command.Execute(debugContext, ["$1000", "3"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("11"));
            Assert.That(outputWriter.ToString(), Does.Contain("22"));
            Assert.That(outputWriter.ToString(), Does.Contain("33"));
        });
    }

    // =====================
    // ReadCommand Tests
    // =====================

    /// <summary>
    /// Verifies that ReadCommand has correct name.
    /// </summary>
    [Test]
    public void ReadCommand_HasCorrectName()
    {
        var command = new ReadCommand();
        Assert.That(command.Name, Is.EqualTo("read"));
    }

    /// <summary>
    /// Verifies that ReadCommand has correct aliases.
    /// </summary>
    [Test]
    public void ReadCommand_HasCorrectAliases()
    {
        var command = new ReadCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "rd" }));
    }

    /// <summary>
    /// Verifies that ReadCommand reads memory.
    /// </summary>
    [Test]
    public void ReadCommand_ReadsMemory()
    {
        WriteByte(bus, 0x2000, 0xCD);
        var command = new ReadCommand();
        var result = command.Execute(debugContext, ["$2000"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("CD"));
            Assert.That(outputWriter.ToString(), Does.Contain("side effects"));
        });
    }

    // =====================
    // WriteCommand Tests
    // =====================

    /// <summary>
    /// Verifies that WriteCommand has correct name.
    /// </summary>
    [Test]
    public void WriteCommand_HasCorrectName()
    {
        var command = new WriteCommand();
        Assert.That(command.Name, Is.EqualTo("write"));
    }

    /// <summary>
    /// Verifies that WriteCommand has correct aliases.
    /// </summary>
    [Test]
    public void WriteCommand_HasCorrectAliases()
    {
        var command = new WriteCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "wr" }));
    }

    /// <summary>
    /// Verifies that WriteCommand writes memory.
    /// </summary>
    [Test]
    public void WriteCommand_WritesMemory()
    {
        var command = new WriteCommand();
        var result = command.Execute(debugContext, ["$3000", "EF"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x3000), Is.EqualTo(0xEF));
            Assert.That(outputWriter.ToString(), Does.Contain("side effects"));
        });
    }

    // =====================
    // ProfileCommand Tests
    // =====================

    /// <summary>
    /// Verifies that ProfileCommand has correct name.
    /// </summary>
    [Test]
    public void ProfileCommand_HasCorrectName()
    {
        var command = new ProfileCommand();
        Assert.That(command.Name, Is.EqualTo("profile"));
    }

    /// <summary>
    /// Verifies that ProfileCommand displays machine profile info.
    /// </summary>
    [Test]
    public void ProfileCommand_DisplaysProfileInfo()
    {
        var command = new ProfileCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Machine Profile"));
        });
    }

    // =====================
    // DeviceMapCommand Tests
    // =====================

    /// <summary>
    /// Verifies that DeviceMapCommand has correct name.
    /// </summary>
    [Test]
    public void DeviceMapCommand_HasCorrectName()
    {
        var command = new DeviceMapCommand();
        Assert.That(command.Name, Is.EqualTo("devicemap"));
    }

    /// <summary>
    /// Verifies that DeviceMapCommand has correct aliases.
    /// </summary>
    [Test]
    public void DeviceMapCommand_HasCorrectAliases()
    {
        var command = new DeviceMapCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "devices", "devmap" }));
    }

    // =====================
    // FaultCommand Tests
    // =====================

    /// <summary>
    /// Verifies that FaultCommand has correct name.
    /// </summary>
    [Test]
    public void FaultCommand_HasCorrectName()
    {
        var command = new FaultCommand();
        Assert.That(command.Name, Is.EqualTo("fault"));
    }

    /// <summary>
    /// Verifies that FaultCommand displays fault status.
    /// </summary>
    [Test]
    public void FaultCommand_DisplaysFaultStatus()
    {
        var command = new FaultCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Bus Fault Status"));
        });
    }

    // =====================
    // SwitchesCommand Tests
    // =====================

    /// <summary>
    /// Verifies that SwitchesCommand has correct name.
    /// </summary>
    [Test]
    public void SwitchesCommand_HasCorrectName()
    {
        var command = new SwitchesCommand();
        Assert.That(command.Name, Is.EqualTo("switches"));
    }

    /// <summary>
    /// Verifies that SwitchesCommand has correct aliases.
    /// </summary>
    [Test]
    public void SwitchesCommand_HasCorrectAliases()
    {
        var command = new SwitchesCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "sw", "softswitch" }));
    }

    // =====================
    // BusLogCommand Tests
    // =====================

    /// <summary>
    /// Verifies that BusLogCommand has correct name.
    /// </summary>
    [Test]
    public void BusLogCommand_HasCorrectName()
    {
        var command = new BusLogCommand();
        Assert.That(command.Name, Is.EqualTo("buslog"));
    }

    /// <summary>
    /// Verifies that BusLogCommand has correct aliases.
    /// </summary>
    [Test]
    public void BusLogCommand_HasCorrectAliases()
    {
        var command = new BusLogCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "bl", "trace" }));
    }

    /// <summary>
    /// Verifies that BusLogCommand displays status by default.
    /// </summary>
    [Test]
    public void BusLogCommand_DisplaysStatus()
    {
        var command = new BusLogCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Bus Logging Status"));
        });
    }

    // =====================
    // CallCommand Tests
    // =====================

    /// <summary>
    /// Verifies that CallCommand has correct name.
    /// </summary>
    [Test]
    public void CallCommand_HasCorrectName()
    {
        var command = new CallCommand();
        Assert.That(command.Name, Is.EqualTo("call"));
    }

    /// <summary>
    /// Verifies that CallCommand has correct aliases.
    /// </summary>
    [Test]
    public void CallCommand_HasCorrectAliases()
    {
        var command = new CallCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "jsr" }));
    }

    /// <summary>
    /// Verifies that CallCommand executes subroutine and returns.
    /// </summary>
    [Test]
    public void CallCommand_ExecutesSubroutineAndReturns()
    {
        // Write a simple subroutine: LDA #$42, RTS
        WriteByte(bus, 0x2000, 0xA9); // LDA immediate
        WriteByte(bus, 0x2001, 0x42); // #$42
        WriteByte(bus, 0x2002, 0x60); // RTS

        var command = new CallCommand();
        var result = command.Execute(debugContext, ["$2000"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("completed"));
            Assert.That(outputWriter.ToString(), Does.Contain("A=$42"));
        });
    }

    /// <summary>
    /// Verifies that CallCommand returns error when address missing.
    /// </summary>
    [Test]
    public void CallCommand_ReturnsError_WhenAddressMissing()
    {
        var command = new CallCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Address required"));
        });
    }

    /// <summary>
    /// Helper method to create a bus with full RAM mapping.
    /// </summary>
    private static Bus.MainBus CreateBusWithRam()
    {
        return CreateBusWithRam(out _);
    }

    /// <summary>
    /// Helper method to create a bus with full RAM mapping and access to physical memory.
    /// </summary>
    /// <remarks>
    /// Creates a 64KB address space (16-bit addressing) with 16 pages of 4KB each,
    /// all mapped to RAM with full read/write permissions.
    /// </remarks>
    private static Bus.MainBus CreateBusWithRam(out Bus.PhysicalMemory physicalMemory)
    {
        // 64KB address space: 16 pages × 4KB = 65536 bytes
        const int TestMemorySize = 65536;
        const int PageCount = 16; // 64KB / 4KB per page

        var bus = new Bus.MainBus(addressSpaceBits: 16);
        physicalMemory = new Bus.PhysicalMemory(TestMemorySize, "TestRAM");
        var target = new Bus.RamTarget(physicalMemory.Slice(0, TestMemorySize));

        bus.MapPageRange(
            startPage: 0,
            pageCount: PageCount,
            deviceId: 1,
            regionTag: Bus.RegionTag.Ram,
            perms: Bus.PagePerms.ReadWrite,
            caps: Bus.TargetCaps.SupportsPeek | Bus.TargetCaps.SupportsPoke | Bus.TargetCaps.SupportsWide,
            target: target,
            physicalBase: 0);

        return bus;
    }
}