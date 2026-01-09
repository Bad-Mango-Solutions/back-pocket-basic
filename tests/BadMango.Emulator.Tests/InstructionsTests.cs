// <copyright file="InstructionsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;

using Emulation.Cpu;

using TestHelpers;

/// <summary>
/// Comprehensive unit tests for instruction implementations.
/// </summary>
[TestFixture]
public class InstructionsTests : CpuTestBase
{
    private const ProcessorStatusFlags FlagC = ProcessorStatusFlags.C;
    private const ProcessorStatusFlags FlagZ = ProcessorStatusFlags.Z;
    private const ProcessorStatusFlags FlagI = ProcessorStatusFlags.I;
    private const ProcessorStatusFlags FlagD = ProcessorStatusFlags.D;
    private const ProcessorStatusFlags FlagV = ProcessorStatusFlags.V;
    private const ProcessorStatusFlags FlagN = ProcessorStatusFlags.N;

    /// <summary>
    /// Sets up test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        Cpu.Reset();
    }

    #region LDA Tests

    /// <summary>
    /// Verifies that LDA loads value and sets zero flag.
    /// </summary>
    [Test]
    public void LDA_LoadsZeroAndSetsZeroFlag()
    {
        // Arrange
        Write(0x1000, 0x00);
        SetupCpu(pc: 0x1000, a: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDA(AddressingModes.Immediate);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x00));
        Assert.That(Cpu.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
        Assert.That(Cpu.Registers.P & FlagN, Is.EqualTo((ProcessorStatusFlags)0), "Negative flag should be clear");
    }

    /// <summary>
    /// Verifies that LDA loads value and sets negative flag.
    /// </summary>
    [Test]
    public void LDA_LoadsNegativeValueAndSetsNegativeFlag()
    {
        // Arrange
        Write(0x1000, 0x80);
        SetupCpu(pc: 0x1000, a: 0x00, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDA(AddressingModes.Immediate);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x80));
        Assert.That(Cpu.Registers.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set");
        Assert.That(Cpu.Registers.P & FlagZ, Is.EqualTo((ProcessorStatusFlags)0), "Zero flag should be clear");
    }

    /// <summary>
    /// Verifies that LDA loads positive value and clears both flags.
    /// </summary>
    [Test]
    public void LDA_LoadsPositiveValueAndClearsBothFlags()
    {
        // Arrange
        Write(0x1000, 0x42);
        SetupCpu(pc: 0x1000, a: 0x00, p: FlagZ | FlagN, cycles: 10);

        // Act
        var handler = Instructions.LDA(AddressingModes.Immediate);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x42));
        Assert.That(Cpu.Registers.P & FlagZ, Is.EqualTo((ProcessorStatusFlags)0), "Zero flag should be clear");
        Assert.That(Cpu.Registers.P & FlagN, Is.EqualTo((ProcessorStatusFlags)0), "Negative flag should be clear");
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
        Write(0x1000, 0x00);
        SetupCpu(pc: 0x1000, x: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDX(AddressingModes.Immediate);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.X.GetByte(), Is.EqualTo(0x00));
        Assert.That(Cpu.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
        Assert.That(Cpu.Registers.P & FlagN, Is.EqualTo((ProcessorStatusFlags)0), "Negative flag should be clear");
    }

    /// <summary>
    /// Verifies that LDX loads value and sets negative flag.
    /// </summary>
    [Test]
    public void LDX_LoadsNegativeValueAndSetsNegativeFlag()
    {
        // Arrange
        Write(0x1000, 0x90);
        SetupCpu(pc: 0x1000, x: 0x00, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDX(AddressingModes.Immediate);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.X.GetByte(), Is.EqualTo(0x90));
        Assert.That(Cpu.Registers.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set");
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
        Write(0x1000, 0x00);
        SetupCpu(pc: 0x1000, y: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDY(AddressingModes.Immediate);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.Y.GetByte(), Is.EqualTo(0x00));
        Assert.That(Cpu.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
    }

    /// <summary>
    /// Verifies that LDY loads value and sets negative flag.
    /// </summary>
    [Test]
    public void LDY_LoadsNegativeValueAndSetsNegativeFlag()
    {
        // Arrange
        Write(0x1000, 0xA0);
        SetupCpu(pc: 0x1000, y: 0x00, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDY(AddressingModes.Immediate);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.Y.GetByte(), Is.EqualTo(0xA0));
        Assert.That(Cpu.Registers.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set");
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
        SetupCpu(pc: 0x1000, a: 0x42, cycles: 10);
        Write(0x1000, 0x50); // ZP address

        // Act
        var handler = Instructions.STA(AddressingModes.ZeroPage);
        handler(Cpu);

        // Assert
        Assert.That(Read(0x0050), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies that STA doesn't affect processor flags.
    /// </summary>
    [Test]
    public void STA_DoesNotAffectFlags()
    {
        // Arrange
        SetupCpu(pc: 0x1000, a: 0x00, p: FlagZ | FlagN, cycles: 10);
        Write(0x1000, 0x60); // ZP address

        // Act
        var handler = Instructions.STA(AddressingModes.ZeroPage);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.P, Is.EqualTo(FlagZ | FlagN), "Flags should not be modified");
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
        SetupCpu(pc: 0x1000, a: 0x42, x: 0x12, y: 0x34, sp: 0xFD, p: FlagZ | FlagN, cycles: 10);

        // Act
        var handler = Instructions.NOP(AddressingModes.Implied);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1000), "PC should not change");
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x42), "RegisterAccumulator should not change");
        Assert.That(Cpu.Registers.X.GetByte(), Is.EqualTo(0x12), "X should not change");
        Assert.That(Cpu.Registers.Y.GetByte(), Is.EqualTo(0x34), "Y should not change");
        Assert.That(Cpu.Registers.SP.GetByte(), Is.EqualTo(0xFD), "SP should not change");
        Assert.That(Cpu.Registers.P, Is.EqualTo(FlagZ | FlagN), "Flags should not change");
        Assert.That(Cpu.GetCycles(), Is.EqualTo(11), "Should consume 1 cycle");
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
        SetupCpu(p: (ProcessorStatusFlags)0xFF, cycles: 10); // All flags set

        // Act
        var handler = Instructions.CLC(AddressingModes.Implied);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.P & FlagC, Is.EqualTo((ProcessorStatusFlags)0), "Carry flag should be clear");
        Assert.That(Cpu.Registers.P & ~FlagC, Is.EqualTo((ProcessorStatusFlags)0xFF & ~FlagC), "Other flags should be unchanged");
        Assert.That(Cpu.GetCycles(), Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that SEC sets the carry flag.
    /// </summary>
    [Test]
    public void SEC_SetsCarryFlag()
    {
        // Arrange
        SetupCpu(p: 0, cycles: 10); // All flags clear

        // Act
        var handler = Instructions.SEC(AddressingModes.Implied);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.P & FlagC, Is.EqualTo(FlagC), "Carry flag should be set");
        Assert.That(Cpu.GetCycles(), Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that CLI clears the interrupt disable flag.
    /// </summary>
    [Test]
    public void CLI_ClearsInterruptDisableFlag()
    {
        // Arrange
        SetupCpu(p: (ProcessorStatusFlags)0xFF, cycles: 10); // All flags set

        // Act
        var handler = Instructions.CLI(AddressingModes.Implied);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.P & FlagI, Is.EqualTo((ProcessorStatusFlags)0), "Interrupt disable flag should be clear");
        Assert.That(Cpu.Registers.P & ~FlagI, Is.EqualTo((ProcessorStatusFlags)0xFF & ~FlagI), "Other flags should be unchanged");
        Assert.That(Cpu.GetCycles(), Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that SEI sets the interrupt disable flag.
    /// </summary>
    [Test]
    public void SEI_SetsInterruptDisableFlag()
    {
        // Arrange
        SetupCpu(p: 0, cycles: 10); // All flags clear

        // Act
        var handler = Instructions.SEI(AddressingModes.Implied);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.P & FlagI, Is.EqualTo(FlagI), "Interrupt disable flag should be set");
        Assert.That(Cpu.GetCycles(), Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that CLD clears the decimal mode flag.
    /// </summary>
    [Test]
    public void CLD_ClearsDecimalModeFlag()
    {
        // Arrange
        SetupCpu(p: (ProcessorStatusFlags)0xFF, cycles: 10); // All flags set

        // Act
        var handler = Instructions.CLD(AddressingModes.Implied);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.P & FlagD, Is.EqualTo((ProcessorStatusFlags)0), "Decimal mode flag should be clear");
        Assert.That(Cpu.Registers.P & ~FlagD, Is.EqualTo((ProcessorStatusFlags)0xFF & ~FlagD), "Other flags should be unchanged");
        Assert.That(Cpu.GetCycles(), Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that SED sets the decimal mode flag.
    /// </summary>
    [Test]
    public void SED_SetsDecimalModeFlag()
    {
        // Arrange
        SetupCpu(p: 0, cycles: 10); // All flags clear

        // Act
        var handler = Instructions.SED(AddressingModes.Implied);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.P & FlagD, Is.EqualTo(FlagD), "Decimal mode flag should be set");
        Assert.That(Cpu.GetCycles(), Is.EqualTo(11));
    }

    /// <summary>
    /// Verifies that CLV clears the overflow flag.
    /// </summary>
    [Test]
    public void CLV_ClearsOverflowFlag()
    {
        // Arrange
        SetupCpu(p: (ProcessorStatusFlags)0xFF, cycles: 10); // All flags set

        // Act
        var handler = Instructions.CLV(AddressingModes.Implied);
        handler(Cpu);

        // Assert
        Assert.That(Cpu.Registers.P & FlagV, Is.EqualTo((ProcessorStatusFlags)0), "Overflow flag should be clear");
        Assert.That(Cpu.Registers.P & ~FlagV, Is.EqualTo((ProcessorStatusFlags)0xFF & ~FlagV), "Other flags should be unchanged");
        Assert.That(Cpu.GetCycles(), Is.EqualTo(11));
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
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x18); // CLC opcode
        Cpu.Reset();
        Cpu.Registers.P = (ProcessorStatusFlags)0xFF; // Set all flags

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P & FlagC, Is.EqualTo((ProcessorStatusFlags)0), "Carry flag should be clear");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that SEC instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void SEC_ViaOpcodeTable_SetsCarryFlag()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x38); // SEC opcode
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P & FlagC, Is.EqualTo(FlagC), "Carry flag should be set");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that CLI instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void CLI_ViaOpcodeTable_ClearsInterruptDisableFlag()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x58); // CLI opcode
        Cpu.Reset();

        Cpu.Registers.P = (ProcessorStatusFlags)0xFF; // Set all flags

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P & FlagI, Is.EqualTo((ProcessorStatusFlags)0), "Interrupt disable flag should be clear");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that SEI instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void SEI_ViaOpcodeTable_SetsInterruptDisableFlag()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x78); // SEI opcode
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P & FlagI, Is.EqualTo(FlagI), "Interrupt disable flag should be set");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that CLD instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void CLD_ViaOpcodeTable_ClearsDecimalModeFlag()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xD8); // CLD opcode
        Cpu.Reset();

        Cpu.Registers.P = (ProcessorStatusFlags)0xFF; // Set all flags

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P & FlagD, Is.EqualTo((ProcessorStatusFlags)0), "Decimal mode flag should be clear");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that SED instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void SED_ViaOpcodeTable_SetsDecimalModeFlag()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xF8); // SED opcode
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P & FlagD, Is.EqualTo(FlagD), "Decimal mode flag should be set");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
    }

    /// <summary>
    /// Verifies that CLV instruction works correctly via opcode table.
    /// </summary>
    [Test]
    public void CLV_ViaOpcodeTable_ClearsOverflowFlag()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xB8); // CLV opcode
        Cpu.Reset();

        Cpu.Registers.P = (ProcessorStatusFlags)0xFF; // Set all flags

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P & FlagV, Is.EqualTo((ProcessorStatusFlags)0), "Overflow flag should be clear");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
    }

    #endregion

    #region BRK Instruction Tests

    /// <summary>
    /// Test that BRK pushes correct values to the stack.
    /// </summary>
    [Test]
    public void BRK_PushesCorrectValuesToStack()
    {
        // Arrange
        // Set up interrupt vector at 0xFFFE
        WriteWord(0xFFFE, 0x8000);

        // Write BRK instruction
        Write(0x1000, 0x00); // BRK

        // Set initial state
        Cpu.Reset();

        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.SP.SetByte(0xFD); // Stack pointer starts at 0xFD
        Cpu.Registers.P = (ProcessorStatusFlags)0x24; // Some flags set

        // Act
        Cpu.Step();

        // Assert

        // Check stack pointer decremented by 3
        Assert.That(Cpu.Registers.SP.GetByte(), Is.EqualTo(0xFA), "SP should be decremented by 3");

        // Check pushed values on stack
        // BRK pushes PC+1 (0x1002 since BRK increments PC), then P with B flag set
        const byte FlagB = 0x10; // Break flag
        Assert.Multiple(() =>
        {
            Assert.That(Read(0x01FD), Is.EqualTo(0x10), "HighByte byte of return address should be on stack");
            Assert.That(Read(0x01FC), Is.EqualTo(0x02), "LowByte byte of return address should be on stack");
            Assert.That(Read(0x01FB) & FlagB, Is.EqualTo(FlagB), "B flag should be set in pushed P register");

            // Check PC set to interrupt vector
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x8000), "PC should be set to IRQ vector");

            // Check I flag set
            Assert.That(Cpu.Registers.P & FlagI, Is.EqualTo(FlagI), "Interrupt disable flag should be set");

            // BRK does not halt - execution continues from interrupt vector
            Assert.That(Cpu.Halted, Is.False, "CPU should not be halted after BRK");
        });
    }

    /// <summary>
    /// Test that BRK sets the interrupt disable flag.
    /// </summary>
    [Test]
    public void BRK_SetsInterruptDisableFlag()
    {
        // Arrange
        WriteWord(0xFFFE, 0x9000);
        Write(0x1000, 0x00); // BRK

        Cpu.Reset();

        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.SP.SetByte(0xFF);
        Cpu.Registers.P = (ProcessorStatusFlags)0x00; // I flag clear initially

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P & FlagI, Is.EqualTo(FlagI), "I flag should be set");
    }

    /// <summary>
    /// Integration test for BRK instruction.
    /// </summary>
    [Test]
    public void BRK_IntegrationTest()
    {
        // Arrange
        // Set up interrupt vector
        WriteWord(0xFFFE, 0xA000);

        // Write BRK instruction
        Write(0x2000, 0x00); // BRK

        Cpu.Reset();

        Cpu.Registers.PC.SetWord(0x2000);
        Cpu.Registers.SP.SetByte(0xFD);
        Cpu.Registers.P = (ProcessorStatusFlags)0x20;

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0xA000), "Should jump to interrupt vector");
        Assert.That(Cpu.Halted, Is.False, "Should not be halted - execution continues from interrupt vector");
        Assert.That(Cpu.Registers.SP.GetByte(), Is.EqualTo(0xFA), "Stack pointer should be decremented by 3");
    }

    #endregion

    /// <summary>
    /// Sets up the CPU registers for testing with the specified values.
    /// </summary>
    private void SetupCpu(
        Word pc = 0,
        byte a = 0,
        byte x = 0,
        byte y = 0,
        byte sp = 0,
        ProcessorStatusFlags p = 0,
        ulong cycles = 0,
        bool compat = true)
    {
        Cpu.Registers.Reset(compat);
        Cpu.Registers.PC.SetWord(pc);
        Cpu.Registers.A.SetByte(a);
        Cpu.Registers.X.SetByte(x);
        Cpu.Registers.Y.SetByte(y);
        Cpu.Registers.SP.SetByte(sp);
        Cpu.Registers.P = p;
        Cpu.SetCycles(cycles);
    }
}