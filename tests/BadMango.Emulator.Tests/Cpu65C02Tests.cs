// <copyright file="Cpu65C02Tests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Core;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Memory;

/// <summary>
/// Unit tests for the <see cref="Cpu65C02"/> class.
/// </summary>
[TestFixture]
public class Cpu65C02Tests
{
    private IMemory memory = null!;
    private Cpu65C02 cpu = null!;

    /// <summary>
    /// Sets up the test environment by initializing memory and CPU.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        memory = new BasicMemory();
        cpu = new Cpu65C02(memory);
    }

    /// <summary>
    /// Verifies that Reset() initializes the CPU to the correct state.
    /// </summary>
    [Test]
    public void Reset_InitializesCpuCorrectly()
    {
        // Arrange: Set reset vector to 0x1000
        memory.WriteWord(0xFFFC, 0x1000);

        // Act
        cpu.Reset();

        // Assert
        var state = cpu.GetState();
        Assert.That(state.PC, Is.EqualTo(0x1000));
        Assert.That(state.S, Is.EqualTo(0xFD));
        Assert.That(state.A, Is.EqualTo(0));
        Assert.That(state.X, Is.EqualTo(0));
        Assert.That(state.Y, Is.EqualTo(0));
        Assert.That(cpu.Halted, Is.False);
    }

    /// <summary>
    /// Verifies that LDA immediate loads the accumulator correctly.
    /// </summary>
    [Test]
    public void LDA_Immediate_LoadsAccumulator()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xA9); // LDA #$42
        memory.Write(0x1001, 0x42);
        cpu.Reset();

        // Act
        int cycles = cpu.Step();

        // Assert
        var state = cpu.GetState();
        Assert.That(state.A, Is.EqualTo(0x42));
        Assert.That(cycles, Is.EqualTo(2));
        Assert.That(state.PC, Is.EqualTo(0x1002));
    }

    /// <summary>
    /// Verifies that LDA sets the Zero flag correctly.
    /// </summary>
    [Test]
    public void LDA_SetsZeroFlag()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xA9); // LDA #$00
        memory.Write(0x1001, 0x00);
        cpu.Reset();

        // Act
        cpu.Step();

        // Assert
        var state = cpu.GetState();
        Assert.That(state.P & 0x02, Is.EqualTo(0x02)); // Zero flag set
    }

    /// <summary>
    /// Verifies that LDA sets the Negative flag correctly.
    /// </summary>
    [Test]
    public void LDA_SetsNegativeFlag()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xA9); // LDA #$FF
        memory.Write(0x1001, 0xFF);
        cpu.Reset();

        // Act
        cpu.Step();

        // Assert
        var state = cpu.GetState();
        Assert.That(state.P & 0x80, Is.EqualTo(0x80)); // Negative flag set
    }

    /// <summary>
    /// Verifies that STA stores the accumulator to memory.
    /// </summary>
    [Test]
    public void STA_ZeroPage_StoresAccumulator()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xA9); // LDA #$42
        memory.Write(0x1001, 0x42);
        memory.Write(0x1002, 0x85); // STA $10
        memory.Write(0x1003, 0x10);
        cpu.Reset();

        // Act
        cpu.Step(); // LDA
        cpu.Step(); // STA

        // Assert
        Assert.That(memory.Read(0x10), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies that LDA from zero page works correctly.
    /// </summary>
    [Test]
    public void LDA_ZeroPage_LoadsFromMemory()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x10, 0x99); // Value in zero page
        memory.Write(0x1000, 0xA5); // LDA $10
        memory.Write(0x1001, 0x10);
        cpu.Reset();

        // Act
        int cycles = cpu.Step();

        // Assert
        var state = cpu.GetState();
        Assert.That(state.A, Is.EqualTo(0x99));
        Assert.That(cycles, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that LDA absolute addressing works correctly.
    /// </summary>
    [Test]
    public void LDA_Absolute_LoadsFromMemory()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x2000, 0x55); // Value at absolute address
        memory.Write(0x1000, 0xAD); // LDA $2000
        memory.WriteWord(0x1001, 0x2000);
        cpu.Reset();

        // Act
        int cycles = cpu.Step();

        // Assert
        var state = cpu.GetState();
        Assert.That(state.A, Is.EqualTo(0x55));
        Assert.That(cycles, Is.EqualTo(4));
    }

    /// <summary>
    /// Verifies that NOP executes correctly.
    /// </summary>
    [Test]
    public void NOP_ExecutesCorrectly()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xEA); // NOP
        cpu.Reset();
        var stateBefore = cpu.GetState();

        // Act
        int cycles = cpu.Step();

        // Assert
        var stateAfter = cpu.GetState();
        Assert.That(cycles, Is.EqualTo(2));
        Assert.That(stateAfter.PC, Is.EqualTo(stateBefore.PC + 1));
        Assert.That(stateAfter.A, Is.EqualTo(stateBefore.A));
        Assert.That(stateAfter.X, Is.EqualTo(stateBefore.X));
        Assert.That(stateAfter.Y, Is.EqualTo(stateBefore.Y));
    }

    /// <summary>
    /// Verifies that GetState and SetState work correctly.
    /// </summary>
    [Test]
    public void GetState_SetState_WorkCorrectly()
    {
        // Arrange
        var originalState = new Cpu65C02State
        {
            A = 0x42,
            X = 0x10,
            Y = 0x20,
            S = 0xFF,
            P = 0x30,
            PC = 0x1234,
            Cycles = 100,
        };

        // Act
        cpu.SetState(originalState);
        var retrievedState = cpu.GetState();

        // Assert
        Assert.That(retrievedState.A, Is.EqualTo(originalState.A));
        Assert.That(retrievedState.X, Is.EqualTo(originalState.X));
        Assert.That(retrievedState.Y, Is.EqualTo(originalState.Y));
        Assert.That(retrievedState.S, Is.EqualTo(originalState.S));
        Assert.That(retrievedState.P, Is.EqualTo(originalState.P));
        Assert.That(retrievedState.PC, Is.EqualTo(originalState.PC));
        Assert.That(retrievedState.Cycles, Is.EqualTo(originalState.Cycles));
    }

    /// <summary>
    /// Verifies that BRK halts the CPU.
    /// </summary>
    [Test]
    public void BRK_HaltsCpu()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0x00); // BRK
        cpu.Reset();

        // Act
        cpu.Step();

        // Assert
        Assert.That(cpu.Halted, Is.True);
    }

    /// <summary>
    /// Verifies that cycle counting is accurate.
    /// </summary>
    [Test]
    public void CycleCounting_IsAccurate()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xA9); // LDA #$42 (2 cycles)
        memory.Write(0x1001, 0x42);
        memory.Write(0x1002, 0xEA); // NOP (2 cycles)
        memory.Write(0x1003, 0xA5); // LDA $10 (3 cycles)
        memory.Write(0x1004, 0x10);
        cpu.Reset();

        // Act
        cpu.Step(); // LDA immediate
        cpu.Step(); // NOP
        cpu.Step(); // LDA zero page

        // Assert
        var state = cpu.GetState();
        Assert.That(state.Cycles, Is.EqualTo(2 + 2 + 3));
    }

    /// <summary>
    /// Verifies that GetRegisters returns only register state without cycle count.
    /// </summary>
    [Test]
    public void GetRegisters_ReturnsRegisterStateOnly()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xA9); // LDA #$42
        memory.Write(0x1001, 0x42);
        cpu.Reset();
        cpu.Step();

        // Act
        var registers = cpu.GetRegisters();

        // Assert
        Assert.That(registers.A, Is.EqualTo(0x42));
        Assert.That(registers.PC, Is.EqualTo(0x1002));
        Assert.That(registers.S, Is.EqualTo(0xFD));

        // Verify the registers struct doesn't have a Cycles property
        var registersType = typeof(Cpu65C02Registers);
        Assert.That(registersType.GetProperty("Cycles"), Is.Null);
    }
}