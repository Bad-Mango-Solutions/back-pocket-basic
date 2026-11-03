// <copyright file="Cpu65C02NewOpcodesTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;

using TestHelpers;

/// <summary>
/// Comprehensive tests for newly implemented 65C02 opcodes.
/// </summary>
/// <remarks>
/// Tests Phase 1 implementation: new addressing modes (ZeroPageIndirect, AbsoluteIndirectX),
/// BIT immediate, INC A/DEC A, JMP ($abs,X), and all (zp) instructions.
/// </remarks>
[TestFixture]
public class Cpu65C02NewOpcodesTests : CpuTestBase
{
    /// <summary>
    /// Sets up test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        Cpu.Reset();
    }

    #region ZeroPageIndirect Tests

    /// <summary>
    /// Verifies LDA ($zp) reads from address in zero page pointer.
    /// </summary>
    [Test]
    public void LDA_ZeroPageIndirect_LoadsCorrectValue()
    {
        // Arrange: LDA ($20) where ($20/$21) = $1234
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xB2); // LDA ($zp)
        Write(0x1001, 0x20); // Zero page offset
        WriteWord(0x0020, 0x1234); // Pointer in zero page
        Write(0x1234, 0x42); // Target value
        Cpu.Reset();

        // Act
        var result = Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x42), "A should contain loaded value");
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1002), "PC should advance by 2");
            Assert.That(result.CyclesConsumed.Value, Is.EqualTo(5UL), "Should consume 5 cycles");
        });
    }

    /// <summary>
    /// Verifies STA ($zp) stores accumulator to address in zero page pointer.
    /// </summary>
    [Test]
    public void STA_ZeroPageIndirect_StoresCorrectValue()
    {
        // Arrange: STA ($30) where ($30/$31) = $2000
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x92); // STA ($zp)
        Write(0x1001, 0x30); // Zero page offset
        WriteWord(0x0030, 0x2000); // Pointer in zero page
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.A.SetByte(0x5A);

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Read(0x2000), Is.EqualTo(0x5A), "Value should be stored at target address");
    }

    /// <summary>
    /// Verifies ORA ($zp) performs OR operation.
    /// </summary>
    [Test]
    public void ORA_ZeroPageIndirect_PerformsOrOperation()
    {
        // Arrange: ORA ($40) where ($40/$41) = $3000, memory = $0F
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x12); // ORA ($zp)
        Write(0x1001, 0x40);
        WriteWord(0x0040, 0x3000);
        Write(0x3000, 0x0F);
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.A.SetByte(0xF0);

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0xFF), "A should be OR'd with memory");
    }

    /// <summary>
    /// Verifies ADC ($zp) performs addition with carry.
    /// </summary>
    [Test]
    public void ADC_ZeroPageIndirect_PerformsAddition()
    {
        // Arrange: ADC ($50) where ($50/$51) = $4000, memory = $10
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x72); // ADC ($zp)
        Write(0x1001, 0x50);
        WriteWord(0x0050, 0x4000);
        Write(0x4000, 0x10);
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.A.SetByte(0x20);
        Cpu.Registers.P = 0;

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x30), "A should be sum of A + memory");
    }

    #endregion

    #region JMP ($abs,X) Tests

    /// <summary>
    /// Verifies JMP ($abs,X) jumps to address at (base+X).
    /// </summary>
    [Test]
    public void JMP_AbsoluteIndirectX_JumpsToCorrectAddress()
    {
        // Arrange: JMP ($2000,X) with X=$10, ($2010) = $8000
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x7C); // JMP ($abs,X)
        WriteWord(0x1001, 0x2000);
        WriteWord(0x2010, 0x8000); // Target address
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.X.SetByte(0x10);

        // Act
        var result = Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x8000), "PC should jump to target");
            Assert.That(result.CyclesConsumed.Value, Is.EqualTo(6UL), "JMP ($abs,X) takes 6 cycles");
        });
    }

    /// <summary>
    /// Verifies JMP ($abs,X) with page boundary crossing.
    /// </summary>
    [Test]
    public void JMP_AbsoluteIndirectX_CrossesPageBoundary()
    {
        // Arrange: JMP ($20FF,X) with X=$01, should read from $2100 not wrap to $2000
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x7C); // JMP ($abs,X)
        WriteWord(0x1001, 0x20FF);
        WriteWord(0x2100, 0x5000); // Target at $2100
        WriteWord(0x2000, 0xDEAD); // Should NOT read from here
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.X.SetByte(0x01);

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x5000), "Should read correctly across page");
    }

    #endregion

    #region BIT Immediate Tests

    /// <summary>
    /// Verifies BIT #imm only affects Z flag, not N or V.
    /// </summary>
    [Test]
    public void BIT_Immediate_OnlyAffectsZeroFlag()
    {
        // Arrange: BIT #$00 with A=$FF, previous N=1, V=1
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x89); // BIT #imm
        Write(0x1001, 0x00);
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.A.SetByte(0xFF);
        Cpu.Registers.P = ProcessorStatusFlags.N | ProcessorStatusFlags.V;

        // Act
        Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.Z) != 0, Is.True, "Z should be set");
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.N) != 0, Is.True, "N should be UNCHANGED");
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.V) != 0, Is.True, "V should be UNCHANGED");
        });
    }

    /// <summary>
    /// Verifies BIT $abs sets N and V from memory bits.
    /// </summary>
    [Test]
    public void BIT_Absolute_SetsNandVFromMemory()
    {
        // Arrange: BIT $2000 with A=$FF, memory=$C0 (N=1, V=1)
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x2C); // BIT abs
        WriteWord(0x1001, 0x2000);
        Write(0x2000, 0xC0); // Bit 7=1, Bit 6=1
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.A.SetByte(0xFF);
        Cpu.Registers.P = 0;

        // Act
        Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.Z) == 0, Is.True, "Z should be clear (result non-zero)");
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.N) != 0, Is.True, "N should be set from bit 7");
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.V) != 0, Is.True, "V should be set from bit 6");
        });
    }

    #endregion

    #region INC A / DEC A Tests

    /// <summary>
    /// Verifies INC A increments accumulator.
    /// </summary>
    [Test]
    public void INA_IncrementsAccumulator()
    {
        // Arrange: INC A with A=$42
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x1A); // INC A
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.A.SetByte(0x42);

        // Act
        Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x43), "A should be incremented");
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.Z) == 0, Is.True, "Z should be clear");
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.N) == 0, Is.True, "N should be clear");
        });
    }

    /// <summary>
    /// Verifies INC A on $FF wraps to $00 and sets Z flag.
    /// </summary>
    [Test]
    public void INA_WrapsToZeroAndSetsZeroFlag()
    {
        // Arrange: INC A with A=$FF
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x1A); // INC A
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.A.SetByte(0xFF);

        // Act
        Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x00), "A should wrap to zero");
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.Z) != 0, Is.True, "Z should be set");
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.N) == 0, Is.True, "N should be clear");
        });
    }

    /// <summary>
    /// Verifies DEC A decrements accumulator.
    /// </summary>
    [Test]
    public void DEA_DecrementsAccumulator()
    {
        // Arrange: DEC A with A=$42
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x3A); // DEC A
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.A.SetByte(0x42);

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x41), "A should be decremented");
    }

    /// <summary>
    /// Verifies DEC A on $00 wraps to $FF and sets N flag.
    /// </summary>
    [Test]
    public void DEA_WrapsToFFAndSetsNegativeFlag()
    {
        // Arrange: DEC A with A=$00
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x3A); // DEC A
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.A.SetByte(0x00);

        // Act
        Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0xFF), "A should wrap to FF");
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.Z) == 0, Is.True, "Z should be clear");
            Assert.That((Cpu.Registers.P & ProcessorStatusFlags.N) != 0, Is.True, "N should be set");
        });
    }

    #endregion

    #region BRK D-flag Clear Test

    /// <summary>
    /// Verifies BRK clears D flag on 65C02.
    /// </summary>
    [Test]
    public void BRK_ClearsDFlag()
    {
        // Arrange: BRK with D=1
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x00); // BRK
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.P = ProcessorStatusFlags.D;

        // Act
        Cpu.Step();

        // Assert
        Assert.That((Cpu.Registers.P & ProcessorStatusFlags.D) == 0, Is.True, "D flag should be cleared after BRK");
    }

    #endregion
}