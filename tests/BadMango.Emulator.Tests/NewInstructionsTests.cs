// <copyright file="NewInstructionsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core;
using Core.Cpu;

using Emulation.Cpu;
using Emulation.Memory;

/// <summary>
/// Comprehensive unit tests for newly implemented 65C02 instructions.
/// </summary>
[TestFixture]
public class NewInstructionsTests
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
    }

    #region Register Transfer Tests

    /// <summary>
    /// Verifies TAX transfers accumulator to X and sets flags.
    /// </summary>
    [Test]
    public void TAX_TransfersAccumulatorToX()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, a: 0x42, x: 0x00, p: 0, cycles: 10);

        // Act
        var handler = Instructions.TAX(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.X.GetByte(), Is.EqualTo(0x42));
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo((ProcessorStatusFlags)0), "Zero flag should be clear");
        Assert.That(state.Registers.P & FlagN, Is.EqualTo((ProcessorStatusFlags)0), "Negative flag should be clear");
    }

    /// <summary>
    /// Verifies TAY transfers RegisterAccumulator to Y with zero flag.
    /// </summary>
    [Test]
    public void TAY_TransfersZeroAndSetsZeroFlag()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, a: 0x00, y: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.TAY(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.Y.GetByte(), Is.EqualTo(0x00));
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
    }

    /// <summary>
    /// Verifies TXA transfers X to RegisterAccumulator with negative flag.
    /// </summary>
    [Test]
    public void TXA_TransfersNegativeValue()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, a: 0x00, x: 0x80, p: 0, cycles: 10, compat: true);

        // Act
        var handler = Instructions.TXA(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x80));
        Assert.That(state.Registers.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set");
    }

    /// <summary>
    /// Verifies TYA transfers Y to RegisterAccumulator.
    /// </summary>
    [Test]
    public void TYA_TransfersYToAccumulator()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, a: 0x00, y: 0x55, p: 0, cycles: 10);

        // Act
        var handler = Instructions.TYA(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x55));
    }

    /// <summary>
    /// Verifies TXS transfers X to SP without affecting flags.
    /// </summary>
    [Test]
    public void TXS_TransfersXToStackPointer()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, x: 0xAB, sp: 0xFF, p: FlagZ | FlagN, cycles: 10);

        // Act
        var handler = Instructions.TXS(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xAB));
        Assert.That(state.Registers.P, Is.EqualTo(FlagZ | FlagN), "Flags should not be affected");
    }

    /// <summary>
    /// Verifies TSX transfers SP to X and sets flags.
    /// </summary>
    [Test]
    public void TSX_TransfersStackPointerToX()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, x: 0x00, sp: 0x00, p: 0, cycles: 10);

        // Act
        var handler = Instructions.TSX(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.X.GetByte(), Is.EqualTo(0x00));
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
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
        var state = CreateState(pc: 0x1000, a: 0x42, sp: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.PHA(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xFE));
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
        var state = CreateState(pc: 0x1000, a: 0x00, sp: 0xFE, p: 0, cycles: 10);

        // Act
        var handler = Instructions.PLA(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xFF));
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies PHP pushes processor status with B flag.
    /// </summary>
    [Test]
    public void PHP_PushesProcessorStatusWithBFlag()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, a: 0x00, sp: 0xFF, p: FlagC | FlagZ, cycles: 10);

        // Act
        var handler = Instructions.PHP(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xFE));
        Assert.That(memory.Read(0x01FF), Is.EqualTo((byte)(FlagC | FlagZ | ProcessorStatusFlags.B))); // B flag should be set
    }

    /// <summary>
    /// Verifies PLP pulls processor status from stack.
    /// </summary>
    [Test]
    public void PLP_PullsProcessorStatusFromStack()
    {
        // Arrange
        memory.Write(0x01FF, (byte)(FlagC | FlagN));
        var state = CreateState(pc: 0x1000, a: 0x00, sp: 0xFE, p: 0, cycles: 10);

        // Act
        var handler = Instructions.PLP(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xFF));
        Assert.That(state.Registers.P, Is.EqualTo(FlagC | FlagN));
    }

    #endregion

    #region Comparison Tests

    /// <summary>
    /// Verifies CMP sets carry when RegisterAccumulator >= value.
    /// </summary>
    [Test]
    public void CMP_SetsCarryWhenAGreaterOrEqual()
    {
        // Arrange
        memory.Write(0x1000, 0x42);
        var state = CreateState(pc: 0x1000, a: 0x42, p: 0, cycles: 10);

        // Act
        var handler = Instructions.CMP(AddressingModes.Immediate);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.P & FlagC, Is.EqualTo(FlagC), "Carry should be set");
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero should be set");
    }

    /// <summary>
    /// Verifies CMP clears carry when RegisterAccumulator less than value.
    /// </summary>
    [Test]
    public void CMP_ClearsCarryWhenALessThan()
    {
        // Arrange
        memory.Write(0x1000, 0x50);
        var state = CreateState(pc: 0x1000, a: 0x42, p: FlagC, cycles: 10);

        // Act
        var handler = Instructions.CMP(AddressingModes.Immediate);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.P & FlagC, Is.EqualTo((ProcessorStatusFlags)0), "Carry should be clear");
        Assert.That(state.Registers.P & FlagN, Is.EqualTo(FlagN), "Negative should be set");
    }

    /// <summary>
    /// Verifies CPX compares X register.
    /// </summary>
    [Test]
    public void CPX_ComparesXRegister()
    {
        // Arrange
        memory.Write(0x1000, 0x20);
        var state = CreateState(pc: 0x1000, x: 0x30, p: 0, cycles: 10);

        // Act
        var handler = Instructions.CPX(AddressingModes.Immediate);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.P & FlagC, Is.EqualTo(FlagC), "Carry should be set");
    }

    /// <summary>
    /// Verifies CPY compares Y register.
    /// </summary>
    [Test]
    public void CPY_ComparesYRegister()
    {
        // Arrange
        memory.Write(0x1000, 0x40);
        var state = CreateState(pc: 0x1000, y: 0x40, p: 0, cycles: 10);

        // Act
        var handler = Instructions.CPY(AddressingModes.Immediate);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.P & FlagC, Is.EqualTo(FlagC), "Carry should be set");
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero should be set");
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
        var state = CreateState(pc: 0x1000, p: 0, cycles: 10);

        // Act
        var handler = Instructions.BCC(AddressingModes.Relative);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x1011)); // 0x1001 + 0x10
    }

    /// <summary>
    /// Verifies BCC does not branch when carry is set.
    /// </summary>
    [Test]
    public void BCC_DoesNotBranchWhenCarrySet()
    {
        // Arrange
        memory.Write(0x1000, 0x10);
        var state = CreateState(pc: 0x1000, p: FlagC, cycles: 10);
        ushort originalPC = state.Registers.PC.GetWord();

        // Act
        var handler = Instructions.BCC(AddressingModes.Relative);
        handler(memory, ref state);

        // Assert - PC should only advance by the addressing mode (1 byte for relative)
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo((ushort)(originalPC + 1)));
    }

    /// <summary>
    /// Verifies BEQ branches when zero is set.
    /// </summary>
    [Test]
    public void BEQ_BranchesWhenZeroSet()
    {
        // Arrange
        memory.Write(0x1000, 0x05);
        var state = CreateState(pc: 0x1000, p: FlagZ, cycles: 10);

        // Act
        var handler = Instructions.BEQ(AddressingModes.Relative);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x1006)); // 0x1001 + 0x05
    }

    /// <summary>
    /// Verifies BNE branches when zero is clear.
    /// </summary>
    [Test]
    public void BNE_BranchesWhenZeroClear()
    {
        // Arrange
        memory.Write(0x1000, 0xFE); // -2 in signed byte
        var state = CreateState(pc: 0x1000, p: 0, cycles: 10);

        // Act
        var handler = Instructions.BNE(AddressingModes.Relative);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x0FFF)); // 0x1001 + (-2)
    }

    /// <summary>
    /// Verifies BRA always branches (65C02 specific).
    /// </summary>
    [Test]
    public void BRA_AlwaysBranches()
    {
        // Arrange
        memory.Write(0x1000, 0x20); // Offset +32
        var state = CreateState(pc: 0x1000, p: (ProcessorStatusFlags)0xFF, cycles: 10); // All flags set

        // Act
        var handler = Instructions.BRA(AddressingModes.Relative);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x1021)); // 0x1001 + 0x20
    }

    /// <summary>
    /// Verifies BRA branches backward.
    /// </summary>
    [Test]
    public void BRA_BranchesBackward()
    {
        // Arrange
        memory.Write(0x1000, 0xF0); // -16 in signed byte
        var state = CreateState(pc: 0x1000, p: 0, cycles: 10);

        // Act
        var handler = Instructions.BRA(AddressingModes.Relative);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x0FF1)); // 0x1001 + (-16)
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
        var state = CreateState(pc: 0x1000, a: 0x10, p: FlagC, cycles: 10);

        // Act
        var handler = Instructions.ADC(AddressingModes.Immediate);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x53)); // 0x10 + 0x42 + 1
    }

    /// <summary>
    /// Verifies ADC sets overflow flag correctly.
    /// </summary>
    [Test]
    public void ADC_SetsOverflowFlag()
    {
        // Arrange
        memory.Write(0x1000, 0x7F);
        var state = CreateState(pc: 0x1000, a: 0x01, p: 0, cycles: 10);

        // Act
        var handler = Instructions.ADC(AddressingModes.Immediate);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x80));
        Assert.That(state.Registers.P & FlagV, Is.EqualTo(FlagV), "Overflow should be set");
    }

    /// <summary>
    /// Verifies SBC subtracts with borrow in binary mode.
    /// </summary>
    [Test]
    public void SBC_SubtractsWithBorrowBinaryMode()
    {
        // Arrange
        memory.Write(0x1000, 0x10);
        var state = CreateState(pc: 0x1000, a: 0x50, p: FlagC, cycles: 10);

        // Act
        var handler = Instructions.SBC(AddressingModes.Immediate);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x40)); // 0x50 - 0x10 - 0
    }

    /// <summary>
    /// Verifies INX increments X register.
    /// </summary>
    [Test]
    public void INX_IncrementsXRegister()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, x: 0x42, p: 0, cycles: 10);

        // Act
        var handler = Instructions.INX(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.X.GetByte(), Is.EqualTo(0x43));
    }

    /// <summary>
    /// Verifies INY increments Y register and wraps.
    /// </summary>
    [Test]
    public void INY_IncrementsAndWraps()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, y: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.INY(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.Y.GetByte(), Is.EqualTo(0x00));
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
    }

    /// <summary>
    /// Verifies DEX decrements X register.
    /// </summary>
    [Test]
    public void DEX_DecrementsXRegister()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, x: 0x01, p: 0, cycles: 10);

        // Act
        var handler = Instructions.DEX(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.X.GetByte(), Is.EqualTo(0x00));
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
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
        var state = CreateState(pc: 0x1000, p: 0, cycles: 10);

        // Act
        var handler = Instructions.INC(AddressingModes.ZeroPage);
        handler(memory, ref state);

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
        var state = CreateState(pc: 0x1000, p: 0, cycles: 10);

        // Act
        var handler = Instructions.DEC(AddressingModes.ZeroPage);
        handler(memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0x00));
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
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
        var state = CreateState(pc: 0x1000, a: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.AND(AddressingModes.Immediate);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x0F));
    }

    /// <summary>
    /// Verifies ORA performs logical OR.
    /// </summary>
    [Test]
    public void ORA_PerformsLogicalOR()
    {
        // Arrange
        memory.Write(0x1000, 0x0F);
        var state = CreateState(pc: 0x1000, a: 0xF0, p: 0, cycles: 10);

        // Act
        var handler = Instructions.ORA(AddressingModes.Immediate);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies EOR performs exclusive OR.
    /// </summary>
    [Test]
    public void EOR_PerformsExclusiveOR()
    {
        // Arrange
        memory.Write(0x1000, 0xFF);
        var state = CreateState(pc: 0x1000, a: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.EOR(AddressingModes.Immediate);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x00));
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set");
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
        var state = CreateState(pc: 0x1000, a: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.BIT(AddressingModes.ZeroPage);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.P & FlagN, Is.EqualTo(FlagN), "Negative flag should be set from bit 7");
        Assert.That(state.Registers.P & FlagV, Is.EqualTo(FlagV), "Overflow flag should be set from bit 6");
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo((ProcessorStatusFlags)0), "Zero flag should be clear (RegisterAccumulator & M != 0)");
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
        var state = CreateState(pc: 0x1000, a: 0x42, p: 0, cycles: 10);

        // Act
        var handler = Instructions.ASLa(AddressingModes.Accumulator);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x84));
        Assert.That(state.Registers.P & FlagC, Is.EqualTo((ProcessorStatusFlags)0), "Carry should be clear");
    }

    /// <summary>
    /// Verifies ASL sets carry from bit 7.
    /// </summary>
    [Test]
    public void ASL_SetsCarryFromBit7()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, a: 0x80, p: 0, cycles: 10);

        // Act
        var handler = Instructions.ASLa(AddressingModes.Accumulator);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x00));
        Assert.That(state.Registers.P & FlagC, Is.EqualTo(FlagC), "Carry should be set");
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero should be set");
    }

    /// <summary>
    /// Verifies LSR shifts accumulator right.
    /// </summary>
    [Test]
    public void LSR_ShiftsAccumulatorRight()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, a: 0x42, p: 0, cycles: 10);

        // Act
        var handler = Instructions.LSRa(AddressingModes.Accumulator);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x21));
        Assert.That(state.Registers.P & FlagC, Is.EqualTo((ProcessorStatusFlags)0), "Carry should be clear");
    }

    /// <summary>
    /// Verifies ROL rotates left through carry.
    /// </summary>
    [Test]
    public void ROL_RotatesLeftThroughCarry()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, a: 0x42, p: FlagC, cycles: 10);

        // Act
        var handler = Instructions.ROLa(AddressingModes.Accumulator);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0x85)); // 0x42 << 1 | 1
        Assert.That(state.Registers.P & FlagC, Is.EqualTo((ProcessorStatusFlags)0), "Carry should be clear");
    }

    /// <summary>
    /// Verifies ROR rotates right through carry.
    /// </summary>
    [Test]
    public void ROR_RotatesRightThroughCarry()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, a: 0x42, p: FlagC, cycles: 10);

        // Act
        var handler = Instructions.RORa(AddressingModes.Accumulator);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.A.GetByte(), Is.EqualTo(0xA1)); // 0x80 | (0x42 >> 1)
        Assert.That(state.Registers.P & FlagC, Is.EqualTo((ProcessorStatusFlags)0), "Carry should be clear");
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
        var state = CreateState(pc: 0x1000, p: 0, cycles: 10);

        // Act
        var handler = Instructions.JMP(AddressingModes.Absolute);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x2000));
    }

    /// <summary>
    /// Verifies JSR pushes return address and jumps.
    /// </summary>
    [Test]
    public void JSR_PushesReturnAddressAndJumps()
    {
        // Arrange
        memory.WriteWord(0x1000, 0x2000);
        var state = CreateState(pc: 0x1000, sp: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.JSR(AddressingModes.Absolute);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x2000));
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xFD));
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
        var state = CreateState(pc: 0x1000, sp: 0xFD, p: 0, cycles: 10);

        // Act
        var handler = Instructions.RTS(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x2001)); // Return address + 1
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies RTI pulls status and return address.
    /// </summary>
    [Test]
    public void RTI_PullsStatusAndReturnAddress()
    {
        // Arrange
        memory.Write(0x01FD, (byte)(FlagC | FlagZ));
        memory.Write(0x01FE, 0x00);
        memory.Write(0x01FF, 0x20);
        var state = CreateState(pc: 0x1000, sp: 0xFC, p: 0, cycles: 10);

        // Act
        var handler = Instructions.RTI(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x2000));
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xFF));
        Assert.That(state.Registers.P, Is.EqualTo(FlagC | FlagZ));
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
        var state = CreateState(pc: 0x1000, x: 0x42, p: 0, cycles: 10);

        // Act
        var handler = Instructions.STX(AddressingModes.ZeroPage);
        handler(memory, ref state);

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
        var state = CreateState(pc: 0x1000, y: 0x55, p: 0, cycles: 10);

        // Act
        var handler = Instructions.STY(AddressingModes.ZeroPage);
        handler(memory, ref state);

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
        var state = CreateState(pc: 0x1000, p: 0, cycles: 10);

        // Act
        var handler = Instructions.STZ(AddressingModes.ZeroPage);
        handler(memory, ref state);

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
        var state = CreateState(pc: 0x1000, x: 0x42, sp: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.PHX(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xFE));
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
        var state = CreateState(pc: 0x1000, x: 0x00, sp: 0xFE, p: 0, cycles: 10);

        // Act
        var handler = Instructions.PLX(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xFF));
        Assert.That(state.Registers.X.GetByte(), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies PHY pushes Y register to stack.
    /// </summary>
    [Test]
    public void PHY_PushesYRegisterToStack()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, y: 0x55, sp: 0xFF, p: 0, cycles: 10);

        // Act
        var handler = Instructions.PHY(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xFE));
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
        var state = CreateState(pc: 0x1000, y: 0x00, sp: 0xFE, p: 0, cycles: 10);

        // Act
        var handler = Instructions.PLY(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Registers.SP.GetByte(), Is.EqualTo(0xFF));
        Assert.That(state.Registers.Y.GetByte(), Is.EqualTo(0x55));
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
        var state = CreateState(pc: 0x1000, a: 0xF0, p: 0, cycles: 10);

        // Act
        var handler = Instructions.TSB(AddressingModes.ZeroPage);
        handler(memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0xFF)); // 0x0F OR 0xF0
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo(FlagZ), "Zero flag should be set (RegisterAccumulator AND M was 0)");
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
        var state = CreateState(pc: 0x1000, a: 0x80, p: FlagZ, cycles: 10);

        // Act
        var handler = Instructions.TSB(AddressingModes.ZeroPage);
        handler(memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0xFF)); // 0xFF OR 0x80 = 0xFF
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo((ProcessorStatusFlags)0), "Zero flag should be clear (RegisterAccumulator AND M != 0)");
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
        var state = CreateState(pc: 0x1000, a: 0xF0, p: 0, cycles: 10);

        // Act
        var handler = Instructions.TRB(AddressingModes.ZeroPage);
        handler(memory, ref state);

        // Assert
        Assert.That(memory.Read(0x50), Is.EqualTo(0x0F)); // 0xFF AND (NOT 0xF0)
        Assert.That(state.Registers.P & FlagZ, Is.EqualTo((ProcessorStatusFlags)0), "Zero flag should be clear (RegisterAccumulator AND M != 0)");
    }

    /// <summary>
    /// Verifies WAI halts the processor.
    /// </summary>
    [Test]
    public void WAI_HaltsProcessor()
    {
        // Arrange
        var state = CreateState(pc: 0x1000, haltReason: HaltState.None, p: 0, cycles: 10);

        // Act
        var handler = Instructions.WAI(AddressingModes.Implied);
        handler(memory, ref state);

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
        var state = CreateState(pc: 0x1000, haltReason: HaltState.None, p: 0, cycles: 10);

        // Act
        var handler = Instructions.STP(AddressingModes.Implied);
        handler(memory, ref state);

        // Assert
        Assert.That(state.Halted, Is.True, "Processor should be halted");
    }

    #endregion

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
        ulong cycles = 0,
        HaltState haltReason = HaltState.None,
        bool compat = true)
    {
        var state = new CpuState { Cycles = cycles, HaltReason = haltReason };
        state.Registers.Reset(compat);
        state.Registers.PC.SetWord(pc);
        state.Registers.A.SetByte(a);
        state.Registers.X.SetByte(x);
        state.Registers.Y.SetByte(y);
        state.Registers.SP.SetByte(sp);
        state.Registers.P = p;
        return state;
    }
}