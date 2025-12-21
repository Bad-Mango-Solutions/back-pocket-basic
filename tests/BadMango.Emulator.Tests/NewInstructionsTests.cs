// <copyright file="NewInstructionsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Core;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Memory;

/// <summary>
/// Comprehensive unit tests for newly implemented 65C02 instructions.
/// </summary>
[TestFixture]
public class NewInstructionsTests
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

    #region Register Transfer Tests

    /// <summary>
    /// Verifies TAX transfers A to X and sets flags.
    /// </summary>
    [Test]
    public void TAX_TransfersAccumulatorToX()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x42, X = 0x00, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.TAX(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.X, Is.EqualTo(0x42));
        Assert.That(state.P & FlagZ, Is.EqualTo(0), "Zero flag should be clear");
        Assert.That(state.P & FlagN, Is.EqualTo(0), "Negative flag should be clear");
    }

    /// <summary>
    /// Verifies TAY transfers A to Y with zero flag.
    /// </summary>
    [Test]
    public void TAY_TransfersZeroAndSetsZeroFlag()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, Y = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.TAY(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.Y, Is.EqualTo(0x00));
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
    }

    /// <summary>
    /// Verifies TXA transfers X to A with negative flag.
    /// </summary>
    [Test]
    public void TXA_TransfersNegativeValue()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, X = 0x80, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.TXA(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x80));
        Assert.That(state.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set");
    }

    /// <summary>
    /// Verifies TYA transfers Y to A.
    /// </summary>
    [Test]
    public void TYA_TransfersYToAccumulator()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, Y = 0x55, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.TYA(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x55));
    }

    /// <summary>
    /// Verifies TXS transfers X to SP without affecting flags.
    /// </summary>
    [Test]
    public void TXS_TransfersXToStackPointer()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, X = 0xAB, SP = 0xFF, P = FlagZ | FlagN, Cycles = 10 };

        // Act
        var handler = Instructions.TXS(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.SP, Is.EqualTo(0xAB));
        Assert.That(state.P, Is.EqualTo(FlagZ | FlagN), "Flags should not be affected");
    }

    /// <summary>
    /// Verifies TSX transfers SP to X and sets flags.
    /// </summary>
    [Test]
    public void TSX_TransfersStackPointerToX()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, X = 0x00, SP = 0x00, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.TSX(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.X, Is.EqualTo(0x00));
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
    }

    #endregion

    #region Stack Operation Tests

    /// <summary>
    /// Verifies PHA pushes accumulator to stack.
    /// </summary>
    [Test]
    public void PHA_PushesAccumulatorToStack()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x42, SP = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.PHA(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.SP, Is.EqualTo(0xFE));
        Assert.That(memory.Read(0x01FF), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies PLA pulls accumulator from stack.
    /// </summary>
    [Test]
    public void PLA_PullsAccumulatorFromStack()
    {
        // Arrange
        memory.Write(0x01FF, 0x42);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, SP = 0xFE, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.PLA(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.SP, Is.EqualTo(0xFF));
        Assert.That(state.A, Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies PHP pushes processor status with B flag.
    /// </summary>
    [Test]
    public void PHP_PushesProcessorStatusWithBFlag()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, SP = 0xFF, P = FlagC | FlagZ, Cycles = 10 };

        // Act
        var handler = Instructions.PHP(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.SP, Is.EqualTo(0xFE));
        Assert.That(memory.Read(0x01FF), Is.EqualTo(FlagC | FlagZ | 0x10)); // B flag should be set
    }

    /// <summary>
    /// Verifies PLP pulls processor status from stack.
    /// </summary>
    [Test]
    public void PLP_PullsProcessorStatusFromStack()
    {
        // Arrange
        memory.Write(0x01FF, FlagC | FlagN);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x00, SP = 0xFE, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.PLP(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.SP, Is.EqualTo(0xFF));
        Assert.That(state.P, Is.EqualTo(FlagC | FlagN));
    }

    #endregion

    #region Comparison Tests

    /// <summary>
    /// Verifies CMP sets carry when A >= value.
    /// </summary>
    [Test]
    public void CMP_SetsCarryWhenAGreaterOrEqual()
    {
        // Arrange
        memory.Write(0x1000, 0x42);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x42, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.CMP(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagC, Is.EqualTo(FlagC), "Carry should be set");
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero should be set");
    }

    /// <summary>
    /// Verifies CMP clears carry when A less than value.
    /// </summary>
    [Test]
    public void CMP_ClearsCarryWhenALessThan()
    {
        // Arrange
        memory.Write(0x1000, 0x50);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x42, P = FlagC, Cycles = 10 };

        // Act
        var handler = Instructions.CMP(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagC, Is.EqualTo(0), "Carry should be clear");
        Assert.That(state.P & FlagN, Is.EqualTo(FlagN), "Negative should be set");
    }

    /// <summary>
    /// Verifies CPX compares X register.
    /// </summary>
    [Test]
    public void CPX_ComparesXRegister()
    {
        // Arrange
        memory.Write(0x1000, 0x20);
        var state = new Cpu65C02State { PC = 0x1000, X = 0x30, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.CPX(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagC, Is.EqualTo(FlagC), "Carry should be set");
    }

    /// <summary>
    /// Verifies CPY compares Y register.
    /// </summary>
    [Test]
    public void CPY_ComparesYRegister()
    {
        // Arrange
        memory.Write(0x1000, 0x40);
        var state = new Cpu65C02State { PC = 0x1000, Y = 0x40, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.CPY(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagC, Is.EqualTo(FlagC), "Carry should be set");
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero should be set");
    }

    #endregion

    #region Branch Tests

    /// <summary>
    /// Verifies BCC branches when carry is clear.
    /// </summary>
    [Test]
    public void BCC_BranchesWhenCarryClear()
    {
        // Arrange
        memory.Write(0x1000, 0x10); // Offset +16
        var state = new Cpu65C02State { PC = 0x1000, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.BCC(AddressingModes.Relative);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.PC, Is.EqualTo(0x1011)); // 0x1001 + 0x10
    }

    /// <summary>
    /// Verifies BCC does not branch when carry is set.
    /// </summary>
    [Test]
    public void BCC_DoesNotBranchWhenCarrySet()
    {
        // Arrange
        memory.Write(0x1000, 0x10);
        var state = new Cpu65C02State { PC = 0x1000, P = FlagC, Cycles = 10 };
        ushort originalPC = state.PC;

        // Act
        var handler = Instructions.BCC(AddressingModes.Relative);
        handler(cpu, memory, ref state);

        // Assert - PC should only advance by the addressing mode (1 byte for relative)
        Assert.That(state.PC, Is.EqualTo((ushort)(originalPC + 1)));
    }

    /// <summary>
    /// Verifies BEQ branches when zero is set.
    /// </summary>
    [Test]
    public void BEQ_BranchesWhenZeroSet()
    {
        // Arrange
        memory.Write(0x1000, 0x05);
        var state = new Cpu65C02State { PC = 0x1000, P = FlagZ, Cycles = 10 };

        // Act
        var handler = Instructions.BEQ(AddressingModes.Relative);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.PC, Is.EqualTo(0x1006)); // 0x1001 + 0x05
    }

    /// <summary>
    /// Verifies BNE branches when zero is clear.
    /// </summary>
    [Test]
    public void BNE_BranchesWhenZeroClear()
    {
        // Arrange
        memory.Write(0x1000, 0xFE); // -2 in signed byte
        var state = new Cpu65C02State { PC = 0x1000, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.BNE(AddressingModes.Relative);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.PC, Is.EqualTo(0x0FFF)); // 0x1001 + (-2)
    }

    /// <summary>
    /// Verifies BRA always branches (65C02 specific).
    /// </summary>
    [Test]
    public void BRA_AlwaysBranches()
    {
        // Arrange
        memory.Write(0x1000, 0x20); // Offset +32
        var state = new Cpu65C02State { PC = 0x1000, P = 0xFF, Cycles = 10 }; // All flags set

        // Act
        var handler = Instructions.BRA(AddressingModes.Relative);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.PC, Is.EqualTo(0x1021)); // 0x1001 + 0x20
    }

    /// <summary>
    /// Verifies BRA branches backward.
    /// </summary>
    [Test]
    public void BRA_BranchesBackward()
    {
        // Arrange
        memory.Write(0x1000, 0xF0); // -16 in signed byte
        var state = new Cpu65C02State { PC = 0x1000, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.BRA(AddressingModes.Relative);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.PC, Is.EqualTo(0x0FF1)); // 0x1001 + (-16)
    }

    #endregion

    #region Arithmetic Tests

    /// <summary>
    /// Verifies ADC adds with carry in binary mode.
    /// </summary>
    [Test]
    public void ADC_AddsWithCarryBinaryMode()
    {
        // Arrange
        memory.Write(0x1000, 0x42);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x10, P = FlagC, Cycles = 10 };

        // Act
        var handler = Instructions.ADC(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x53)); // 0x10 + 0x42 + 1
    }

    /// <summary>
    /// Verifies ADC sets overflow flag correctly.
    /// </summary>
    [Test]
    public void ADC_SetsOverflowFlag()
    {
        // Arrange
        memory.Write(0x1000, 0x7F);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x01, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.ADC(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x80));
        Assert.That(state.P & FlagV, Is.EqualTo(FlagV), "Overflow should be set");
    }

    /// <summary>
    /// Verifies SBC subtracts with borrow in binary mode.
    /// </summary>
    [Test]
    public void SBC_SubtractsWithBorrowBinaryMode()
    {
        // Arrange
        memory.Write(0x1000, 0x10);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x50, P = FlagC, Cycles = 10 };

        // Act
        var handler = Instructions.SBC(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x40)); // 0x50 - 0x10 - 0
    }

    /// <summary>
    /// Verifies INX increments X register.
    /// </summary>
    [Test]
    public void INX_IncrementsXRegister()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, X = 0x42, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.INX(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.X, Is.EqualTo(0x43));
    }

    /// <summary>
    /// Verifies INY increments Y register and wraps.
    /// </summary>
    [Test]
    public void INY_IncrementsAndWraps()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, Y = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.INY(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.Y, Is.EqualTo(0x00));
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
    }

    /// <summary>
    /// Verifies DEX decrements X register.
    /// </summary>
    [Test]
    public void DEX_DecrementsXRegister()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, X = 0x01, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.DEX(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.X, Is.EqualTo(0x00));
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
    }

    /// <summary>
    /// Verifies INC increments memory.
    /// </summary>
    [Test]
    public void INC_IncrementsMemory()
    {
        // Arrange
        memory.Write(0x50, 0x42);
        memory.Write(0x1000, 0x50);
        var state = new Cpu65C02State { PC = 0x1000, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.INC(AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0x43));
    }

    /// <summary>
    /// Verifies DEC decrements memory.
    /// </summary>
    [Test]
    public void DEC_DecrementsMemory()
    {
        // Arrange
        memory.Write(0x50, 0x01);
        memory.Write(0x1000, 0x50);
        var state = new Cpu65C02State { PC = 0x1000, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.DEC(AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0x00));
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
    }

    #endregion

    #region Logical Operation Tests

    /// <summary>
    /// Verifies AND performs logical AND.
    /// </summary>
    [Test]
    public void AND_PerformsLogicalAND()
    {
        // Arrange
        memory.Write(0x1000, 0x0F);
        var state = new Cpu65C02State { PC = 0x1000, A = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.AND(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x0F));
    }

    /// <summary>
    /// Verifies ORA performs logical OR.
    /// </summary>
    [Test]
    public void ORA_PerformsLogicalOR()
    {
        // Arrange
        memory.Write(0x1000, 0x0F);
        var state = new Cpu65C02State { PC = 0x1000, A = 0xF0, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.ORA(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies EOR performs exclusive OR.
    /// </summary>
    [Test]
    public void EOR_PerformsExclusiveOR()
    {
        // Arrange
        memory.Write(0x1000, 0xFF);
        var state = new Cpu65C02State { PC = 0x1000, A = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.EOR(AddressingModes.Immediate);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x00));
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
    }

    /// <summary>
    /// Verifies BIT tests bits and sets flags.
    /// </summary>
    [Test]
    public void BIT_TestsBitsAndSetsFlags()
    {
        // Arrange
        memory.Write(0x50, 0xC0); // Bits 7 and 6 set
        memory.Write(0x1000, 0x50);
        var state = new Cpu65C02State { PC = 0x1000, A = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.BIT(AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set from bit 7");
        Assert.That(state.P & FlagV, Is.EqualTo(FlagV), "Overflow flag should be set from bit 6");
        Assert.That(state.P & FlagZ, Is.EqualTo(0), "Zero flag should be clear (A & M != 0)");
    }

    #endregion

    #region Shift and Rotate Tests

    /// <summary>
    /// Verifies ASL shifts accumulator left.
    /// </summary>
    [Test]
    public void ASL_ShiftsAccumulatorLeft()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x42, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.ASLa(AddressingModes.Accumulator);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x84));
        Assert.That(state.P & FlagC, Is.EqualTo(0), "Carry should be clear");
    }

    /// <summary>
    /// Verifies ASL sets carry from bit 7.
    /// </summary>
    [Test]
    public void ASL_SetsCarryFromBit7()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x80, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.ASLa(AddressingModes.Accumulator);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x00));
        Assert.That(state.P & FlagC, Is.EqualTo(FlagC), "Carry should be set");
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero should be set");
    }

    /// <summary>
    /// Verifies LSR shifts accumulator right.
    /// </summary>
    [Test]
    public void LSR_ShiftsAccumulatorRight()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x42, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.LSRa(AddressingModes.Accumulator);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x21));
        Assert.That(state.P & FlagC, Is.EqualTo(0), "Carry should be clear");
    }

    /// <summary>
    /// Verifies ROL rotates left through carry.
    /// </summary>
    [Test]
    public void ROL_RotatesLeftThroughCarry()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x42, P = FlagC, Cycles = 10 };

        // Act
        var handler = Instructions.ROLa(AddressingModes.Accumulator);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0x85)); // 0x42 << 1 | 1
        Assert.That(state.P & FlagC, Is.EqualTo(0), "Carry should be clear");
    }

    /// <summary>
    /// Verifies ROR rotates right through carry.
    /// </summary>
    [Test]
    public void ROR_RotatesRightThroughCarry()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, A = 0x42, P = FlagC, Cycles = 10 };

        // Act
        var handler = Instructions.RORa(AddressingModes.Accumulator);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.A, Is.EqualTo(0xA1)); // 0x80 | (0x42 >> 1)
        Assert.That(state.P & FlagC, Is.EqualTo(0), "Carry should be clear");
    }

    #endregion

    #region Jump and Subroutine Tests

    /// <summary>
    /// Verifies JMP jumps to absolute address.
    /// </summary>
    [Test]
    public void JMP_JumpsToAbsoluteAddress()
    {
        // Arrange
        memory.WriteWord(0x1000, 0x2000);
        var state = new Cpu65C02State { PC = 0x1000, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.JMP(AddressingModes.Absolute);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.PC, Is.EqualTo(0x2000));
    }

    /// <summary>
    /// Verifies JSR pushes return address and jumps.
    /// </summary>
    [Test]
    public void JSR_PushesReturnAddressAndJumps()
    {
        // Arrange
        memory.WriteWord(0x1000, 0x2000);
        var state = new Cpu65C02State { PC = 0x1000, SP = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.JSR(AddressingModes.Absolute);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.PC, Is.EqualTo(0x2000));
        Assert.That(state.SP, Is.EqualTo(0xFD));
        ushort returnAddr = (ushort)((memory.Read(0x01FF) << 8) | memory.Read(0x01FE));
        Assert.That(returnAddr, Is.EqualTo(0x1001)); // PC - 1 after reading operand
    }

    /// <summary>
    /// Verifies RTS pulls return address and returns.
    /// </summary>
    [Test]
    public void RTS_PullsReturnAddressAndReturns()
    {
        // Arrange
        memory.Write(0x01FE, 0x00);
        memory.Write(0x01FF, 0x20);
        var state = new Cpu65C02State { PC = 0x1000, SP = 0xFD, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.RTS(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.PC, Is.EqualTo(0x2001)); // Return address + 1
        Assert.That(state.SP, Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies RTI pulls status and return address.
    /// </summary>
    [Test]
    public void RTI_PullsStatusAndReturnAddress()
    {
        // Arrange
        memory.Write(0x01FE, FlagC | FlagZ);
        memory.Write(0x01FF, 0x00);
        memory.Write(0x0100, 0x20);
        var state = new Cpu65C02State { PC = 0x1000, SP = 0xFD, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.RTI(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.PC, Is.EqualTo(0x2000));
        Assert.That(state.SP, Is.EqualTo(0x00));
        Assert.That(state.P, Is.EqualTo(FlagC | FlagZ));
    }

    #endregion

    #region Store Tests

    /// <summary>
    /// Verifies STX stores X register to memory.
    /// </summary>
    [Test]
    public void STX_StoresXRegisterToMemory()
    {
        // Arrange
        memory.Write(0x1000, 0x50);
        var state = new Cpu65C02State { PC = 0x1000, X = 0x42, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.STX(AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies STY stores Y register to memory.
    /// </summary>
    [Test]
    public void STY_StoresYRegisterToMemory()
    {
        // Arrange
        memory.Write(0x1000, 0x50);
        var state = new Cpu65C02State { PC = 0x1000, Y = 0x55, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.STY(AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0x55));
    }

    #endregion

    #region 65C02-Specific Instruction Tests

    /// <summary>
    /// Verifies STZ stores zero to memory.
    /// </summary>
    [Test]
    public void STZ_StoresZeroToMemory()
    {
        // Arrange
        memory.Write(0x50, 0xFF);
        memory.Write(0x1000, 0x50);
        var state = new Cpu65C02State { PC = 0x1000, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.STZ(AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies PHX pushes X register to stack.
    /// </summary>
    [Test]
    public void PHX_PushesXRegisterToStack()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, X = 0x42, SP = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.PHX(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.SP, Is.EqualTo(0xFE));
        Assert.That(memory.Read(0x01FF), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies PLX pulls X register from stack.
    /// </summary>
    [Test]
    public void PLX_PullsXRegisterFromStack()
    {
        // Arrange
        memory.Write(0x01FF, 0x42);
        var state = new Cpu65C02State { PC = 0x1000, X = 0x00, SP = 0xFE, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.PLX(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.SP, Is.EqualTo(0xFF));
        Assert.That(state.X, Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies PHY pushes Y register to stack.
    /// </summary>
    [Test]
    public void PHY_PushesYRegisterToStack()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, Y = 0x55, SP = 0xFF, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.PHY(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.SP, Is.EqualTo(0xFE));
        Assert.That(memory.Read(0x01FF), Is.EqualTo(0x55));
    }

    /// <summary>
    /// Verifies PLY pulls Y register from stack.
    /// </summary>
    [Test]
    public void PLY_PullsYRegisterFromStack()
    {
        // Arrange
        memory.Write(0x01FF, 0x55);
        var state = new Cpu65C02State { PC = 0x1000, Y = 0x00, SP = 0xFE, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.PLY(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.SP, Is.EqualTo(0xFF));
        Assert.That(state.Y, Is.EqualTo(0x55));
    }

    /// <summary>
    /// Verifies TSB tests and sets bits.
    /// </summary>
    [Test]
    public void TSB_TestsAndSetsBits()
    {
        // Arrange
        memory.Write(0x50, 0x0F);
        memory.Write(0x1000, 0x50);
        var state = new Cpu65C02State { PC = 0x1000, A = 0xF0, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.TSB(AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0xFF)); // 0x0F OR 0xF0
        Assert.That(state.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set (A AND M was 0)");
    }

    /// <summary>
    /// Verifies TSB clears zero flag when bits match.
    /// </summary>
    [Test]
    public void TSB_ClearsZeroFlagWhenBitsMatch()
    {
        // Arrange
        memory.Write(0x50, 0xFF);
        memory.Write(0x1000, 0x50);
        var state = new Cpu65C02State { PC = 0x1000, A = 0x80, P = FlagZ, Cycles = 10 };

        // Act
        var handler = Instructions.TSB(AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0xFF)); // 0xFF OR 0x80 = 0xFF
        Assert.That(state.P & FlagZ, Is.EqualTo(0), "Zero flag should be clear (A AND M != 0)");
    }

    /// <summary>
    /// Verifies TRB tests and resets bits.
    /// </summary>
    [Test]
    public void TRB_TestsAndResetsBits()
    {
        // Arrange
        memory.Write(0x50, 0xFF);
        memory.Write(0x1000, 0x50);
        var state = new Cpu65C02State { PC = 0x1000, A = 0xF0, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.TRB(AddressingModes.ZeroPage);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0x0F)); // 0xFF AND (NOT 0xF0)
        Assert.That(state.P & FlagZ, Is.EqualTo(0), "Zero flag should be clear (A AND M != 0)");
    }

    /// <summary>
    /// Verifies WAI halts the processor.
    /// </summary>
    [Test]
    public void WAI_HaltsProcessor()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, Halted = false, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.WAI(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.Halted, Is.True, "Processor should be halted");
    }

    /// <summary>
    /// Verifies STP halts the processor.
    /// </summary>
    [Test]
    public void STP_HaltsProcessor()
    {
        // Arrange
        var state = new Cpu65C02State { PC = 0x1000, Halted = false, P = 0x00, Cycles = 10 };

        // Act
        var handler = Instructions.STP(AddressingModes.Implied);
        handler(cpu, memory, ref state);

        // Assert
        Assert.That(state.Halted, Is.True, "Processor should be halted");
    }

    #endregion
}