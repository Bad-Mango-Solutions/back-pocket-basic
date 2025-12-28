// <copyright file="DebugCommandsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Debugging;
using BadMango.Emulator.Emulation.Memory;

using Bus.Interfaces;

using Moq;

using Bus = BadMango.Emulator.Bus;

/// <summary>
/// Unit tests for the debug command handlers.
/// </summary>
[TestFixture]
public class DebugCommandsTests
{
    private CommandDispatcher dispatcher = null!;
    private BasicMemory memory = null!;
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
        memory = new BasicMemory();
        cpu = new Cpu65C02(memory);
        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
        disassembler = new Disassembler(opcodeTable, memory);

        outputWriter = new StringWriter();
        errorWriter = new StringWriter();
        debugContext = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, memory, disassembler);

        // Set up reset vector so CPU can be reset properly
        memory.WriteWord(0xFFFC, 0x1000);
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
        memory.Write(0x1000, 0xEA); // NOP
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
        memory.Write(0x1000, 0xEA); // NOP
        memory.Write(0x1001, 0xEA); // NOP
        memory.Write(0x1002, 0xEA); // NOP
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
        memory.Write(0x1000, 0xDB); // STP
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
        memory.Write(0x1000, 0xEA); // NOP
        memory.Write(0x1001, 0xEA); // NOP
        memory.Write(0x1002, 0xDB); // STP
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
        memory.Write(0x1000, 0xEA); // NOP
        memory.Write(0x1001, 0x4C); // JMP $1000
        memory.Write(0x1002, 0x00);
        memory.Write(0x1003, 0x10);
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
        memory.Write(0x0200, 0xFF);

        // Need to re-set reset vector after clear
        memory.WriteWord(0xFFFC, 0x1000);

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
        memory.Write(0x0200, 0x41); // 'A'
        memory.Write(0x0201, 0x42); // 'B'
        memory.Write(0x0202, 0x43); // 'C'

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
            Assert.That(memory.Read(0x0300), Is.EqualTo(0xAB));
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
            Assert.That(memory.Read(0x0400), Is.EqualTo(0x11));
            Assert.That(memory.Read(0x0401), Is.EqualTo(0x22));
            Assert.That(memory.Read(0x0402), Is.EqualTo(0x33));
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
            Assert.That(memory.Read(0x0900), Is.EqualTo(0xAB));
            Assert.That(memory.Read(0x0901), Is.EqualTo(0xCD));
            Assert.That(memory.Read(0x0902), Is.EqualTo(0xEF));
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
            Assert.That(memory.Read(0x0950), Is.EqualTo(0x11));
            Assert.That(memory.Read(0x0951), Is.EqualTo(0x22));
            Assert.That(memory.Read(0x0952), Is.EqualTo(0x33));
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
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, memory, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0500", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(memory.Read(0x0500), Is.EqualTo(0xAA));
            Assert.That(memory.Read(0x0501), Is.EqualTo(0xBB));
            Assert.That(memory.Read(0x0502), Is.EqualTo(0xCC));
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
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, memory, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0600", "--interactive"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(memory.Read(0x0600), Is.EqualTo(0x11));
            Assert.That(memory.Read(0x0601), Is.EqualTo(0x22));
            Assert.That(memory.Read(0x0602), Is.EqualTo(0x33));
            Assert.That(memory.Read(0x0603), Is.EqualTo(0x44));
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
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, memory, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0700", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(memory.Read(0x0700), Is.EqualTo(0x55));
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
        var contextWithoutInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, memory, disassembler, null);

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
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, memory, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0A00", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(memory.Read(0x0A00), Is.EqualTo(0x11));
            Assert.That(memory.Read(0x0A01), Is.EqualTo(0x22));
            Assert.That(memory.Read(0x0B00), Is.EqualTo(0x33));
            Assert.That(memory.Read(0x0B01), Is.EqualTo(0x44));
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
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, memory, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0C00", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(memory.Read(0x0D00), Is.EqualTo(0x55));
            Assert.That(memory.Read(0x0D01), Is.EqualTo(0x66));
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
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, memory, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0100", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(memory.Read(0x0E00), Is.EqualTo(0x77));
            Assert.That(memory.Read(0x0E01), Is.EqualTo(0x88));
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
        memory.Write(0x1000, 0xEA); // NOP

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
        memory.Write(0x2000, 0xA9); // LDA immediate
        memory.Write(0x2001, 0x42); // #$42

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
        var contextWithoutDisasm = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, memory, null);
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

        context.AttachSystem(cpu, memory, disassembler);
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
    public void DebugContext_AttachBus_CreatesMemoryAdapter()
    {
        var bus = CreateBusWithRam();
        debugContext.AttachBus(bus);

        Assert.Multiple(() =>
        {
            Assert.That(debugContext.Memory, Is.Not.Null);
            Assert.That(debugContext.Memory, Is.InstanceOf<Bus.MemoryBusAdapter>());
        });
    }

    /// <summary>
    /// Verifies that AttachSystem with bus creates correct adapter.
    /// </summary>
    [Test]
    public void DebugContext_AttachSystemWithBus_CreatesAdapter()
    {
        var bus = CreateBusWithRam();
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);

        context.AttachSystem(cpu, bus, disassembler);

        Assert.Multiple(() =>
        {
            Assert.That(context.Cpu, Is.SameAs(cpu));
            Assert.That(context.Bus, Is.SameAs(bus));
            Assert.That(context.Disassembler, Is.SameAs(disassembler));
            Assert.That(context.Memory, Is.InstanceOf<Bus.MemoryBusAdapter>());
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
        var bus = CreateBusWithRam();
        var machineInfo = new MachineInfo("TestMachine", "Test Machine", "65C02", 65536);
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);

        context.AttachSystem(cpu, bus, disassembler, machineInfo);

        Assert.Multiple(() =>
        {
            Assert.That(context.Cpu, Is.SameAs(cpu));
            Assert.That(context.Bus, Is.SameAs(bus));
            Assert.That(context.Disassembler, Is.SameAs(disassembler));
            Assert.That(context.MachineInfo, Is.SameAs(machineInfo));
            Assert.That(context.Memory, Is.InstanceOf<Bus.MemoryBusAdapter>());
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
        var bus = CreateBusWithRam();
        var machineInfo = new MachineInfo("TestMachine", "Test Machine", "65C02", 65536);
        var tracingListener = new TracingDebugListener();
        var context = new DebugContext(dispatcher, outputWriter, errorWriter);

        context.AttachSystem(cpu, bus, disassembler, machineInfo, tracingListener);

        Assert.Multiple(() =>
        {
            Assert.That(context.Cpu, Is.SameAs(cpu));
            Assert.That(context.Bus, Is.SameAs(bus));
            Assert.That(context.Disassembler, Is.SameAs(disassembler));
            Assert.That(context.MachineInfo, Is.SameAs(machineInfo));
            Assert.That(context.TracingListener, Is.SameAs(tracingListener));
            Assert.That(context.Memory, Is.InstanceOf<Bus.MemoryBusAdapter>());
            Assert.That(context.IsSystemAttached, Is.True);
            Assert.That(context.IsBusAttached, Is.True);
        });
    }

    /// <summary>
    /// Verifies that legacy memory access patterns work through MemoryBusAdapter.
    /// </summary>
    [Test]
    public void DebugContext_WithBus_LegacyMemoryAccessPatternWorks()
    {
        // Write to physical memory
        var bus = CreateBusWithRam(out var physicalMemory);
        physicalMemory.AsSpan()[0x100] = 0x42;

        var context = new DebugContext(dispatcher, outputWriter, errorWriter);
        context.AttachSystem(cpu, bus, disassembler);

        // Read through the Memory interface (which is now MemoryBusAdapter)
        byte value = context.Memory!.Read(0x100);
        Assert.That(value, Is.EqualTo(0x42));

        // Write through the Memory interface
        context.Memory.Write(0x200, 0xAB);
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