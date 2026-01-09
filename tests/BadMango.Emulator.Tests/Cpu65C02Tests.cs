// <copyright file="Cpu65C02Tests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;

using Emulation.Cpu;

using TestHelpers;

/// <summary>
/// Unit tests for the <see cref="Cpu65C02"/> class.
/// </summary>
[TestFixture]
public class Cpu65C02Tests : CpuTestBase
{
    /// <summary>
    /// Verifies that Reset() initializes the CPU to the correct state.
    /// </summary>
    [Test]
    public void Reset_InitializesCpuCorrectly()
    {
        // Arrange: Set reset vector to 0x1000
        WriteWord(0xFFFC, 0x1000);

        // Act
        Cpu.Reset();

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1000));
        Assert.That(Cpu.Registers.SP.GetByte(), Is.EqualTo(0xFF));
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0));
        Assert.That(Cpu.Registers.X.GetByte(), Is.EqualTo(0));
        Assert.That(Cpu.Registers.Y.GetByte(), Is.EqualTo(0));
        Assert.That(Cpu.Halted, Is.False);
    }

    /// <summary>
    /// Verifies that LDA immediate loads the accumulator correctly.
    /// </summary>
    [Test]
    public void LDA_Immediate_LoadsAccumulator()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$42
        Write(0x1001, 0x42);
        Cpu.Reset();

        // Act
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value;

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x42));
        Assert.That(cycles, Is.EqualTo(2));
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1002));
    }

    /// <summary>
    /// Verifies that LDA sets the Zero flag correctly.
    /// </summary>
    [Test]
    public void LDA_SetsZeroFlag()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$00
        Write(0x1001, 0x00);
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P & ProcessorStatusFlags.Z, Is.EqualTo(ProcessorStatusFlags.Z)); // Zero flag set
    }

    /// <summary>
    /// Verifies that LDA sets the Negative flag correctly.
    /// </summary>
    [Test]
    public void LDA_SetsNegativeFlag()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$FF
        Write(0x1001, 0xFF);
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Registers.P & ProcessorStatusFlags.N, Is.EqualTo(ProcessorStatusFlags.N)); // Negative flag set
    }

    /// <summary>
    /// Verifies that STA stores the accumulator to memory.
    /// </summary>
    [Test]
    public void STA_ZeroPage_StoresAccumulator()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$42
        Write(0x1001, 0x42);
        Write(0x1002, 0x85); // STA $10
        Write(0x1003, 0x10);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDA
        Cpu.Step(); // STA

        // Assert
        Assert.That(Read(0x10), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies that LDA from zero page works correctly.
    /// </summary>
    [Test]
    public void LDA_ZeroPage_LoadsFromMemory()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x10, 0x99); // Value in zero page
        Write(0x1000, 0xA5); // LDA $10
        Write(0x1001, 0x10);
        Cpu.Reset();

        // Act
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value;

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x99));
        Assert.That(cycles, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that LDA absolute addressing works correctly.
    /// </summary>
    [Test]
    public void LDA_Absolute_LoadsFromMemory()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x2000, 0x55); // Value at absolute address
        Write(0x1000, 0xAD); // LDA $2000
        WriteWord(0x1001, 0x2000);
        Cpu.Reset();

        // Act
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value;

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x55));
        Assert.That(cycles, Is.EqualTo(4));
    }

    /// <summary>
    /// Verifies that NOP executes correctly.
    /// </summary>
    [Test]
    public void NOP_ExecutesCorrectly()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xEA); // NOP
        Cpu.Reset();
        var pcBefore = Cpu.Registers.PC.GetWord();
        var aBefore = Cpu.Registers.A.GetByte();
        var xBefore = Cpu.Registers.X.GetByte();
        var yBefore = Cpu.Registers.Y.GetByte();

        // Act
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value;

        // Assert
        Assert.That(cycles, Is.EqualTo(2));
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(pcBefore + 1));
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(aBefore));
        Assert.That(Cpu.Registers.X.GetByte(), Is.EqualTo(xBefore));
        Assert.That(Cpu.Registers.Y.GetByte(), Is.EqualTo(yBefore));
    }

    /// <summary>
    /// Verifies that BRK does not halt the CPU.
    /// </summary>
    [Test]
    public void BRK_DoesNotHaltCpu()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x00); // BRK
        Write(0x2000, 0xEA); // NOP at IRQ handler
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert - BRK should not halt, execution continues from IRQ vector
        Assert.That(Cpu.Halted, Is.False);
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x2000), "PC should be at IRQ vector");
    }

    /// <summary>
    /// Verifies that cycle counting is accurate.
    /// </summary>
    [Test]
    public void CycleCounting_IsAccurate()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$42 (2 cycles)
        Write(0x1001, 0x42);
        Write(0x1002, 0xEA); // NOP (2 cycles)
        Write(0x1003, 0xA5); // LDA $10 (3 cycles)
        Write(0x1004, 0x10);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDA immediate
        Cpu.Step(); // NOP
        Cpu.Step(); // LDA zero page

        // Assert
        Assert.That(Cpu.GetCycles(), Is.EqualTo(2 + 2 + 3));
    }

    /// <summary>
    /// Verifies that GetRegisters returns only register state without cycle count.
    /// </summary>
    [Test]
    public void GetRegisters_ReturnsRegisterStateOnly()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$42
        Write(0x1001, 0x42);
        Cpu.Reset();
        Cpu.Step();

        // Act
        var registers = Cpu.GetRegisters();

        // Assert
        Assert.That(registers.A.GetByte(), Is.EqualTo(0x42));
        Assert.That(registers.PC.GetWord(), Is.EqualTo(0x1002));
        Assert.That(registers.SP.GetByte(), Is.EqualTo(0xFF));

        // Verify the registers struct doesn't have a Cycles property
        var registersType = typeof(Registers);
        Assert.That(registersType.GetProperty("Cycles"), Is.Null);
    }

    /// <summary>
    /// Verifies that LDA Zero Page,X loads correctly.
    /// </summary>
    [Test]
    public void LDA_ZeroPageX_LoadsFromMemory()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x20, 0x99); // Value at ZP $20
        Write(0x1000, 0xA2); // LDX #$10
        Write(0x1001, 0x10);
        Write(0x1002, 0xB5); // LDA $10,X ($10 + $10 = $20)
        Write(0x1003, 0x10);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDX #$10
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // LDA $10,X

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x99));
        Assert.That(cycles, Is.EqualTo(4)); // 4 cycles for ZP,X
    }

    /// <summary>
    /// Verifies that LDA Absolute,X loads correctly.
    /// </summary>
    [Test]
    public void LDA_AbsoluteX_LoadsFromMemory()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x2050, 0xAA); // Value at $2050
        Write(0x1000, 0xA2); // LDX #$50
        Write(0x1001, 0x50);
        Write(0x1002, 0xBD); // LDA $2000,X
        WriteWord(0x1003, 0x2000);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDX #$50
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // LDA $2000,X

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0xAA));
        Assert.That(cycles, Is.EqualTo(4)); // 4 cycles (no page cross)
    }

    /// <summary>
    /// Verifies that LDA Absolute,X adds cycle on page boundary crossing.
    /// </summary>
    [Test]
    public void LDA_AbsoluteX_PageCrossAddsCycle()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x2100, 0xBB); // Value at $2100 (crosses page from $20FF)
        Write(0x1000, 0xA2); // LDX #$01
        Write(0x1001, 0x01);
        Write(0x1002, 0xBD); // LDA $20FF,X
        WriteWord(0x1003, 0x20FF);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDA #$01
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // LDA $20FF,X

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0xBB));
        Assert.That(cycles, Is.EqualTo(5)); // 5 cycles (page cross)
    }

    /// <summary>
    /// Verifies that LDA Absolute,Y loads correctly.
    /// </summary>
    [Test]
    public void LDA_AbsoluteY_LoadsFromMemory()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x3030, 0xCC); // Value at $3030
        Write(0x1000, 0xA0); // LDY #$30 (set Y)
        Write(0x1001, 0x30);
        Write(0x1002, 0xB9); // LDA $3000,Y
        WriteWord(0x1003, 0x3000);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDY #$30
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // LDA $3000,Y

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0xCC));
        Assert.That(cycles, Is.EqualTo(4)); // 4 cycles (no page cross)
    }

    /// <summary>
    /// Verifies that LDA (Indirect,X) loads correctly.
    /// </summary>
    [Test]
    public void LDA_IndirectX_LoadsFromMemory()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0x24, 0x4000); // Pointer at ZP $24
        Write(0x4000, 0xDD); // Value at $4000
        Write(0x1000, 0xA2); // LDX #$04
        Write(0x1001, 0x04);
        Write(0x1002, 0xA1); // LDA ($20,X) -> ($20+$04=$24)
        Write(0x1003, 0x20);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDX #$04
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // LDA ($20,X)

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0xDD));
        Assert.That(cycles, Is.EqualTo(6)); // 6 cycles for (Indirect,X)
    }

    /// <summary>
    /// Verifies that LDA (Indirect),Y loads correctly.
    /// </summary>
    [Test]
    public void LDA_IndirectY_LoadsFromMemory()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0x30, 0x5000); // Pointer at ZP $30
        Write(0x5010, 0xEE); // Value at $5010
        Write(0x1000, 0xA0); // LDY #$10 (set Y)
        Write(0x1001, 0x10);
        Write(0x1002, 0xB1); // LDA ($30),Y -> ($5000+$10=$5010)
        Write(0x1003, 0x30);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDY #$10
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // LDA ($30),Y

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0xEE));
        Assert.That(cycles, Is.EqualTo(5)); // 5 cycles (no page cross)
    }

    /// <summary>
    /// Verifies that LDA (Indirect),Y adds cycle on page boundary crossing.
    /// </summary>
    [Test]
    public void LDA_IndirectY_PageCrossAddsCycle()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0x40, 0x60FF); // Pointer at ZP $40
        Write(0x6100, 0xFF); // Value at $6100 (crosses page from $60FF)
        Write(0x1000, 0xA0); // LDY #$01 (set Y)
        Write(0x1001, 0x01);
        Write(0x1002, 0xB1); // LDA ($40),Y -> ($60FF+$01=$6100)
        Write(0x1003, 0x40);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDY #$01
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // LDA ($40),Y

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0xFF));
        Assert.That(cycles, Is.EqualTo(6)); // 6 cycles (page cross)
    }

    /// <summary>
    /// Verifies that STA Zero Page,X stores correctly.
    /// </summary>
    [Test]
    public void STA_ZeroPageX_StoresAccumulator()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$77
        Write(0x1001, 0x77);
        Write(0x1002, 0xA2); // LDX #$05
        Write(0x1003, 0x05);
        Write(0x1004, 0x95); // STA $10,X ($10+$05=$15)
        Write(0x1005, 0x10);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDA #$77
        Cpu.Step(); // LDX #$05
        Cpu.Step(); // STA $10,X

        // Assert
        Assert.That(Read(0x15), Is.EqualTo(0x77));
    }

    /// <summary>
    /// Verifies that STA Absolute stores correctly.
    /// </summary>
    [Test]
    public void STA_Absolute_StoresAccumulator()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$88
        Write(0x1001, 0x88);
        Write(0x1002, 0x8D); // STA $3000
        WriteWord(0x1003, 0x3000);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDA #$88
        Cpu.Step(); // STA $3000

        // Assert
        Assert.That(Read(0x3000), Is.EqualTo(0x88));
    }

    /// <summary>
    /// Verifies that STA (Indirect,X) stores correctly.
    /// </summary>
    [Test]
    public void STA_IndirectX_StoresAccumulator()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0x25, 0x7000); // Pointer at ZP $25
        Write(0x1000, 0xA9); // LDA #$99
        Write(0x1001, 0x99);
        Write(0x1002, 0xA2); // LDX #$05
        Write(0x1003, 0x05);
        Write(0x1004, 0x81); // STA ($20,X) -> ($20+$05=$25)
        Write(0x1005, 0x20);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDA #$99
        Cpu.Step(); // LDX #$05
        Cpu.Step(); // STA ($20,X)

        // Assert
        Assert.That(Read(0x7000), Is.EqualTo(0x99));
    }

    /// <summary>
    /// Verifies that STA (Indirect),Y stores correctly.
    /// </summary>
    [Test]
    public void STA_IndirectY_StoresAccumulator()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0x50, 0x8000); // Pointer at ZP $50
        Write(0x1000, 0xA9); // LDA #$AB
        Write(0x1001, 0xAB);
        Write(0x1002, 0xA0); // LDY #$20
        Write(0x1003, 0x20);
        Write(0x1004, 0x91); // STA ($50),Y -> ($8000+$20=$8020)
        Write(0x1005, 0x50);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDA #$AB
        Cpu.Step(); // LDY #$20
        Cpu.Step(); // STA ($50),Y

        // Assert
        Assert.That(Read(0x8020), Is.EqualTo(0xAB));
    }

    /// <summary>
    /// Verifies that STA Absolute,X stores the accumulator correctly.
    /// </summary>
    [Test]
    public void STA_AbsoluteX_StoresAccumulator()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$CD
        Write(0x1001, 0xCD);
        Write(0x1002, 0xA2); // LDX #$10
        Write(0x1003, 0x10);
        Write(0x1004, 0x9D); // STA $2000,X -> $2010
        Write(0x1005, 0x00);
        Write(0x1006, 0x20);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDA #$CD
        Cpu.Step(); // LDX #$10
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // STA $2000,X

        // Assert
        Assert.That(Read(0x2010), Is.EqualTo(0xCD));
        Assert.That(cycles, Is.EqualTo(5)); // 5 cycles for STA abs,X
    }

    /// <summary>
    /// Verifies that STA Absolute,Y stores the accumulator correctly.
    /// </summary>
    [Test]
    public void STA_AbsoluteY_StoresAccumulator()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$EF
        Write(0x1001, 0xEF);
        Write(0x1002, 0xA0); // LDY #$08
        Write(0x1003, 0x08);
        Write(0x1004, 0x99); // STA $3000,Y -> $3008
        Write(0x1005, 0x00);
        Write(0x1006, 0x30);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDA #$EF
        Cpu.Step(); // LDY #$08
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // STA $3000,Y

        // Assert
        Assert.That(Read(0x3008), Is.EqualTo(0xEF));
        Assert.That(cycles, Is.EqualTo(5)); // 5 cycles for STA abs,Y
    }

    /// <summary>
    /// Verifies that LDA Absolute,Y with page crossing adds an extra cycle.
    /// </summary>
    [Test]
    public void LDA_AbsoluteY_PageCrossAddsCycle()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x2100, 0x42); // Value at $2100 (crosses page from $20FF)
        Write(0x1000, 0xA0); // LDY #$01
        Write(0x1001, 0x01);
        Write(0x1002, 0xB9); // LDA $20FF,Y -> $2100 (page cross from $20 to $21)
        Write(0x1003, 0xFF);
        Write(0x1004, 0x20);
        Cpu.Reset();

        // Act
        Cpu.Step(); // LDY #$01
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // LDA $20FF,Y

        // Assert
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x42));
        Assert.That(cycles, Is.EqualTo(5)); // 4 base + 1 for page crossing
    }

    /// <summary>
    /// Verifies that IllegalOpcode halts the CPU.
    /// </summary>
    [Test]
    public void IllegalOpcode_HaltsCpu()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x02); // Illegal opcode (not implemented)
        Cpu.Reset();

        // Act
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value;

        // Assert
        Assert.That(Cpu.Halted, Is.True);
        Assert.That(cycles, Is.EqualTo(1)); // 1 cycle for fetching illegal opcode
    }

    /// <summary>
    /// Verifies that Step returns 0 when CPU is halted.
    /// </summary>
    [Test]
    public void Step_WhenHalted_ReturnsZero()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x02); // Illegal opcode
        Cpu.Reset();
        Cpu.Step(); // Execute illegal opcode to halt

        // Act
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value;

        // Assert
        Assert.That(Cpu.Halted, Is.True);
        Assert.That(cycles, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that Execute runs instructions until CPU halts.
    /// </summary>
    [Test]
    public void Execute_RunsUntilHalted()
    {
        // Arrange
        Write(0x1000, 0xA9); // LDA #$42
        Write(0x1001, 0x42);
        Write(0x1002, 0xA2); // LDX #$10
        Write(0x1003, 0x10);
        Write(0x1004, 0xEA); // NOP
        Write(0x1005, 0xDB); // STP (halts CPU)
        Cpu.Reset();

        // Act
        Cpu.Execute(0x1000);

        // Assert
        Assert.That(Cpu.Halted, Is.True);
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x42));
        Assert.That(Cpu.Registers.X.GetByte(), Is.EqualTo(0x10));
    }

    /// <summary>
    /// Verifies that Execute accepts uint parameter without sign issues.
    /// </summary>
    [Test]
    public void Execute_AcceptsUintParameter()
    {
        // Arrange
        uint startAddress = 0x8000; // HighByte address that would be negative as int16
        Write(0x8000, 0xA9); // LDA #$55
        Write(0x8001, 0x55);
        Write(0x8002, 0xDB); // STP (halts CPU)
        Cpu.Reset();

        // Act
        Cpu.Execute(startAddress);

        // Assert
        Assert.That(Cpu.Halted, Is.True);
        Assert.That(Cpu.Registers.A.GetByte(), Is.EqualTo(0x55));
    }
}