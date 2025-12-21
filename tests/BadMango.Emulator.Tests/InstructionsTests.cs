// <copyright file="InstructionsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Core;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Memory;

/// <summary>
/// Comprehensive unit tests for instruction implementations.
/// </summary>
[TestFixture]
public class InstructionsTests
{
    private const byte FlagC = 0x01; // Carry
    private const byte FlagZ = 0x02; // Zero
    private const byte FlagI = 0x04; // Interrupt Disable
    private const byte FlagD = 0x08; // Decimal
    private const byte FlagV = 0x40; // Overflow
    private const byte FlagN = 0x80; // Negative

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

    #region LDA Tests

    /// <summary>
    /// Verifies that LDA loads value and sets zero flag.
    /// </summary>
    [Test]
    public void LDA_LoadsZeroAndSetsZeroFlag()
    {
        // Arrange
        memory.Write(0x1000, 0x00);
        var state = new Cpu65C02State { PC = 0x1000, A = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.LDA(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x00));
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
        Assert.That(state.P & FlagN, Is.EqualTo(0), "Negative flag should be clear");
    }

    /// <summary>
    /// Verifies that LDA loads value and sets negative flag.
    /// </summary>
    [Test]
    public void LDA_LoadsNegativeValueAndSetsNegativeFlag()
    {
        // Arrange
        memory.Write(0x1000, 0x80);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.LDA(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x80));
        Assert.That(state.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set");
        Assert.That(state.P & FlagZ, Is.EqualTo(0), "Zero flag should be clear");
    }

    /// <summary>
    /// Verifies that LDA loads positive value and clears both flags.
    /// </summary>
    [Test]
    public void LDA_LoadsPositiveValueAndClearsBothFlags()
    {
        // Arrange
        memory.Write(0x1000, 0x42);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, P = FlagZ | FlagN, Cycles = 10 };

        // Act
        var handler = Instructions.LDA(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x42));
        Assert.That(state.P & FlagZ, Is.EqualTo(0), "Zero flag should be clear");
        Assert.That(state.P & FlagN, Is.EqualTo(0), "Negative flag should be clear");
    }

    #endregion

    #region LDX Tests

    /// <summary>
    /// Verifies that LDX loads value into X register and sets zero flag.
    /// </summary>
    [Test]
    public void LDX_LoadsZeroAndSetsZeroFlag()
    {
        // Arrange
        memory.Write(0x1000, 0x00);
        var state = new Cpu65C02State { PC = 0x1000, X = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.LDX(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.X, Is.EqualTo(0x00));
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
        Assert.That(state.P & FlagN, Is.EqualTo(0), "Negative flag should be clear");
    }

    /// <summary>
    /// Verifies that LDX loads value and sets negative flag.
    /// </summary>
    [Test]
    public void LDX_LoadsNegativeValueAndSetsNegativeFlag()
    {
        // Arrange
        memory.Write(0x1000, 0x90);
        var state = new Cpu65C02State { PC = 0x1000, X = 0x00, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.LDX(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.X, Is.EqualTo(0x90));
        Assert.That(state.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set");
    }

    #endregion

    #region LDY Tests

    /// <summary>
    /// Verifies that LDY loads value into Y register and sets zero flag.
    /// </summary>
    [Test]
    public void LDY_LoadsZeroAndSetsZeroFlag()
    {
        // Arrange
        memory.Write(0x1000, 0x00);
        var state = new Cpu65C02State { PC = 0x1000, Y = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.LDY(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.Y, Is.EqualTo(0x00));
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
    }

    /// <summary>
    /// Verifies that LDY loads value and sets negative flag.
    /// </summary>
    [Test]
    public void LDY_LoadsNegativeValueAndSetsNegativeFlag()
    {
        // Arrange
        memory.Write(0x1000, 0xA0);
        var state = new Cpu65C02State { PC = 0x1000, Y = 0x00, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.LDY(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.Y, Is.EqualTo(0xA0));
        Assert.That(state.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set");
    }

    #endregion

    #region STA Tests

    /// <summary>
    /// Verifies that STA stores accumulator value to memory.
    /// </summary>
    [Test]
    public void STA_StoresAccumulatorToMemory()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x42, Cycles = 10 };
        memory.Write(0x1000, 0x50); // ZP address

        // Act
        var handler = Instructions.STA(AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(memory.Read(0x0050), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies that STA doesn't affect processor flags.
    /// </summary>
    [Test]
    public void STA_DoesNotAffectFlags()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, P = FlagZ | FlagN, Cycles = 10 };
        memory.Write(0x1000, 0x60); // ZP address

        // Act
        var handler = Instructions.STA(AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P, Is.EqualTo(FlagZ | FlagN), "Flags should not be modified");
    }

    #endregion

    #region NOP Tests

    /// <summary>
    /// Verifies that NOP does nothing but consume cycles.
    /// </summary>
    [Test]
    public void NOP_DoesNothingButConsumesCycles()
    {
        // Arrange
        var state = new Cpu65C02State
        {
            PC = 0x1000,
            A = 0x42,
            X = 0x12,
            Y = 0x34,
            SP = 0xFD,
            P = FlagZ | FlagN,
            Cycles = 10,
        };

        // Act
        var handler = Instructions.NOP(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.PC, Is.EqualTo(0x1000), "PC should not change");
        Assert.That(state.A, Is.EqualTo(0x42), "A should not change");
        Assert.That(state.X, Is.EqualTo(0x12), "X should not change");
        Assert.That(state.Y, Is.EqualTo(0x34), "Y should not change");
        Assert.That(state.SP, Is.EqualTo(0xFD), "SP should not change");
        Assert.That(state.P, Is.EqualTo(FlagZ | FlagN), "Flags should not change");
        Assert.That(state.Cycles, Is.EqualTo(11), "Should consume 1 cycle");
    }

    #endregion

    #region Flag Instruction Tests

    /// <summary>
    /// Verifies that CLC clears the carry flag.
    /// </summary>
    [Test]
    public void CLC_ClearsCarryFlag()
    {
        // Arrange
        var state = new Cpu65C02State { P = 0xFF, Cycles = 10 }; // All flags set

        // Act
        var handler = Instructions.CLC(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagC, Is.EqualTo(0), "Carry flag should be clear");
        Assert.That(state.P & ~FlagC, Is.EqualTo(0xFF & ~FlagC), "Other flags should be unchanged");
        Assert.That(state.Cycles, Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that SEC sets the carry flag.
    /// </summary>
    [Test]
    public void SEC_SetsCarryFlag()
    {
        // Arrange
        var state = new Cpu65C02State { P = 0x00, Cycles = 10 }; // All flags clear

        // Act
        var handler = Instructions.SEC(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagC, Is.EqualTo(FlagC), "Carry flag should be set");
        Assert.That(state.Cycles, Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that CLI clears the interrupt disable flag.
    /// </summary>
    [Test]
    public void CLI_ClearsInterruptDisableFlag()
    {
        // Arrange
        var state = new Cpu65C02State { P = 0xFF, Cycles = 10 }; // All flags set

        // Act
        var handler = Instructions.CLI(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagI, Is.EqualTo(0), "Interrupt disable flag should be clear");
        Assert.That(state.P & ~FlagI, Is.EqualTo(0xFF & ~FlagI), "Other flags should be unchanged");
        Assert.That(state.Cycles, Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that SEI sets the interrupt disable flag.
    /// </summary>
    [Test]
    public void SEI_SetsInterruptDisableFlag()
    {
        // Arrange
        var state = new Cpu65C02State { P = 0x00, Cycles = 10 }; // All flags clear

        // Act
        var handler = Instructions.SEI(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagI, Is.EqualTo(FlagI), "Interrupt disable flag should be set");
        Assert.That(state.Cycles, Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that CLD clears the decimal mode flag.
    /// </summary>
    [Test]
    public void CLD_ClearsDecimalModeFlag()
    {
        // Arrange
        var state = new Cpu65C02State { P = 0xFF, Cycles = 10 }; // All flags set

        // Act
        var handler = Instructions.CLD(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagD, Is.EqualTo(0), "Decimal mode flag should be clear");
        Assert.That(state.P & ~FlagD, Is.EqualTo(0xFF & ~FlagD), "Other flags should be unchanged");
        Assert.That(state.Cycles, Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that SED sets the decimal mode flag.
    /// </summary>
    [Test]
    public void SED_SetsDecimalModeFlag()
    {
        // Arrange
        var state = new Cpu65C02State { P = 0x00, Cycles = 10 }; // All flags clear

        // Act
        var handler = Instructions.SED(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagD, Is.EqualTo(FlagD), "Decimal mode flag should be set");
        Assert.That(state.Cycles, Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that CLV clears the overflow flag.
    /// </summary>
    [Test]
    public void CLV_ClearsOverflowFlag()
    {
        // Arrange
        var state = new Cpu65C02State { P = 0xFF, Cycles = 10 }; // All flags set

        // Act
        var handler = Instructions.CLV(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagV, Is.EqualTo(0), "Overflow flag should be clear");
        Assert.That(state.P & ~FlagV, Is.EqualTo(0xFF & ~FlagV), "Other flags should be unchanged");
        Assert.That(state.Cycles, Is.EqualTo(11));
    }

    #endregion

    #region Integration Tests with Opcode Table

    /// <summary>
    /// Verifies that CLC instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void CLC_ViaOpcodeTable_ClearsCarryFlag()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0x18); // CLC opcode
        cpu.Reset();
        var state = cpu.GetState();
        state.P = 0xFF; // Set all flags
        cpu.SetState(state);

        // Act
        cpu.Step();

        // Assert
        state = cpu.GetState();
        Assert.That(state.P & FlagC, Is.EqualTo(0), "Carry flag should be clear");
        Assert.That(state.PC, Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that SEC instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void SEC_ViaOpcodeTable_SetsCarryFlag()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0x38); // SEC opcode
        cpu.Reset();

        // Act
        cpu.Step();

        // Assert
        var state = cpu.GetState();
        Assert.That(state.P & FlagC, Is.EqualTo(FlagC), "Carry flag should be set");
        Assert.That(state.PC, Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that CLI instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void CLI_ViaOpcodeTable_ClearsInterruptDisableFlag()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0x58); // CLI opcode
        cpu.Reset();
        var state = cpu.GetState();
        state.P = 0xFF; // Set all flags
        cpu.SetState(state);

        // Act
        cpu.Step();

        // Assert
        state = cpu.GetState();
        Assert.That(state.P & FlagI, Is.EqualTo(0), "Interrupt disable flag should be clear");
        Assert.That(state.PC, Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that SEI instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void SEI_ViaOpcodeTable_SetsInterruptDisableFlag()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0x78); // SEI opcode
        cpu.Reset();

        // Act
        cpu.Step();

        // Assert
        var state = cpu.GetState();
        Assert.That(state.P & FlagI, Is.EqualTo(FlagI), "Interrupt disable flag should be set");
        Assert.That(state.PC, Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that CLD instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void CLD_ViaOpcodeTable_ClearsDecimalModeFlag()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xD8); // CLD opcode
        cpu.Reset();
        var state = cpu.GetState();
        state.P = 0xFF; // Set all flags
        cpu.SetState(state);

        // Act
        cpu.Step();

        // Assert
        state = cpu.GetState();
        Assert.That(state.P & FlagD, Is.EqualTo(0), "Decimal mode flag should be clear");
        Assert.That(state.PC, Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that SED instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void SED_ViaOpcodeTable_SetsDecimalModeFlag()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xF8); // SED opcode
        cpu.Reset();

        // Act
        cpu.Step();

        // Assert
        var state = cpu.GetState();
        Assert.That(state.P & FlagD, Is.EqualTo(FlagD), "Decimal mode flag should be set");
        Assert.That(state.PC, Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that CLV instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void CLV_ViaOpcodeTable_ClearsOverflowFlag()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xB8); // CLV opcode
        cpu.Reset();
        var state = cpu.GetState();
        state.P = 0xFF; // Set all flags
        cpu.SetState(state);

        // Act
        cpu.Step();

        // Assert
        state = cpu.GetState();
        Assert.That(state.P & FlagV, Is.EqualTo(0), "Overflow flag should be clear");
        Assert.That(state.PC, Is.EqualTo(0x1001));
    }

    #endregion
}