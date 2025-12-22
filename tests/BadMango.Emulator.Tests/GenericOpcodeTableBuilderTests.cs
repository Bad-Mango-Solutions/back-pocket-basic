// <copyright file="GenericOpcodeTableBuilderTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Core;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Memory;

/// <summary>
/// Tests for the generic opcode table builder pattern.
/// </summary>
[TestFixture]
public class GenericOpcodeTableBuilderTests
{
    private IMemory memory = null!;
    private Cpu65C02 cpu = null!;

    /// <summary>
    /// Sets up test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        memory = new BasicMemory();
        cpu = new Cpu65C02(memory);
    }

    /// <summary>
    /// Demonstrates the clean builder API for creating opcode handlers.
    /// </summary>
    [Test]
    public void Builder_ProvidesCleanAPI_ForCreatingHandlers()
    {
        // Arrange - Create a builder instance
        var builder = OpcodeTableBuilders.ForCpu65C02();

        // Act - Use the clean API to create handlers
        var ldaHandler = builder.Instructions.LDA(builder.AddressingModes.Immediate);
        var staHandler = builder.Instructions.STA(builder.AddressingModes.ZeroPage);

        // Assert - Handlers are created successfully
        Assert.That(ldaHandler, Is.Not.Null);
        Assert.That(staHandler, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that handlers created via builder work correctly.
    /// </summary>
    [Test]
    public void Builder_CreatedHandlers_ExecuteCorrectly()
    {
        // Arrange
        var builder = OpcodeTableBuilders.ForCpu65C02();
        memory.Write(0x1000, 0x42);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, P = 0x00, Cycles = 10 };

        // Act - Create and execute an LDA handler
        var handler = builder.Instructions.LDA(builder.AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x42));
        Assert.That(state.PC, Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Demonstrates building a simple opcode table using the builder pattern.
    /// </summary>
    [Test]
    public void Builder_CanBuildSimpleOpcodeTable()
    {
        // Arrange
        var builder = OpcodeTableBuilders.ForCpu65C02();
        var handlers = new OpcodeHandler<Cpu65C02, Cpu65C02State>[256];

        // Act - Build a few opcodes using the clean API
        handlers[0xA9] = builder.Instructions.LDA(builder.AddressingModes.Immediate);
        handlers[0xA5] = builder.Instructions.LDA(builder.AddressingModes.ZeroPage);
        handlers[0xB5] = builder.Instructions.LDA(builder.AddressingModes.ZeroPageX);
        handlers[0x85] = builder.Instructions.STA(builder.AddressingModes.ZeroPage);
        handlers[0xEA] = builder.Instructions.NOP(builder.AddressingModes.Implied);

        // Assert - All handlers are created
        Assert.That(handlers[0xA9], Is.Not.Null, "LDA Immediate should be created");
        Assert.That(handlers[0xA5], Is.Not.Null, "LDA ZeroPage should be created");
        Assert.That(handlers[0xB5], Is.Not.Null, "LDA ZeroPageX should be created");
        Assert.That(handlers[0x85], Is.Not.Null, "STA ZeroPage should be created");
        Assert.That(handlers[0xEA], Is.Not.Null, "NOP should be created");
    }

    /// <summary>
    /// Compares the readability of the builder pattern vs direct generic usage.
    /// </summary>
    [Test]
    public void Builder_API_IsMoreReadable_ThanDirectGenericUsage()
    {
        // This test demonstrates the difference in readability

        // Using builder pattern (clean and readable):
        var builder = OpcodeTableBuilders.ForCpu65C02();
        var handler1 = builder.Instructions.LDA(builder.AddressingModes.Immediate);

        // Direct generic usage (verbose and hard to read):
        AddressingMode<Cpu65C02State> addressingMode = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.Immediate;
        var handler2 = InstructionsFor<Cpu65C02, Cpu65C02Registers, byte, byte, byte, Word, Cpu65C02State>.LDA(addressingMode);

        // Both produce the same result
        Assert.That(handler1, Is.Not.Null);
        Assert.That(handler2, Is.Not.Null);

        // This test serves as documentation showing the improved API
    }

    /// <summary>
    /// Verifies that the builder can be reused for multiple opcode table constructions.
    /// </summary>
    [Test]
    public void Builder_CanBeReused_ForMultipleTables()
    {
        // Arrange
        var builder = OpcodeTableBuilders.ForCpu65C02();

        // Act - Create multiple handlers reusing the same builder
        var handler1 = builder.Instructions.LDA(builder.AddressingModes.Immediate);
        var handler2 = builder.Instructions.LDA(builder.AddressingModes.ZeroPage);
        var handler3 = builder.Instructions.STA(builder.AddressingModes.Absolute);

        // Assert - All handlers are independent and functional
        Assert.That(handler1, Is.Not.Null);
        Assert.That(handler2, Is.Not.Null);
        Assert.That(handler3, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that composed instructions execute correctly.
    /// </summary>
    [Test]
    public void Builder_ComposedInstructions_ExecuteCorrectly()
    {
        // Arrange
        var builder = OpcodeTableBuilders.ForCpu65C02();
        memory.Write(0x1000, 0x50); // ZP address
        memory.Write(0x0050, 0x99); // Value at ZP
        var state = new Cpu65C02State { PC = 0x1000, X = 0x00, P = 0x00, Cycles = 10 };

        // Act - Use builder to compose LDX with ZeroPage
        var handler = builder.Instructions.LDX(builder.AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.X, Is.EqualTo(0x99));
        Assert.That(state.PC, Is.EqualTo(0x1001));
        Assert.That(state.Cycles, Is.EqualTo(12)); // +1 ZP fetch, +1 read
    }
}
