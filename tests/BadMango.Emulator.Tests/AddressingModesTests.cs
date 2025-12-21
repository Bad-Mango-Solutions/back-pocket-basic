// <copyright file="AddressingModesTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Core;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Memory;

/// <summary>
/// Unit tests for the <see cref="AddressingModes"/> class.
/// </summary>
[TestFixture]
public class AddressingModesTests
{
    private IMemory memory = null!;
    private ushort pc;
    private ulong cycles;

    /// <summary>
    /// Sets up the test environment by initializing memory and state.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        memory = new BasicMemory();
        pc = 0x1000;
        cycles = 0;
    }

    /// <summary>
    /// Verifies that ReadImmediate reads from PC and increments correctly.
    /// </summary>
    [Test]
    public void ReadImmediate_ReadsFromPCAndIncrements()
    {
        // Arrange
        memory.Write(0x1000, 0x42);

        // Act
        byte value = AddressingModes.ReadImmediate(memory, ref pc, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0x42));
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that ReadZeroPage works correctly.
    /// </summary>
    [Test]
    public void ReadZeroPage_ReadsCorrectly()
    {
        // Arrange
        memory.Write(0x1000, 0x50); // ZP address
        memory.Write(0x50, 0x99); // Value at ZP

        // Act
        byte value = AddressingModes.ReadZeroPage(memory, ref pc, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0x99));
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(2));
    }

    /// <summary>
    /// Verifies that ReadZeroPageX works correctly.
    /// </summary>
    [Test]
    public void ReadZeroPageX_ReadsCorrectly()
    {
        // Arrange
        memory.Write(0x1000, 0x10); // Base ZP address
        memory.Write(0x20, 0xAA); // Value at $10 + $10 = $20
        byte x = 0x10;

        // Act
        byte value = AddressingModes.ReadZeroPageX(memory, ref pc, x, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0xAA));
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that ReadZeroPageX wraps around in zero page.
    /// </summary>
    [Test]
    public void ReadZeroPageX_WrapsAround()
    {
        // Arrange
        memory.Write(0x1000, 0xFF); // Base ZP address
        memory.Write(0x04, 0xBB); // Value at ($FF + $05) & 0xFF = $04
        byte x = 0x05;

        // Act
        byte value = AddressingModes.ReadZeroPageX(memory, ref pc, x, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0xBB));
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that ReadZeroPageY works correctly.
    /// </summary>
    [Test]
    public void ReadZeroPageY_ReadsCorrectly()
    {
        // Arrange
        memory.Write(0x1000, 0x30); // Base ZP address
        memory.Write(0x38, 0xCC); // Value at $30 + $08 = $38
        byte y = 0x08;

        // Act
        byte value = AddressingModes.ReadZeroPageY(memory, ref pc, y, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0xCC));
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that ReadAbsolute works correctly.
    /// </summary>
    [Test]
    public void ReadAbsolute_ReadsCorrectly()
    {
        // Arrange
        memory.WriteWord(0x1000, 0x2000); // Absolute address
        memory.Write(0x2000, 0xDD); // Value at absolute address

        // Act
        byte value = AddressingModes.ReadAbsolute(memory, ref pc, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0xDD));
        Assert.That(pc, Is.EqualTo(0x1002));
        Assert.That(cycles, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that ReadAbsoluteX works correctly without page crossing.
    /// </summary>
    [Test]
    public void ReadAbsoluteX_NoPageCross_ReadsCorrectly()
    {
        // Arrange
        memory.WriteWord(0x1000, 0x2000); // Base address
        memory.Write(0x2010, 0xEE); // Value at $2000 + $10 = $2010
        byte x = 0x10;

        // Act
        byte value = AddressingModes.ReadAbsoluteX(memory, ref pc, x, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0xEE));
        Assert.That(pc, Is.EqualTo(0x1002));
        Assert.That(cycles, Is.EqualTo(3)); // No page cross
    }

    /// <summary>
    /// Verifies that ReadAbsoluteX adds cycle on page crossing.
    /// </summary>
    [Test]
    public void ReadAbsoluteX_PageCross_AddsCycle()
    {
        // Arrange
        memory.WriteWord(0x1000, 0x20FF); // Base address
        memory.Write(0x2100, 0xFF); // Value at $20FF + $01 = $2100 (page cross)
        byte x = 0x01;

        // Act
        byte value = AddressingModes.ReadAbsoluteX(memory, ref pc, x, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0xFF));
        Assert.That(pc, Is.EqualTo(0x1002));
        Assert.That(cycles, Is.EqualTo(4)); // Page cross adds 1 cycle
    }

    /// <summary>
    /// Verifies that ReadAbsoluteY works correctly without page crossing.
    /// </summary>
    [Test]
    public void ReadAbsoluteY_NoPageCross_ReadsCorrectly()
    {
        // Arrange
        memory.WriteWord(0x1000, 0x3000); // Base address
        memory.Write(0x3020, 0x11); // Value at $3000 + $20 = $3020
        byte y = 0x20;

        // Act
        byte value = AddressingModes.ReadAbsoluteY(memory, ref pc, y, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0x11));
        Assert.That(pc, Is.EqualTo(0x1002));
        Assert.That(cycles, Is.EqualTo(3)); // No page cross
    }

    /// <summary>
    /// Verifies that ReadAbsoluteY adds cycle on page crossing.
    /// </summary>
    [Test]
    public void ReadAbsoluteY_PageCross_AddsCycle()
    {
        // Arrange
        memory.WriteWord(0x1000, 0x30FF); // Base address
        memory.Write(0x3100, 0x22); // Value at $30FF + $01 = $3100 (page cross)
        byte y = 0x01;

        // Act
        byte value = AddressingModes.ReadAbsoluteY(memory, ref pc, y, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0x22));
        Assert.That(pc, Is.EqualTo(0x1002));
        Assert.That(cycles, Is.EqualTo(4)); // Page cross adds 1 cycle
    }

    /// <summary>
    /// Verifies that ReadIndirectX works correctly.
    /// </summary>
    [Test]
    public void ReadIndirectX_ReadsCorrectly()
    {
        // Arrange
        memory.Write(0x1000, 0x40); // Base ZP address
        memory.WriteWord(0x45, 0x4000); // Pointer at ($40 + $05) = $45
        memory.Write(0x4000, 0x33); // Value at indirect address
        byte x = 0x05;

        // Act
        byte value = AddressingModes.ReadIndirectX(memory, ref pc, x, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0x33));
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(5));
    }

    /// <summary>
    /// Verifies that ReadIndirectY works correctly without page crossing.
    /// </summary>
    [Test]
    public void ReadIndirectY_NoPageCross_ReadsCorrectly()
    {
        // Arrange
        memory.Write(0x1000, 0x50); // ZP address
        memory.WriteWord(0x50, 0x5000); // Pointer at ZP $50
        memory.Write(0x5010, 0x44); // Value at ($5000 + $10) = $5010
        byte y = 0x10;

        // Act
        byte value = AddressingModes.ReadIndirectY(memory, ref pc, y, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0x44));
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(4)); // No page cross
    }

    /// <summary>
    /// Verifies that ReadIndirectY adds cycle on page crossing.
    /// </summary>
    [Test]
    public void ReadIndirectY_PageCross_AddsCycle()
    {
        // Arrange
        memory.Write(0x1000, 0x60); // ZP address
        memory.WriteWord(0x60, 0x60FF); // Pointer at ZP $60
        memory.Write(0x6100, 0x55); // Value at ($60FF + $01) = $6100 (page cross)
        byte y = 0x01;

        // Act
        byte value = AddressingModes.ReadIndirectY(memory, ref pc, y, ref cycles);

        // Assert
        Assert.That(value, Is.EqualTo(0x55));
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(5)); // Page cross adds 1 cycle
    }

    /// <summary>
    /// Verifies that WriteZeroPage works correctly.
    /// </summary>
    [Test]
    public void WriteZeroPage_WritesCorrectly()
    {
        // Arrange
        memory.Write(0x1000, 0x70); // ZP address

        // Act
        AddressingModes.WriteZeroPage(memory, ref pc, 0x66, ref cycles);

        // Assert
        Assert.That(memory.Read(0x70), Is.EqualTo(0x66));
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(2));
    }

    /// <summary>
    /// Verifies that WriteZeroPageX works correctly.
    /// </summary>
    [Test]
    public void WriteZeroPageX_WritesCorrectly()
    {
        // Arrange
        memory.Write(0x1000, 0x10); // Base ZP address
        byte x = 0x05;

        // Act
        AddressingModes.WriteZeroPageX(memory, ref pc, x, 0x77, ref cycles);

        // Assert
        Assert.That(memory.Read(0x15), Is.EqualTo(0x77)); // $10 + $05 = $15
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that WriteZeroPageY works correctly.
    /// </summary>
    [Test]
    public void WriteZeroPageY_WritesCorrectly()
    {
        // Arrange
        memory.Write(0x1000, 0x20); // Base ZP address
        byte y = 0x08;

        // Act
        AddressingModes.WriteZeroPageY(memory, ref pc, y, 0x88, ref cycles);

        // Assert
        Assert.That(memory.Read(0x28), Is.EqualTo(0x88)); // $20 + $08 = $28
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that WriteAbsolute works correctly.
    /// </summary>
    [Test]
    public void WriteAbsolute_WritesCorrectly()
    {
        // Arrange
        memory.WriteWord(0x1000, 0x7000); // Absolute address

        // Act
        AddressingModes.WriteAbsolute(memory, ref pc, 0x99, ref cycles);

        // Assert
        Assert.That(memory.Read(0x7000), Is.EqualTo(0x99));
        Assert.That(pc, Is.EqualTo(0x1002));
        Assert.That(cycles, Is.EqualTo(4));
    }

    /// <summary>
    /// Verifies that WriteAbsoluteX works correctly.
    /// </summary>
    [Test]
    public void WriteAbsoluteX_WritesCorrectly()
    {
        // Arrange
        memory.WriteWord(0x1000, 0x8000); // Base address
        byte x = 0x10;

        // Act
        AddressingModes.WriteAbsoluteX(memory, ref pc, x, 0xAA, ref cycles);

        // Assert
        Assert.That(memory.Read(0x8010), Is.EqualTo(0xAA)); // $8000 + $10 = $8010
        Assert.That(pc, Is.EqualTo(0x1002));
        Assert.That(cycles, Is.EqualTo(4));
    }

    /// <summary>
    /// Verifies that WriteAbsoluteY works correctly.
    /// </summary>
    [Test]
    public void WriteAbsoluteY_WritesCorrectly()
    {
        // Arrange
        memory.WriteWord(0x1000, 0x9000); // Base address
        byte y = 0x20;

        // Act
        AddressingModes.WriteAbsoluteY(memory, ref pc, y, 0xBB, ref cycles);

        // Assert
        Assert.That(memory.Read(0x9020), Is.EqualTo(0xBB)); // $9000 + $20 = $9020
        Assert.That(pc, Is.EqualTo(0x1002));
        Assert.That(cycles, Is.EqualTo(4));
    }

    /// <summary>
    /// Verifies that WriteIndirectX works correctly.
    /// </summary>
    [Test]
    public void WriteIndirectX_WritesCorrectly()
    {
        // Arrange
        memory.Write(0x1000, 0x30); // Base ZP address
        memory.WriteWord(0x35, 0xA000); // Pointer at ($30 + $05) = $35
        byte x = 0x05;

        // Act
        AddressingModes.WriteIndirectX(memory, ref pc, x, 0xCC, ref cycles);

        // Assert
        Assert.That(memory.Read(0xA000), Is.EqualTo(0xCC));
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(5));
    }

    /// <summary>
    /// Verifies that WriteIndirectY works correctly.
    /// </summary>
    [Test]
    public void WriteIndirectY_WritesCorrectly()
    {
        // Arrange
        memory.Write(0x1000, 0x40); // ZP address
        memory.WriteWord(0x40, 0xB000); // Pointer at ZP $40
        byte y = 0x15;

        // Act
        AddressingModes.WriteIndirectY(memory, ref pc, y, 0xDD, ref cycles);

        // Assert
        Assert.That(memory.Read(0xB015), Is.EqualTo(0xDD)); // $B000 + $15 = $B015
        Assert.That(pc, Is.EqualTo(0x1001));
        Assert.That(cycles, Is.EqualTo(5));
    }
}