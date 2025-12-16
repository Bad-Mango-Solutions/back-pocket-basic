// <copyright file="EmulationTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using Interpreter.Emulation;
using Microsoft.Extensions.Logging;
using Moq;

[TestFixture]
public class EmulationTests
{
    private AppleMemory _memory = null!;
    private Cpu6502 _cpu = null!;

    [SetUp]
    public void Setup()
    {
        var memoryLogger = new Mock<ILogger<AppleMemory>>();
        var cpuLogger = new Mock<ILogger<Cpu6502>>();

        _memory = new AppleMemory(memoryLogger.Object);
        _cpu = new Cpu6502(_memory, cpuLogger.Object);
    }

    [Test]
    public void Memory_ReadWrite_WorksCorrectly()
    {
        _memory.Write(0x300, 0x42);

        Assert.That(_memory.Read(0x300), Is.EqualTo(0x42));
    }

    [Test]
    public void Memory_ReadWriteWord_WorksCorrectly()
    {
        _memory.WriteWord(0x300, 0x1234);

        Assert.That(_memory.ReadWord(0x300), Is.EqualTo(0x1234));
        Assert.That(_memory.Read(0x300), Is.EqualTo(0x34)); // Low byte
        Assert.That(_memory.Read(0x301), Is.EqualTo(0x12)); // High byte
    }

    [Test]
    public void Memory_OutOfBounds_ThrowsException()
    {
        Assert.Throws<MemoryAccessException>(() => _memory.Read(0x10000));
        Assert.Throws<MemoryAccessException>(() => _memory.Write(0x10000, 0));
        Assert.Throws<MemoryAccessException>(() => _memory.Read(-1));
    }

    [Test]
    public void Memory_Clear_ZerosMemory()
    {
        _memory.Write(0x300, 0xFF);
        _memory.Clear();

        Assert.That(_memory.Read(0x300), Is.EqualTo(0));
    }

    [Test]
    public void Memory_LoadData_WritesCorrectly()
    {
        byte[] data = { 0x01, 0x02, 0x03, 0x04 };
        _memory.LoadData(0x300, data);

        Assert.That(_memory.Read(0x300), Is.EqualTo(0x01));
        Assert.That(_memory.Read(0x301), Is.EqualTo(0x02));
        Assert.That(_memory.Read(0x302), Is.EqualTo(0x03));
        Assert.That(_memory.Read(0x303), Is.EqualTo(0x04));
    }

    [Test]
    public void Cpu_Reset_InitializesRegisters()
    {
        _cpu.Reset();

        Assert.That(_cpu.Registers.A, Is.EqualTo(0));
        Assert.That(_cpu.Registers.X, Is.EqualTo(0));
        Assert.That(_cpu.Registers.Y, Is.EqualTo(0));
        Assert.That(_cpu.Registers.SP, Is.EqualTo(0xFF));
    }

    [Test]
    public void Cpu_LdaImmediate_LoadsValue()
    {
        // LDA #$42
        _memory.Write(0x0300, 0xA9); // LDA immediate
        _memory.Write(0x0301, 0x42); // Value
        _memory.Write(0x0302, 0x00); // BRK

        _cpu.Registers.PC = 0x0300;
        _cpu.Step();

        Assert.That(_cpu.Registers.A, Is.EqualTo(0x42));
    }

    [Test]
    public void Cpu_StaZeroPage_StoresValue()
    {
        // STA $50
        _cpu.Registers.A = 0x42;
        _memory.Write(0x0300, 0x85); // STA zero page
        _memory.Write(0x0301, 0x50); // Address

        _cpu.Registers.PC = 0x0300;
        _cpu.Step();

        Assert.That(_memory.Read(0x50), Is.EqualTo(0x42));
    }

    [Test]
    public void Cpu_Inx_IncrementsX()
    {
        _cpu.Registers.X = 0x41;
        _memory.Write(0x0300, 0xE8); // INX

        _cpu.Registers.PC = 0x0300;
        _cpu.Step();

        Assert.That(_cpu.Registers.X, Is.EqualTo(0x42));
    }

    [Test]
    public void Cpu_Dex_DecrementsX()
    {
        _cpu.Registers.X = 0x43;
        _memory.Write(0x0300, 0xCA); // DEX

        _cpu.Registers.PC = 0x0300;
        _cpu.Step();

        Assert.That(_cpu.Registers.X, Is.EqualTo(0x42));
    }

    [Test]
    public void Cpu_AdcImmediate_AddsWithCarry()
    {
        _cpu.Registers.A = 0x10;
        _cpu.Registers.Carry = false;
        _memory.Write(0x0300, 0x69); // ADC immediate
        _memory.Write(0x0301, 0x20);

        _cpu.Registers.PC = 0x0300;
        _cpu.Step();

        Assert.That(_cpu.Registers.A, Is.EqualTo(0x30));
    }

    [Test]
    public void Cpu_BneRelative_BranchesWhenNotZero()
    {
        _cpu.Registers.Zero = false;
        _memory.Write(0x0300, 0xD0); // BNE
        _memory.Write(0x0301, 0x05); // Offset (+5)

        _cpu.Registers.PC = 0x0300;
        _cpu.Step();

        Assert.That(_cpu.Registers.PC, Is.EqualTo(0x0307)); // 0x302 + 5
    }

    [Test]
    public void Cpu_BeqRelative_DoesNotBranchWhenNotZero()
    {
        _cpu.Registers.Zero = false;
        _memory.Write(0x0300, 0xF0); // BEQ
        _memory.Write(0x0301, 0x05); // Offset

        _cpu.Registers.PC = 0x0300;
        _cpu.Step();

        Assert.That(_cpu.Registers.PC, Is.EqualTo(0x0302)); // Just past instruction
    }

    [Test]
    public void Cpu_FlagsSetCorrectly_AfterLda()
    {
        _memory.Write(0x0300, 0xA9); // LDA immediate
        _memory.Write(0x0301, 0x00);

        _cpu.Registers.PC = 0x0300;
        _cpu.Step();

        Assert.That(_cpu.Registers.Zero, Is.True);
        Assert.That(_cpu.Registers.Negative, Is.False);

        _memory.Write(0x0302, 0xA9);
        _memory.Write(0x0303, 0x80); // Negative value

        _cpu.Step();

        Assert.That(_cpu.Registers.Zero, Is.False);
        Assert.That(_cpu.Registers.Negative, Is.True);
    }

    [Test]
    public void CpuRegisters_CarryFlag_WorksCorrectly()
    {
        var registers = new CpuRegisters();

        registers.Carry = true;
        Assert.That(registers.Carry, Is.True);
        Assert.That(registers.P & 0x01, Is.EqualTo(0x01));

        registers.Carry = false;
        Assert.That(registers.Carry, Is.False);
        Assert.That(registers.P & 0x01, Is.EqualTo(0x00));
    }

    [Test]
    public void CpuRegisters_ZeroFlag_WorksCorrectly()
    {
        var registers = new CpuRegisters();

        registers.Zero = true;
        Assert.That(registers.Zero, Is.True);

        registers.Zero = false;
        Assert.That(registers.Zero, Is.False);
    }

    [Test]
    public void CpuRegisters_SetNZ_SetsFlags()
    {
        var registers = new CpuRegisters();

        registers.SetNZ(0);
        Assert.That(registers.Zero, Is.True);
        Assert.That(registers.Negative, Is.False);

        registers.SetNZ(0x80);
        Assert.That(registers.Zero, Is.False);
        Assert.That(registers.Negative, Is.True);

        registers.SetNZ(0x42);
        Assert.That(registers.Zero, Is.False);
        Assert.That(registers.Negative, Is.False);
    }
}
