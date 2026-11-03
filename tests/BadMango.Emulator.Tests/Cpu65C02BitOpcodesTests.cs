// <copyright file="Cpu65C02BitOpcodesTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;

using TestHelpers;

/// <summary>
/// Tests for Rockwell/WDC bit manipulation instructions (RMB, SMB, BBR, BBS).
/// </summary>
[TestFixture]
public class Cpu65C02BitOpcodesTests : CpuTestBase
{
    /// <summary>
    /// Sets up test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        Cpu.Reset();
    }

    #region RMB Tests

    /// <summary>
    /// Verifies RMB3 $80 clears bit 3.
    /// </summary>
    [Test]
    public void RMB3_ClearsBit3()
    {
        // Arrange: RMB3 $80 with ($80)=$FF
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x37); // RMB3 $zp
        Write(0x1001, 0x80);
        Write(0x0080, 0xFF);
        Cpu.Reset();

        // Act
        var result = Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Read(0x0080), Is.EqualTo(0xF7), "Bit 3 should be cleared");
            Assert.That(result.CyclesConsumed.Value, Is.EqualTo(5UL), "RMB takes 5 cycles");
        });
    }

    /// <summary>
    /// Verifies RMB operations do not affect flags.
    /// </summary>
    [Test]
    public void RMB_DoesNotAffectFlags()
    {
        // Arrange: RMB0 $80 with all flags set
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x07); // RMB0 $zp
        Write(0x1001, 0x80);
        Write(0x0080, 0xFF);
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.P = (ProcessorStatusFlags)0xFF;

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P, Is.EqualTo((ProcessorStatusFlags)0xFF), "Flags should be unchanged");
    }

    #endregion

    #region SMB Tests

    /// <summary>
    /// Verifies SMB5 $80 sets bit 5.
    /// </summary>
    [Test]
    public void SMB5_SetsBit5()
    {
        // Arrange: SMB5 $80 with ($80)=$00
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xD7); // SMB5 $zp
        Write(0x1001, 0x80);
        Write(0x0080, 0x00);
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Read(0x0080), Is.EqualTo(0x20), "Bit 5 should be set");
    }

    #endregion

    #region BBR Tests

    /// <summary>
    /// Verifies BBR0 $80,+4 branches when bit 0 is reset.
    /// </summary>
    [Test]
    public void BBR0_BranchesWhenBitIsReset()
    {
        // Arrange: BBR0 $80,+4 with ($80)=$FE (bit 0 is clear)
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x0F); // BBR0 $zp,rel
        Write(0x1001, 0x80); // Zero page address
        Write(0x1002, 0x04); // Relative offset +4
        Write(0x0080, 0xFE); // Bit 0 is reset
        Cpu.Reset();

        // Act
        var result = Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1007), "Should branch to PC+3+4");
            Assert.That(result.CyclesConsumed.Value, Is.EqualTo(6UL), "BBR taken: 5 base + 1 branch");
        });
    }

    /// <summary>
    /// Verifies BBR0 $80,+4 does not branch when bit 0 is set.
    /// </summary>
    [Test]
    public void BBR0_DoesNotBranchWhenBitIsSet()
    {
        // Arrange: BBR0 $80,+4 with ($80)=$01 (bit 0 is set)
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x0F); // BBR0 $zp,rel
        Write(0x1001, 0x80);
        Write(0x1002, 0x04);
        Write(0x0080, 0x01); // Bit 0 is set
        Cpu.Reset();

        // Act
        var result = Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1003), "Should not branch, PC+3");
            Assert.That(result.CyclesConsumed.Value, Is.EqualTo(5UL), "BBR not taken: 5 cycles");
        });
    }

    /// <summary>
    /// Verifies BBR does not affect flags.
    /// </summary>
    [Test]
    public void BBR_DoesNotAffectFlags()
    {
        // Arrange: BBR0 with all flags set
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x0F); // BBR0
        Write(0x1001, 0x80);
        Write(0x1002, 0x00);
        Write(0x0080, 0xFE);
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0x1000);
        Cpu.Registers.P = (ProcessorStatusFlags)0xFF;

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P, Is.EqualTo((ProcessorStatusFlags)0xFF), "Flags should be unchanged");
    }

    /// <summary>
    /// Verifies BBR0 branch targets wrap at the 16-bit boundary ($FFFF to $0000)
    /// instead of escaping the 64KB PC space.
    /// </summary>
    [Test]
    public void BBR0_BranchTargetWrapsAt64K()
    {
        // Arrange: BBR0 $80,+$7F at $FFF0 with bit 0 clear; PC after fetch is $FFF3,
        // so the target $FFF3 + $7F = $10072 must wrap to $0072.
        WriteWord(0xFFFC, 0x1000);
        Write(0xFFF0, 0x0F); // BBR0 $zp,rel
        Write(0xFFF1, 0x80);
        Write(0xFFF2, 0x7F);
        Write(0x0080, 0x00);
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0xFFF0);

        // Act
        Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x0072), "Branch target should wrap to $0072");
            Assert.That(Cpu.Registers.PC.GetAddr(), Is.EqualTo(0x0072u), "PC must stay within 16 bits");
        });
    }

    #endregion

    #region BBS Tests

    /// <summary>
    /// Verifies BBS7 $80,+4 branches when bit 7 is set.
    /// </summary>
    [Test]
    public void BBS7_BranchesWhenBitIsSet()
    {
        // Arrange: BBS7 $80,+4 with ($80)=$80 (bit 7 is set)
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xFF); // BBS7 $zp,rel
        Write(0x1001, 0x80);
        Write(0x1002, 0x04);
        Write(0x0080, 0x80); // Bit 7 is set
        Cpu.Reset();

        // Act
        var result = Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1007), "Should branch to PC+3+4");
            Assert.That(result.CyclesConsumed.Value, Is.EqualTo(6UL), "BBS taken: 5 base + 1 branch");
        });
    }

    /// <summary>
    /// Verifies BBS7 does not branch when bit is reset.
    /// </summary>
    [Test]
    public void BBS7_DoesNotBranchWhenBitIsReset()
    {
        // Arrange: BBS7 $80,+4 with ($80)=$7F (bit 7 is clear)
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xFF); // BBS7
        Write(0x1001, 0x80);
        Write(0x1002, 0x04);
        Write(0x0080, 0x7F); // Bit 7 is reset
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1003), "Should not branch");
    }

    /// <summary>
    /// Verifies BBR/BBS with negative offset (backward branch).
    /// </summary>
    [Test]
    public void BBR_WithNegativeOffset_BranchesBackward()
    {
        // Arrange: BBR0 $80,-4 (offset = $FC = -4) with bit 0 clear
        WriteWord(0xFFFC, 0x1010);
        Write(0x1010, 0x0F); // BBR0
        Write(0x1011, 0x80);
        Write(0x1012, 0xFC); // -4 (signed)
        Write(0x0080, 0xFE); // Bit 0 clear
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x100F), "Should branch backward: $1013 + (-4) = $100F");
    }

    /// <summary>
    /// Verifies BBS0 branch targets wrap at the 16-bit boundary ($FFFF to $0000)
    /// instead of escaping the 64KB PC space.
    /// </summary>
    [Test]
    public void BBS0_BranchTargetWrapsAt64K()
    {
        // Arrange: BBS0 $80,+$7F at $FFF0 with bit 0 set; PC after fetch is $FFF3,
        // so the target $FFF3 + $7F = $10072 must wrap to $0072.
        WriteWord(0xFFFC, 0x1000);
        Write(0xFFF0, 0x8F); // BBS0 $zp,rel
        Write(0xFFF1, 0x80);
        Write(0xFFF2, 0x7F);
        Write(0x0080, 0x01);
        Cpu.Reset();
        Cpu.Registers.PC.SetWord(0xFFF0);

        // Act
        Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x0072), "Branch target should wrap to $0072");
            Assert.That(Cpu.Registers.PC.GetAddr(), Is.EqualTo(0x0072u), "PC must stay within 16 bits");
        });
    }

    #endregion
}