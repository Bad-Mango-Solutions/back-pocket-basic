// <copyright file="UnusedNopOpcodeTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;

using TestHelpers;

/// <summary>
/// Verifies that the WDC W65C02S "unused" opcodes are decoded as NOPs with the
/// correct byte length and cycle cost, per section 6 of the
/// <c>65C02 Apple II Emulator Correctness Checklist.md</c>.
/// </summary>
/// <remarks>
/// These slots are listed by the WDC datasheet as reserved/no-effect on the
/// 65C02. The most user-visible consumer is the ProDOS 2.4.3 boot1 code at
/// <c>$102C: C2 02</c>, which uses opcode <c>$C2</c> as a 65C02-vs-65816 CPU
/// detector: on a 65C02 it must execute as a silent 2-byte/2-cycle NOP and
/// leave the Z flag intact, so the subsequent <c>BEQ</c> branch is taken into
/// the 65C02 boot path. On a 65816 the same byte decodes as <c>REP #imm</c>,
/// which clears Z and the branch falls through.
/// </remarks>
[TestFixture]
public class UnusedNopOpcodeTests : CpuTestBase
{
    /// <summary>
    /// Sets up test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        Cpu.Reset();
    }

    /// <summary>
    /// Verifies that <c>$C2 $02</c> (the exact bytes ProDOS uses for its 65816
    /// detection) executes as a silent 2-byte, 2-cycle NOP that preserves the Z
    /// flag, so the following <c>BEQ</c> is taken on a 65C02.
    /// </summary>
    [Test]
    public void C2_Executes_AsTwoByteTwoCycleNop_PreservingZeroFlag()
    {
        // Arrange: replay ProDOS bitsy-bye CPU-detection idiom.
        //   $102C: C2 02   ; NOP #$02 on 65C02 (REP #$02 on 65816)
        //   $102E: F0 04   ; BEQ $1034 - taken on 65C02 because Z survives
        WriteWord(0xFFFC, 0x102C);
        Write(0x102C, 0xC2);
        Write(0x102D, 0x02);
        Write(0x102E, 0xF0);
        Write(0x102F, 0x04);
        Cpu.Reset();

        // Set Z=1 (and a sentinel value in every other observable register)
        // so we can prove $C2 doesn't disturb anything.
        SetupCpu(
            pc: 0x102C,
            a: 0x5A,
            x: 0xA5,
            y: 0x3C,
            sp: 0xFD,
            p: ProcessorStatusFlags.Z | ProcessorStatusFlags.I,
            cycles: 100);

        // Act
        var stepResult = Cpu.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x102E), "PC must advance by exactly 2 bytes (opcode + immediate).");
            Assert.That(stepResult.CyclesConsumed.Value, Is.EqualTo(2UL), "Cost must be exactly 2 cycles.");
            Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x5A), "Accumulator must be preserved.");
            Assert.That(Cpu.Registers.X.GetByte(), Is.EqualTo(0xA5), "X must be preserved.");
            Assert.That(Cpu.Registers.Y.GetByte(), Is.EqualTo(0x3C), "Y must be preserved.");
            Assert.That(Cpu.Registers.SP.GetByte(), Is.EqualTo(0xFD), "SP must be preserved.");
            Assert.That(Cpu.Registers.P, Is.EqualTo(ProcessorStatusFlags.Z | ProcessorStatusFlags.I), "Processor flags (notably Z) must be preserved.");
            Assert.That(Cpu.Halted, Is.False, "$C2 must NOT halt the CPU as an illegal opcode.");
        });
    }

    /// <summary>
    /// Verifies that all seven 2-byte/2-cycle immediate-mode NOP slots
    /// ($02, $22, $42, $62, $82, $C2, $E2) advance PC by 2 and consume 2 cycles.
    /// </summary>
    /// <param name="opcode">The unused-opcode slot under test.</param>
    [TestCase((byte)0x02)]
    [TestCase((byte)0x22)]
    [TestCase((byte)0x42)]
    [TestCase((byte)0x62)]
    [TestCase((byte)0x82)]
    [TestCase((byte)0xC2)]
    [TestCase((byte)0xE2)]
    public void ImmediateNopSlot_AdvancesPcByTwoAndConsumesTwoCycles(byte opcode)
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, opcode);
        Write(0x1001, 0xFF); // immediate operand
        Cpu.Reset();
        SetupCpu(pc: 0x1000, p: (ProcessorStatusFlags)0, cycles: 0);

        var stepResult = Cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1002));
            Assert.That(stepResult.CyclesConsumed.Value, Is.EqualTo(2UL));
            Assert.That(Cpu.Registers.P, Is.EqualTo((ProcessorStatusFlags)0));
            Assert.That(Cpu.Halted, Is.False);
        });
    }

    /// <summary>
    /// Verifies that $44 (the zero-page NOP slot) advances PC by 2 and consumes
    /// 3 cycles.
    /// </summary>
    [Test]
    public void Opcode44_ZeroPageNop_AdvancesPcByTwoAndConsumesThreeCycles()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x44);
        Write(0x1001, 0x80);
        Cpu.Reset();
        SetupCpu(pc: 0x1000, cycles: 0);

        var stepResult = Cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1002));
            Assert.That(stepResult.CyclesConsumed.Value, Is.EqualTo(3UL));
            Assert.That(Cpu.Halted, Is.False);
        });
    }

    /// <summary>
    /// Verifies that the three zero-page,X NOP slots ($54, $D4, $F4) advance PC
    /// by 2 and consume 4 cycles.
    /// </summary>
    /// <param name="opcode">The unused-opcode slot under test.</param>
    [TestCase((byte)0x54)]
    [TestCase((byte)0xD4)]
    [TestCase((byte)0xF4)]
    public void ZeroPageXNopSlot_AdvancesPcByTwoAndConsumesFourCycles(byte opcode)
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, opcode);
        Write(0x1001, 0x80);
        Cpu.Reset();
        SetupCpu(pc: 0x1000, x: 0x05, cycles: 0);

        var stepResult = Cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1002));
            Assert.That(stepResult.CyclesConsumed.Value, Is.EqualTo(4UL));
            Assert.That(Cpu.Halted, Is.False);
        });
    }

    /// <summary>
    /// Verifies that the two 3-byte/4-cycle absolute NOP slots ($DC, $FC)
    /// advance PC by 3 and consume 4 cycles.
    /// </summary>
    /// <param name="opcode">The unused-opcode slot under test.</param>
    [TestCase((byte)0xDC)]
    [TestCase((byte)0xFC)]
    public void AbsoluteNopSlot_AdvancesPcByThreeAndConsumesFourCycles(byte opcode)
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, opcode);
        Write(0x1001, 0x34);
        Write(0x1002, 0x12);
        Cpu.Reset();
        SetupCpu(pc: 0x1000, cycles: 0);

        var stepResult = Cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1003));
            Assert.That(stepResult.CyclesConsumed.Value, Is.EqualTo(4UL));
            Assert.That(Cpu.Halted, Is.False);
        });
    }

    /// <summary>
    /// Verifies that $5C (the "loud" absolute NOP) advances PC by 3 and consumes
    /// 8 cycles, as documented in the WDC W65C02S datasheet.
    /// </summary>
    [Test]
    public void Opcode5C_LoudAbsoluteNop_AdvancesPcByThreeAndConsumesEightCycles()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x5C);
        Write(0x1001, 0x34);
        Write(0x1002, 0x12);
        Cpu.Reset();
        SetupCpu(pc: 0x1000, cycles: 0);

        var stepResult = Cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1003));
            Assert.That(stepResult.CyclesConsumed.Value, Is.EqualTo(8UL));
            Assert.That(Cpu.Halted, Is.False);
        });
    }

    /// <summary>
    /// Verifies that every column-3 unused opcode ($03, $13, ..., $F3) is a
    /// 1-byte, 1-cycle NOP that preserves all flags and registers.
    /// </summary>
    /// <param name="opcode">The unused-opcode slot under test.</param>
    [TestCase((byte)0x03)]
    [TestCase((byte)0x13)]
    [TestCase((byte)0x23)]
    [TestCase((byte)0x33)]
    [TestCase((byte)0x43)]
    [TestCase((byte)0x53)]
    [TestCase((byte)0x63)]
    [TestCase((byte)0x73)]
    [TestCase((byte)0x83)]
    [TestCase((byte)0x93)]
    [TestCase((byte)0xA3)]
    [TestCase((byte)0xB3)]
    [TestCase((byte)0xC3)]
    [TestCase((byte)0xD3)]
    [TestCase((byte)0xE3)]
    [TestCase((byte)0xF3)]
    public void Column3NopSlot_AdvancesPcByOneAndConsumesOneCycle(byte opcode)
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, opcode);
        Cpu.Reset();
        SetupCpu(pc: 0x1000, a: 0x77, x: 0x88, y: 0x99, p: ProcessorStatusFlags.Z, cycles: 0);

        var stepResult = Cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
            Assert.That(stepResult.CyclesConsumed.Value, Is.EqualTo(1UL));
            Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x77));
            Assert.That(Cpu.Registers.X.GetByte(), Is.EqualTo(0x88));
            Assert.That(Cpu.Registers.Y.GetByte(), Is.EqualTo(0x99));
            Assert.That(Cpu.Registers.P, Is.EqualTo(ProcessorStatusFlags.Z));
            Assert.That(Cpu.Halted, Is.False);
        });
    }

    /// <summary>
    /// Verifies that every column-B unused opcode ($0B, $1B, ..., $FB) is a
    /// 1-byte, 1-cycle NOP that preserves all flags and registers.
    /// </summary>
    /// <param name="opcode">The unused-opcode slot under test.</param>
    [TestCase((byte)0x0B)]
    [TestCase((byte)0x1B)]
    [TestCase((byte)0x2B)]
    [TestCase((byte)0x3B)]
    [TestCase((byte)0x4B)]
    [TestCase((byte)0x5B)]
    [TestCase((byte)0x6B)]
    [TestCase((byte)0x7B)]
    [TestCase((byte)0x8B)]
    [TestCase((byte)0x9B)]
    [TestCase((byte)0xAB)]
    [TestCase((byte)0xBB)]
    [TestCase((byte)0xEB)]
    [TestCase((byte)0xFB)]
    public void ColumnBNopSlot_AdvancesPcByOneAndConsumesOneCycle(byte opcode)
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, opcode);
        Cpu.Reset();
        SetupCpu(pc: 0x1000, a: 0x55, x: 0xAA, y: 0xCC, p: ProcessorStatusFlags.N | ProcessorStatusFlags.V, cycles: 0);

        var stepResult = Cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
            Assert.That(stepResult.CyclesConsumed.Value, Is.EqualTo(1UL));
            Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x55));
            Assert.That(Cpu.Registers.X.GetByte(), Is.EqualTo(0xAA));
            Assert.That(Cpu.Registers.Y.GetByte(), Is.EqualTo(0xCC));
            Assert.That(Cpu.Registers.P, Is.EqualTo(ProcessorStatusFlags.N | ProcessorStatusFlags.V));
            Assert.That(Cpu.Halted, Is.False);
        });
    }

    /// <summary>
    /// Sets up the CPU registers for testing with the specified values.
    /// </summary>
    /// <param name="pc">Program counter value.</param>
    /// <param name="a">Accumulator value.</param>
    /// <param name="x">X register value.</param>
    /// <param name="y">Y register value.</param>
    /// <param name="sp">Stack pointer value.</param>
    /// <param name="p">Processor status flags.</param>
    /// <param name="cycles">Initial CPU cycle count.</param>
    /// <param name="compat">Whether to reset registers in 6502 compatibility mode.</param>
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