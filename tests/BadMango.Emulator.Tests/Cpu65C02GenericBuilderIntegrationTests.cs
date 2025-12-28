// <copyright file="Cpu65C02GenericBuilderIntegrationTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;
using Core.Interfaces;

using Emulation.Cpu;
using Emulation.Memory;

/// <summary>
/// Integration tests demonstrating the builder pattern integrated into CPU opcode table construction.
/// </summary>
[TestFixture]
public class Cpu65C02GenericBuilderIntegrationTests
{
    private IMemory memory = null!;

    /// <summary>
    /// Sets up test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        memory = new BasicMemory();
    }

    /// <summary>
    /// Demonstrates that the builder produces a working opcode table.
    /// </summary>
    [Test]
    public void Builder_ProducesWorkingOpcodeTable()
    {
        // Arrange
        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();

        // Assert - Table should be created
        Assert.That(opcodeTable, Is.Not.Null);

        // Verify we can get handlers for opcodes we know we implemented
        var ldaImmediateHandler = opcodeTable.GetHandler(0xA9);
        Assert.That(ldaImmediateHandler, Is.Not.Null);

        var staZeroPageHandler = opcodeTable.GetHandler(0x85);
        Assert.That(staZeroPageHandler, Is.Not.Null);
    }

    /// <summary>
    /// Demonstrates that opcodes built with compositional pattern execute correctly.
    /// </summary>
    [Test]
    public void Builder_Opcodes_ExecuteCorrectly()
    {
        // Arrange
        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
        memory.Write(0x1000, 0x42); // Value to load
        var state = CreateState(pc: 0x1000, a: 0x00, p: 0, cycles: 0);

        // Act - Execute LDA Immediate (opcode 0xA9)
        var handler = opcodeTable.GetHandler(0xA9);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x42), "LDA should load the value");
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x1001), "PC should be incremented");
    }

    /// <summary>
    /// Verifies LDA instruction works across multiple addressing modes.
    /// </summary>
    [Test]
    public void Builder_LDA_WorksAcrossAddressingModes()
    {
        // Arrange
        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();

        // Test LDA Zero Page (0xA5)
        memory.Write(0x1000, 0x50); // ZP address
        memory.Write(0x0050, 0x99); // Value at ZP
        var state = CreateState(pc: 0x1000, a: 0x00, p: 0, cycles: 0);

        // Act
        var handler = opcodeTable.GetHandler(0xA5);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x99), "LDA ZP should load from zero page");
    }

    /// <summary>
    /// Verifies STA instruction stores values correctly.
    /// </summary>
    [Test]
    public void Builder_STA_StoresValuesCorrectly()
    {
        // Arrange
        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
        memory.Write(0x1000, 0x50); // ZP address
        var state = CreateState(pc: 0x1000, a: 0x42, p: 0, cycles: 0);

        // Act - Execute STA Zero Page (0x85)
        var handler = opcodeTable.GetHandler(0x85);
        handler(memory, ref state);

        // Assert
        Assert.That(memory.Read(0x0050), Is.EqualTo(0x42), "STA should store accumulator value");
    }

    /// <summary>
    /// Demonstrates the clean syntax of the compositional pattern.
    /// </summary>
    [Test]
    public void Builder_DemonstratesCleanSyntax()
    {
        // This test exists primarily for documentation purposes
        // It shows how the compositional pattern provides clean, readable code

        // Instructions compose cleanly with addressing modes:
        var handler1 = Instructions.LDA(AddressingModes.Immediate);
        var handler2 = Instructions.STA(AddressingModes.ZeroPage);

        // The pattern allows easy extension without combinatorial explosion
        Assert.That(handler1, Is.Not.Null);
        Assert.That(handler2, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that multiple instructions work together.
    /// </summary>
    [Test]
    public void Builder_MultipleInstructions_WorkTogether()
    {
        // Arrange
        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();

        // Set up a simple program: LDA #$42, STA $50
        memory.Write(0x1000, 0x42); // Value for LDA immediate
        memory.Write(0x1001, 0x50); // ZP address for STA
        var state = CreateState(pc: 0x1000, a: 0x00, p: 0, cycles: 0);

        // Act - Execute LDA #$42 (opcode 0xA9)
        var ldaHandler = opcodeTable.GetHandler(0xA9);
        ldaHandler(memory, ref state);

        // Then execute STA $50 (opcode 0x85)
        var staHandler = opcodeTable.GetHandler(0x85);
        staHandler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x42), "Accumulator should contain loaded value");
        Assert.That(memory.Read(0x0050), Is.EqualTo(0x42), "Memory should contain stored value");
    }

    /// <summary>
    /// Creates a CpuState for testing with the specified register values.
    /// </summary>
    private static CpuState CreateState(
        Word pc = 0,
        byte a = 0,
        byte x = 0,
        byte y = 0,
        byte sp = 0,
        ProcessorStatusFlags p = 0,
        ulong cycles = 0)
    {
        var state = new CpuState { Cycles = cycles };
        state.Registers.Reset(true);  // Initialize for 65C02 8-bit mode
        state.Registers.PC.SetWord(pc);
        state.Registers.A.SetByte(a);
        state.Registers.X.SetByte(x);
        state.Registers.Y.SetByte(y);
        state.Registers.SP.SetByte(sp);
        state.Registers.P = p;
        return state;
    }
}