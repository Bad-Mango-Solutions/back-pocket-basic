// <copyright file="Cpu65C02StackWrapTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using TestHelpers;

/// <summary>
/// Regression tests for 8-bit stack pointer wrap-around on the 65C02 (issue #304).
/// </summary>
/// <remarks>
/// The 65C02 stack pointer is 8 bits wide and the stack always lives in page $01.
/// Pushing at SP=$00 writes $0100 and wraps SP to $FF; popping at SP=$FF wraps SP to
/// $00 and reads $0100. SP must never carry bits above bit 7.
/// </remarks>
[TestFixture]
public class Cpu65C02StackWrapTests : CpuTestBase
{
    private const ushort ProgramStart = 0x1000;

    /// <summary>
    /// PHA at SP=$00 writes $0100 and wraps SP to $FF.
    /// </summary>
    [Test]
    public void Push_AtSpZero_WritesPage1Base_AndWrapsToFF()
    {
        LoadProgram(
        [
            0xA2, 0x00, // LDX #$00
            0x9A,       // TXS
            0xA9, 0x5A, // LDA #$5A
            0x48,       // PHA
        ]);

        StepTimes(4);

        Assert.Multiple(() =>
        {
            Assert.That(Read(0x0100), Is.EqualTo(0x5A));
            Assert.That(Read(0x0000), Is.EqualTo(0x00), "push must not spill into zero page");
            Assert.That(Cpu.Registers.SP.stack, Is.EqualTo(0xFFu));
        });
    }

    /// <summary>
    /// PLA at SP=$FF wraps SP to $00 and reads $0100.
    /// </summary>
    [Test]
    public void Pop_AtSpFF_ReadsPage1Base_AndWrapsToZero()
    {
        Write(0x0100, 0xA5);
        Write(0x0200, 0x11); // what a non-wrapping pop would read
        LoadProgram(
        [
            0xA2, 0xFF, // LDX #$FF
            0x9A,       // TXS
            0x68,       // PLA
        ]);

        StepTimes(3);

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0xA5));
            Assert.That(Cpu.Registers.SP.stack, Is.EqualTo(0x00u));
        });
    }

    /// <summary>
    /// TXS with X=$00 followed by PHP/PLA round-trips the status byte through $0100.
    /// </summary>
    [Test]
    public void Txs_XZero_PhpPla_RoundTripsThroughPage1Base()
    {
        LoadProgram(
        [
            0xA2, 0x00, // LDX #$00
            0x9A,       // TXS
            0x08,       // PHP
            0x68,       // PLA
        ]);

        StepTimes(3);
        byte pushedStatus = Read(0x0100);
        Assert.That(Cpu.Registers.SP.stack, Is.EqualTo(0xFFu));

        StepTimes(1);

        Assert.Multiple(() =>
        {
            Assert.That(pushedStatus & 0x30, Is.EqualTo(0x30), "PHP pushes B and bit 5 set");
            Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(pushedStatus));
            Assert.That(Cpu.Registers.SP.stack, Is.EqualTo(0x00u));
        });
    }

    /// <summary>
    /// JSR at SP=$00 pushes the return address across the wrap ($0100, then $01FF),
    /// and RTS pops it back and restores SP to $00.
    /// </summary>
    [Test]
    public void JsrRts_AcrossWrap_ReturnsToCaller()
    {
        LoadProgram(
        [
            0xA2, 0x00,       // $1000 LDX #$00
            0x9A,             // $1002 TXS
            0x20, 0x00, 0x20, // $1003 JSR $2000
            0xEA,             // $1006 NOP
        ]);
        Write(0x2000, 0x60);  // $2000 RTS

        StepTimes(3); // LDX, TXS, JSR

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x2000));
            Assert.That(Read(0x0100), Is.EqualTo(0x10), "return address high byte at $0100");
            Assert.That(Read(0x01FF), Is.EqualTo(0x05), "return address low byte wraps to $01FF");
            Assert.That(Cpu.Registers.SP.stack, Is.EqualTo(0xFEu));
        });

        StepTimes(1); // RTS

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1006));
            Assert.That(Cpu.Registers.SP.stack, Is.EqualTo(0x00u));
        });
    }

    /// <summary>
    /// Many pushes past SP=$00 keep SP within $00-$FF and stay in page $01.
    /// </summary>
    [Test]
    public void RepeatedPushes_StayWithinPage1()
    {
        LoadProgram(
        [
            0xA2, 0x00,       // $1000 LDX #$00
            0x9A,             // $1002 TXS
            0xA9, 0x5A,       // $1003 LDA #$5A
            0x48,             // $1005 PHA
            0x4C, 0x05, 0x10, // $1006 JMP $1005
        ]);

        StepTimes(3);
        for (int i = 0; i < 600; i++)
        {
            StepTimes(2); // PHA, JMP
            Assert.That(Cpu.Registers.SP.stack, Is.LessThanOrEqualTo(0xFFu));
        }

        Assert.Multiple(() =>
        {
            Assert.That(Read(0x00FF), Is.EqualTo(0x00), "pushes must not spill into zero page");
            Assert.That(Read(0x01FF), Is.EqualTo(0x5A));
        });
    }

    private void LoadProgram(byte[] program)
    {
        for (int i = 0; i < program.Length; i++)
        {
            Write((uint)(ProgramStart + i), program[i]);
        }

        WriteWord(0xFFFC, ProgramStart);
        Cpu.Reset();
    }

    private void StepTimes(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Cpu.Step();
        }
    }
}