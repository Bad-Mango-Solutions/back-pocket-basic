// <copyright file="GenericInstructionsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Core;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Memory;

/// <summary>
/// Tests for generic instruction implementations.
/// </summary>
[TestFixture]
public class GenericInstructionsTests
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
    /// Verifies that generic LDA instruction works with Cpu65C02State.
    /// </summary>
    [Test]
    public void Generic_LDA_LoadsValueAndSetsFlags()
    {
        // Arrange
        memory.Write(0x1000, 0x42);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, P = 0x00, Cycles = 10 };

        // Act - Using generic Instructions with generic AddressingModes
        AddressingMode<Cpu65C02State> addressingMode = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.Immediate;
        var handler = InstructionsFor<Cpu65C02, Cpu65C02Registers, byte, byte, byte, Word, Cpu65C02State>.LDA(addressingMode);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x42));
        Assert.That(state.P & 0x02, Is.EqualTo(0), "Zero flag should be clear");
        Assert.That(state.P & 0x80, Is.EqualTo(0), "Negative flag should be clear");
    }

    /// <summary>
    /// Verifies that generic LDA sets zero flag when loading zero.
    /// </summary>
    [Test]
    public void Generic_LDA_SetsZeroFlagWhenLoadingZero()
    {
        // Arrange
        memory.Write(0x1000, 0x00);
        var state = new Cpu65C02State { PC = 0x1000, A = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        AddressingMode<Cpu65C02State> addressingMode = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.Immediate;
        var handler = InstructionsFor<Cpu65C02, Cpu65C02Registers, byte, byte, byte, Word, Cpu65C02State>.LDA(addressingMode);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x00));
        Assert.That(state.P & 0x02, Is.EqualTo(0x02), "Zero flag should be set");
        Assert.That(state.P & 0x80, Is.EqualTo(0), "Negative flag should be clear");
    }

    /// <summary>
    /// Verifies that generic LDA sets negative flag for values >= 0x80.
    /// </summary>
    [Test]
    public void Generic_LDA_SetsNegativeFlagForHighValues()
    {
        // Arrange
        memory.Write(0x1000, 0x80);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, P = 0x00, Cycles = 10 };

        // Act
        AddressingMode<Cpu65C02State> addressingMode = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.Immediate;
        var handler = InstructionsFor<Cpu65C02, Cpu65C02Registers, byte, byte, byte, Word, Cpu65C02State>.LDA(addressingMode);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x80));
        Assert.That(state.P & 0x02, Is.EqualTo(0), "Zero flag should be clear");
        Assert.That(state.P & 0x80, Is.EqualTo(0x80), "Negative flag should be set");
    }

    /// <summary>
    /// Verifies that generic STA stores accumulator value correctly.
    /// </summary>
    [Test]
    public void Generic_STA_StoresAccumulatorValue()
    {
        // Arrange
        memory.Write(0x1000, 0x50); // Zero page address
        var state = new Cpu65C02State { PC = 0x1000, A = 0x42, P = 0x00, Cycles = 10 };

        // Act
        AddressingMode<Cpu65C02State> addressingMode = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.ZeroPage;
        var handler = InstructionsFor<Cpu65C02, Cpu65C02Registers, byte, byte, byte, Word, Cpu65C02State>.STA(addressingMode);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(memory.Read(0x0050), Is.EqualTo(0x42));
        Assert.That(state.Cycles, Is.EqualTo(12)); // +1 for ZP fetch, +1 for write
    }

    /// <summary>
    /// Verifies that generic NOP instruction works correctly.
    /// </summary>
    [Test]
    public void Generic_NOP_ExecutesWithoutModifyingState()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x42, X = 0x10, Y = 0x20, P = 0x30, Cycles = 10 };

        // Act
        AddressingMode<Cpu65C02State> addressingMode = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.Implied;
        var handler = InstructionsFor<Cpu65C02, Cpu65C02Registers, byte, byte, byte, Word, Cpu65C02State>.NOP(addressingMode);
        handler(cpu, memory, ref state);

        // Assert - Only cycles should change
        Assert.That(state.A, Is.EqualTo(0x42));
        Assert.That(state.X, Is.EqualTo(0x10));
        Assert.That(state.Y, Is.EqualTo(0x20));
        Assert.That(state.P, Is.EqualTo(0x30));
        Assert.That(state.Cycles, Is.EqualTo(11)); // +1 cycle for NOP
    }

    /// <summary>
    /// Demonstrates composition of generic instructions with generic addressing modes.
    /// </summary>
    [Test]
    public void Generic_Instructions_ComposeWithAddressingModes()
    {
        // Arrange
        memory.Write(0x1000, 0x50); // Zero page address
        memory.Write(0x0050, 0x99); // Value at zero page location
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, P = 0x00, Cycles = 10 };

        // Act - Compose LDX with ZeroPage addressing
        AddressingMode<Cpu65C02State> addressingMode = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.ZeroPage;
        var handler = InstructionsFor<Cpu65C02, Cpu65C02Registers, byte, byte, byte, Word, Cpu65C02State>.LDX(addressingMode);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.X, Is.EqualTo(0x99));
        Assert.That(state.Cycles, Is.EqualTo(12)); // +1 for ZP fetch, +1 for read
    }
}
