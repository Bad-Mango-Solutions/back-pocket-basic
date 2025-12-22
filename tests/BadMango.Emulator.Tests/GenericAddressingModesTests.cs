// <copyright file="GenericAddressingModesTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Core;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Memory;

/// <summary>
/// Tests for generic addressing mode implementations.
/// </summary>
[TestFixture]
public class GenericAddressingModesTests
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
    /// Verifies that generic Implied addressing mode works with Cpu65C02State.
    /// </summary>
    [Test]
    public void Generic_Implied_WorksWithCpu65C02State()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, Cycles = 10 };

        // Act - Using generic class directly
        Addr address = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.Implied(memory, ref state);

        // Assert
        Assert.That(address, Is.EqualTo(0));
        Assert.That(state.PC, Is.EqualTo(0x1000)); // PC unchanged
        Assert.That(state.Cycles, Is.EqualTo(10)); // Cycles unchanged
    }

    /// <summary>
    /// Verifies that generic Immediate addressing mode works with Cpu65C02State.
    /// </summary>
    [Test]
    public void Generic_Immediate_WorksWithCpu65C02State()
    {
        // Arrange
        memory.Write(0x1000, 0x42);
        var state = new Cpu65C02State { PC = 0x1000, Cycles = 10 };

        // Act - Using generic class directly
        Addr address = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.Immediate(memory, ref state);

        // Assert
        Assert.That(address, Is.EqualTo(0x1000));
        Assert.That(state.PC, Is.EqualTo(0x1001)); // PC incremented
        Assert.That(state.Cycles, Is.EqualTo(10)); // No extra cycles for immediate
    }

    /// <summary>
    /// Verifies that helper alias works correctly.
    /// </summary>
    [Test]
    public void Helper_Implied_WorksWithCpu65C02State()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, Cycles = 10 };

        // Act - Using helper alias
        Addr address = AddressingModesHelpers.Cpu65C02.Implied(memory, ref state);

        // Assert
        Assert.That(address, Is.EqualTo(0));
        Assert.That(state.PC, Is.EqualTo(0x1000)); // PC unchanged
        Assert.That(state.Cycles, Is.EqualTo(10)); // Cycles unchanged
    }

    /// <summary>
    /// Verifies that generic ZeroPage addressing mode works correctly.
    /// </summary>
    [Test]
    public void Generic_ZeroPage_WorksWithCpu65C02State()
    {
        // Arrange
        memory.Write(0x1000, 0x42);
        var state = new Cpu65C02State { PC = 0x1000, Cycles = 10 };

        // Act
        Addr address = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.ZeroPage(memory, ref state);

        // Assert
        Assert.That(address, Is.EqualTo(0x0042));
        Assert.That(state.PC, Is.EqualTo(0x1001));
        Assert.That(state.Cycles, Is.EqualTo(11)); // +1 cycle for ZP fetch
    }

    /// <summary>
    /// Verifies that generic ZeroPageX addressing mode works correctly.
    /// </summary>
    [Test]
    public void Generic_ZeroPageX_WorksWithCpu65C02State()
    {
        // Arrange
        memory.Write(0x1000, 0xF0);
        var state = new Cpu65C02State { PC = 0x1000, X = 0x20, Cycles = 10 };

        // Act
        Addr address = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.ZeroPageX(memory, ref state);

        // Assert
        Assert.That(address, Is.EqualTo(0x0010)); // (0xF0 + 0x20) & 0xFF = 0x10 (zero page wrap)
        Assert.That(state.PC, Is.EqualTo(0x1001));
        Assert.That(state.Cycles, Is.EqualTo(12)); // +2 cycles (fetch + index)
    }

    /// <summary>
    /// Verifies that generic Absolute addressing mode works correctly.
    /// </summary>
    [Test]
    public void Generic_Absolute_WorksWithCpu65C02State()
    {
        // Arrange
        memory.Write(0x1000, 0x34); // Low byte
        memory.Write(0x1001, 0x12); // High byte
        var state = new Cpu65C02State { PC = 0x1000, Cycles = 10 };

        // Act
        Addr address = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.Absolute(memory, ref state);

        // Assert
        Assert.That(address, Is.EqualTo(0x1234));
        Assert.That(state.PC, Is.EqualTo(0x1002));
        Assert.That(state.Cycles, Is.EqualTo(12)); // +2 cycles for 16-bit address fetch
    }

    /// <summary>
    /// Verifies that generic AbsoluteX addressing mode handles page boundary crossing.
    /// </summary>
    [Test]
    public void Generic_AbsoluteX_HandlesPageBoundaryCrossing()
    {
        // Arrange
        memory.Write(0x1000, 0xFF); // Low byte
        memory.Write(0x1001, 0x10); // High byte  - address is 0x10FF
        var state = new Cpu65C02State { PC = 0x1000, X = 0x02, Cycles = 10 };

        // Act
        Addr address = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.AbsoluteX(memory, ref state);

        // Assert
        Assert.That(address, Is.EqualTo(0x1101)); // 0x10FF + 0x02 = 0x1101 (crosses page boundary)
        Assert.That(state.PC, Is.EqualTo(0x1002));
        Assert.That(state.Cycles, Is.EqualTo(13)); // +2 for address fetch, +1 for page boundary cross
    }

    /// <summary>
    /// Demonstrates that generic addressing modes can work with delegate creation.
    /// </summary>
    [Test]
    public void Generic_AddressingMode_CanBeUsedAsDelegate()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, Cycles = 10 };

        // Act - Create a delegate from the generic method
        AddressingMode<Cpu65C02State> impliedMode = AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.Implied;
        Addr address = impliedMode(memory, ref state);

        // Assert
        Assert.That(address, Is.EqualTo(0));
    }
}
