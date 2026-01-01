// <copyright file="InstructionsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;
using Core.Interfaces;

using Emulation.Cpu;
using Emulation.Memory;

/// <summary>
/// Comprehensive unit tests for instruction implementations.
/// </summary>
[TestFixture]
public class InstructionsTests
{
    private const ProcessorStatusFlags FlagC = ProcessorStatusFlags.C;
    private const ProcessorStatusFlags FlagZ = ProcessorStatusFlags.Z;
    private const ProcessorStatusFlags FlagI = ProcessorStatusFlags.I;
    private const ProcessorStatusFlags FlagD = ProcessorStatusFlags.D;
    private const ProcessorStatusFlags FlagV = ProcessorStatusFlags.V;
    private const ProcessorStatusFlags FlagN = ProcessorStatusFlags.N;

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
        cpu.Reset();
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
        SetupCpu(pc: 0x1000, a: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDA(AddressingModes.Immediate);
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.A.GetByte(), Is.EqualTo(0x00));
        Assert.That(cpu.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
        Assert.That(cpu.Registers.P & FlagN, Is.EqualTo((ProcessorStatusFlags)0), "Negative flag should be clear");
    }

    /// <summary>
    /// Verifies that LDA loads value and sets negative flag.
    /// </summary>
    [Test]
    public void LDA_LoadsNegativeValueAndSetsNegativeFlag()
    {
        // Arrange
        memory.Write(0x1000, 0x80);
        SetupCpu(pc: 0x1000, a: 0x00, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDA(AddressingModes.Immediate);
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.A.GetByte(), Is.EqualTo(0x80));
        Assert.That(cpu.Registers.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set");
        Assert.That(cpu.Registers.P & FlagZ, Is.EqualTo((ProcessorStatusFlags)0), "Zero flag should be clear");
    }

    /// <summary>
    /// Verifies that LDA loads positive value and clears both flags.
    /// </summary>
    [Test]
    public void LDA_LoadsPositiveValueAndClearsBothFlags()
    {
        // Arrange
        memory.Write(0x1000, 0x42);
        SetupCpu(pc: 0x1000, a: 0x00, p: FlagZ | FlagN, cycles: 10);

        // Act
        var handler = Instructions.LDA(AddressingModes.Immediate);
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.A.GetByte(), Is.EqualTo(0x42));
        Assert.That(cpu.Registers.P & FlagZ, Is.EqualTo((ProcessorStatusFlags)0), "Zero flag should be clear");
        Assert.That(cpu.Registers.P & FlagN, Is.EqualTo((ProcessorStatusFlags)0), "Negative flag should be clear");
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
        SetupCpu(pc: 0x1000, x: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDX(AddressingModes.Immediate);
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.X.GetByte(), Is.EqualTo(0x00));
        Assert.That(cpu.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
        Assert.That(cpu.Registers.P & FlagN, Is.EqualTo((ProcessorStatusFlags)0), "Negative flag should be clear");
    }

    /// <summary>
    /// Verifies that LDX loads value and sets negative flag.
    /// </summary>
    [Test]
    public void LDX_LoadsNegativeValueAndSetsNegativeFlag()
    {
        // Arrange
        memory.Write(0x1000, 0x90);
        SetupCpu(pc: 0x1000, x: 0x00, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDX(AddressingModes.Immediate);
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.X.GetByte(), Is.EqualTo(0x90));
        Assert.That(cpu.Registers.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set");
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
        SetupCpu(pc: 0x1000, y: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDY(AddressingModes.Immediate);
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.Y.GetByte(), Is.EqualTo(0x00));
        Assert.That(cpu.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
    }

    /// <summary>
    /// Verifies that LDY loads value and sets negative flag.
    /// </summary>
    [Test]
    public void LDY_LoadsNegativeValueAndSetsNegativeFlag()
    {
        // Arrange
        memory.Write(0x1000, 0xA0);
        SetupCpu(pc: 0x1000, y: 0x00, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LDY(AddressingModes.Immediate);
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.Y.GetByte(), Is.EqualTo(0xA0));
        Assert.That(cpu.Registers.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set");
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
        memory.Write(0x1000, 0x50); // ZP address

        // Act
        var handler = Instructions.STA(AddressingModes.ZeroPage);
        handler(cpu);

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
        SetupCpu(pc: 0x1000, a: 0x00, p: FlagZ | FlagN, cycles: 10);
        memory.Write(0x1000, 0x60); // ZP address

        // Act
        var handler = Instructions.STA(AddressingModes.ZeroPage);
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.P, Is.EqualTo(FlagZ | FlagN), "Flags should not be modified");
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
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0x1000), "PC should not change");
        Assert.That(cpu.Registers.A.GetByte(), Is.EqualTo(0x42), "RegisterAccumulator should not change");
        Assert.That(cpu.Registers.X.GetByte(), Is.EqualTo(0x12), "X should not change");
        Assert.That(cpu.Registers.Y.GetByte(), Is.EqualTo(0x34), "Y should not change");
        Assert.That(cpu.Registers.SP.GetByte(), Is.EqualTo(0xFD), "SP should not change");
        Assert.That(cpu.Registers.P, Is.EqualTo(FlagZ | FlagN), "Flags should not change");
        Assert.That(cpu.GetCycles(), Is.EqualTo(11), "Should consume 1 cycle");
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
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.P & FlagC, Is.EqualTo((ProcessorStatusFlags)0), "Carry flag should be clear");
        Assert.That(cpu.Registers.P & ~FlagC, Is.EqualTo((ProcessorStatusFlags)0xFF & ~FlagC), "Other flags should be unchanged");
        Assert.That(cpu.GetCycles(), Is.EqualTo(11));
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
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.P & FlagC, Is.EqualTo(FlagC), "Carry flag should be set");
        Assert.That(cpu.GetCycles(), Is.EqualTo(11));
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
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.P & FlagI, Is.EqualTo((ProcessorStatusFlags)0), "Interrupt disable flag should be clear");
        Assert.That(cpu.Registers.P & ~FlagI, Is.EqualTo((ProcessorStatusFlags)0xFF & ~FlagI), "Other flags should be unchanged");
        Assert.That(cpu.GetCycles(), Is.EqualTo(11));
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
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.P & FlagI, Is.EqualTo(FlagI), "Interrupt disable flag should be set");
        Assert.That(cpu.GetCycles(), Is.EqualTo(11));
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
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.P & FlagD, Is.EqualTo((ProcessorStatusFlags)0), "Decimal mode flag should be clear");
        Assert.That(cpu.Registers.P & ~FlagD, Is.EqualTo((ProcessorStatusFlags)0xFF & ~FlagD), "Other flags should be unchanged");
        Assert.That(cpu.GetCycles(), Is.EqualTo(11));
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
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.P & FlagD, Is.EqualTo(FlagD), "Decimal mode flag should be set");
        Assert.That(cpu.GetCycles(), Is.EqualTo(11));
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
        handler(cpu);

        // Assert
        Assert.That(cpu.Registers.P & FlagV, Is.EqualTo((ProcessorStatusFlags)0), "Overflow flag should be clear");
        Assert.That(cpu.Registers.P & ~FlagV, Is.EqualTo((ProcessorStatusFlags)0xFF & ~FlagV), "Other flags should be unchanged");
        Assert.That(cpu.GetCycles(), Is.EqualTo(11));
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
        cpu.Registers.P = (ProcessorStatusFlags)0xFF; // Set all flags

        // Act
        cpu.Step();

        // Assert
        Assert.That(cpu.Registers.P & FlagC, Is.EqualTo((ProcessorStatusFlags)0), "Carry flag should be clear");
        Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
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
        Assert.That(cpu.Registers.P & FlagC, Is.EqualTo(FlagC), "Carry flag should be set");
        Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
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
        
        cpu.Registers.P = (ProcessorStatusFlags)0xFF; // Set all flags
        

        // Act
        cpu.Step();

        // Assert
        
        Assert.That(cpu.Registers.P & FlagI, Is.EqualTo((ProcessorStatusFlags)0), "Interrupt disable flag should be clear");
        Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
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
        
        Assert.That(cpu.Registers.P & FlagI, Is.EqualTo(FlagI), "Interrupt disable flag should be set");
        Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
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
        
        cpu.Registers.P = (ProcessorStatusFlags)0xFF; // Set all flags
        

        // Act
        cpu.Step();

        // Assert
        
        Assert.That(cpu.Registers.P & FlagD, Is.EqualTo((ProcessorStatusFlags)0), "Decimal mode flag should be clear");
        Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
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
        
        Assert.That(cpu.Registers.P & FlagD, Is.EqualTo(FlagD), "Decimal mode flag should be set");
        Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
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
        
        cpu.Registers.P = (ProcessorStatusFlags)0xFF; // Set all flags
        

        // Act
        cpu.Step();

        // Assert
        
        Assert.That(cpu.Registers.P & FlagV, Is.EqualTo((ProcessorStatusFlags)0), "Overflow flag should be clear");
        Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
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
        memory.WriteWord(0xFFFE, 0x8000);

        // Write BRK instruction
        memory.Write(0x1000, 0x00); // BRK

        // Set initial state
        cpu.Reset();
        
        cpu.Registers.PC.SetWord(0x1000);
        cpu.Registers.SP.SetByte(0xFD); // Stack pointer starts at 0xFD
        cpu.Registers.P = (ProcessorStatusFlags)0x24; // Some flags set
        

        // Act
        cpu.Step();

        // Assert
        

        // Check stack pointer decremented by 3
        Assert.That(cpu.Registers.SP.GetByte(), Is.EqualTo(0xFA), "SP should be decremented by 3");

        // Check pushed values on stack
        // BRK pushes PC+1 (0x1002 since BRK increments PC), then P with B flag set
        const byte FlagB = 0x10; // Break flag
        Assert.Multiple(() =>
        {
            Assert.That(memory.Read(0x01FD), Is.EqualTo(0x10), "HighByte byte of return address should be on stack");
            Assert.That(memory.Read(0x01FC), Is.EqualTo(0x02), "LowByte byte of return address should be on stack");
            Assert.That(memory.Read(0x01FB) & FlagB, Is.EqualTo(FlagB), "B flag should be set in pushed P register");

            // Check PC set to interrupt vector
            Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0x8000), "PC should be set to IRQ vector");

            // Check I flag set
            Assert.That(cpu.Registers.P & FlagI, Is.EqualTo(FlagI), "Interrupt disable flag should be set");

            // BRK does not halt - execution continues from interrupt vector
            Assert.That(cpu.Halted, Is.False, "CPU should not be halted after BRK");
        });
    }

    /// <summary>
    /// Test that BRK sets the interrupt disable flag.
    /// </summary>
    [Test]
    public void BRK_SetsInterruptDisableFlag()
    {
        // Arrange
        memory.WriteWord(0xFFFE, 0x9000);
        memory.Write(0x1000, 0x00); // BRK

        cpu.Reset();
        
        cpu.Registers.PC.SetWord(0x1000);
        cpu.Registers.SP.SetByte(0xFF);
        cpu.Registers.P = (ProcessorStatusFlags)0x00; // I flag clear initially
        

        // Act
        cpu.Step();

        // Assert
        
        Assert.That(cpu.Registers.P & FlagI, Is.EqualTo(FlagI), "I flag should be set");
    }

    /// <summary>
    /// Integration test for BRK instruction.
    /// </summary>
    [Test]
    public void BRK_IntegrationTest()
    {
        // Arrange
        // Set up interrupt vector
        memory.WriteWord(0xFFFE, 0xA000);

        // Write BRK instruction
        memory.Write(0x2000, 0x00); // BRK

        cpu.Reset();
        
        cpu.Registers.PC.SetWord(0x2000);
        cpu.Registers.SP.SetByte(0xFD);
        cpu.Registers.P = (ProcessorStatusFlags)0x20;
        

        // Act
        cpu.Step();

        // Assert
        
        Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0xA000), "Should jump to interrupt vector");
        Assert.That(cpu.Halted, Is.False, "Should not be halted - execution continues from interrupt vector");
        Assert.That(cpu.Registers.SP.GetByte(), Is.EqualTo(0xFA), "Stack pointer should be decremented by 3");
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
        cpu.Registers.Reset(compat);
        cpu.Registers.PC.SetWord(pc);
        cpu.Registers.A.SetByte(a);
        cpu.Registers.X.SetByte(x);
        cpu.Registers.Y.SetByte(y);
        cpu.Registers.SP.SetByte(sp);
        cpu.Registers.P = p;
        cpu.SetCycles(cycles);
    }
}