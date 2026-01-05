// <copyright file="Cpu65C02GenericBuilderIntegrationTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Systems;

using Emulation.Cpu;

/// <summary>
/// Integration tests demonstrating the builder pattern integrated into CPU opcode table construction.
/// </summary>
/// <remarks>
/// <para>
/// These tests exercise the complete CPU instruction execution path by writing real
/// 65C02 machine language programs and executing them via the CPU emulator's Step method.
/// </para>
/// <para>
/// Unlike unit tests that call opcode handlers directly, these integration tests
/// validate the full execution path including:
/// </para>
/// <list type="bullet">
/// <item><description>Opcode fetch from memory</description></item>
/// <item><description>Operand fetch based on addressing mode</description></item>
/// <item><description>Instruction execution</description></item>
/// <item><description>Register and memory state updates</description></item>
/// <item><description>Cycle counting</description></item>
/// </list>
/// </remarks>
[TestFixture]
public class Cpu65C02GenericBuilderIntegrationTests
{
    /// <summary>
    /// Start address for test ML programs in low memory.
    /// </summary>
    private const ushort TestProgramAddress = 0x0300;

    /// <summary>
    /// Demonstrates that the builder produces a working opcode table
    /// by executing actual ML code that uses the built handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test program verifies opcode table construction by:
    /// </para>
    /// <list type="number">
    /// <item><description>LDA #$A9 - Loads $A9 (the LDA immediate opcode) into A</description></item>
    /// <item><description>LDX #$85 - Loads $85 (the STA zero page opcode) into X</description></item>
    /// <item><description>STP - Halts the CPU</description></item>
    /// </list>
    /// <para>
    /// If the opcode table is correctly built, A will contain $A9 and X will contain $85.
    /// </para>
    /// </remarks>
    [Test]
    public void Builder_ProducesWorkingOpcodeTable()
    {
        // ─── Arrange: Build machine and write ML program ────────────────────────
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // The test program at $0300:
        // $0300: LDA #$A9     ; Load $A9 (LDA immediate opcode) into A
        // $0302: LDX #$85     ; Load $85 (STA zero page opcode) into X
        // $0304: STP          ; Halt the CPU
        ushort addr = TestProgramAddress;

        machine.Cpu.Write8(addr++, 0xA9);     // $0300: LDA immediate
        machine.Cpu.Write8(addr++, 0xA9);     // $0301: Operand ($A9)
        machine.Cpu.Write8(addr++, 0xA2);     // $0302: LDX immediate
        machine.Cpu.Write8(addr++, 0x85);     // $0303: Operand ($85)
        machine.Cpu.Write8(addr, 0xDB);       // $0304: STP

        // ─── Act: Execute the ML program ────────────────────────────────────────
        machine.Cpu.SetPC(TestProgramAddress);

        // Step 1: Execute LDA #$A9
        machine.Step();
        Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0302), "PC should advance past LDA #$A9");

        // Step 2: Execute LDX #$85
        machine.Step();
        Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0304), "PC should advance past LDX #$85");

        // Step 3: Execute STP
        machine.Step();

        // ─── Assert: Opcodes executed correctly ─────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu.Registers.A.GetByte(), Is.EqualTo(0xA9),
                "A should contain LDA immediate opcode value");
            Assert.That(machine.Cpu.Registers.X.GetByte(), Is.EqualTo(0x85),
                "X should contain STA zero page opcode value");
            Assert.That(machine.Cpu.Halted, Is.True, "CPU should be halted after STP");
        });
    }

    /// <summary>
    /// Demonstrates that opcodes built with compositional pattern execute correctly
    /// by running an actual ML program through the CPU emulator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test program at $0300:
    /// </para>
    /// <list type="number">
    /// <item><description>LDA #$42 - Loads $42 into A using immediate addressing</description></item>
    /// <item><description>STP - Halts the CPU</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void Builder_Opcodes_ExecuteCorrectly()
    {
        // ─── Arrange: Build machine and write ML program ────────────────────────
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // The test program at $0300:
        // $0300: LDA #$42     ; Load $42 into A
        // $0302: STP          ; Halt the CPU
        ushort addr = TestProgramAddress;

        machine.Cpu.Write8(addr++, 0xA9);     // $0300: LDA immediate (opcode $A9)
        machine.Cpu.Write8(addr++, 0x42);     // $0301: Operand ($42)
        machine.Cpu.Write8(addr, 0xDB);       // $0302: STP

        var initialCycles = machine.Cpu.GetCycles();

        // ─── Act: Execute the ML program ────────────────────────────────────────
        machine.Cpu.SetPC(TestProgramAddress);

        // Step 1: Execute LDA #$42
        machine.Step();

        // ─── Assert: LDA executed correctly ─────────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu.Registers.A.GetByte(), Is.EqualTo(0x42),
                "LDA should load the immediate value into A");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0302),
                "PC should advance to next instruction");
            Assert.That(machine.Cpu.GetCycles(), Is.GreaterThan(initialCycles),
                "Cycles should advance during execution");
        });

        // Step 2: Execute STP
        machine.Step();
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should be halted after STP");
    }

    /// <summary>
    /// Verifies LDA instruction works across multiple addressing modes
    /// by executing ML code that uses zero page and absolute addressing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test program at $0300:
    /// </para>
    /// <list type="number">
    /// <item><description>LDA $50 - Loads from zero page address $50 into A</description></item>
    /// <item><description>LDX $0400 - Loads from absolute address $0400 into X</description></item>
    /// <item><description>STP - Halts the CPU</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void Builder_LDA_WorksAcrossAddressingModes()
    {
        // ─── Arrange: Build machine and write ML program ────────────────────────
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // Pre-populate memory locations with test values
        machine.Cpu.Write8(0x0050, 0x99);    // ZP location $50 = $99
        machine.Cpu.Write8(0x0400, 0x77);    // Absolute location $0400 = $77

        // The test program at $0300:
        // $0300: LDA $50      ; Load from ZP address $50
        // $0302: LDX $0400    ; Load from absolute address $0400
        // $0305: STP          ; Halt the CPU
        ushort addr = TestProgramAddress;

        machine.Cpu.Write8(addr++, 0xA5);     // $0300: LDA zero page (opcode $A5)
        machine.Cpu.Write8(addr++, 0x50);     // $0301: ZP address ($50)
        machine.Cpu.Write8(addr++, 0xAE);     // $0302: LDX absolute (opcode $AE)
        machine.Cpu.Write8(addr++, 0x00);     // $0303: Low byte of $0400
        machine.Cpu.Write8(addr++, 0x04);     // $0304: High byte of $0400
        machine.Cpu.Write8(addr, 0xDB);       // $0305: STP

        // ─── Act: Execute the ML program ────────────────────────────────────────
        machine.Cpu.SetPC(TestProgramAddress);

        // Step 1: Execute LDA $50 (zero page)
        machine.Step();
        Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0302), "PC should be at LDX instruction");

        // Step 2: Execute LDX $0400 (absolute)
        machine.Step();
        Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0305), "PC should be at STP instruction");

        // ─── Assert: Both addressing modes worked correctly ─────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu.Registers.A.GetByte(), Is.EqualTo(0x99),
                "LDA ZP should load from zero page address $50");
            Assert.That(machine.Cpu.Registers.X.GetByte(), Is.EqualTo(0x77),
                "LDX absolute should load from address $0400");
        });

        // Step 3: Execute STP
        machine.Step();
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should be halted after STP");
    }

    /// <summary>
    /// Verifies STA instruction stores values correctly by executing ML code
    /// that stores a value and then verifying the memory location.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test program at $0300:
    /// </para>
    /// <list type="number">
    /// <item><description>LDA #$42 - Loads $42 into A</description></item>
    /// <item><description>STA $50 - Stores A to zero page address $50</description></item>
    /// <item><description>STP - Halts the CPU</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void Builder_STA_StoresValuesCorrectly()
    {
        // ─── Arrange: Build machine and write ML program ────────────────────────
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // The test program at $0300:
        // $0300: LDA #$42     ; Load $42 into A
        // $0302: STA $50      ; Store A to ZP address $50
        // $0304: STP          ; Halt the CPU
        ushort addr = TestProgramAddress;

        machine.Cpu.Write8(addr++, 0xA9);     // $0300: LDA immediate (opcode $A9)
        machine.Cpu.Write8(addr++, 0x42);     // $0301: Operand ($42)
        machine.Cpu.Write8(addr++, 0x85);     // $0302: STA zero page (opcode $85)
        machine.Cpu.Write8(addr++, 0x50);     // $0303: ZP address ($50)
        machine.Cpu.Write8(addr, 0xDB);       // $0304: STP

        // Verify initial value at $50 is not $42
        Assert.That(machine.Cpu.Read8(0x0050), Is.Not.EqualTo(0x42),
            "Initial value at $50 should not be $42");

        // ─── Act: Execute the ML program ────────────────────────────────────────
        machine.Cpu.SetPC(TestProgramAddress);

        // Step 1: Execute LDA #$42
        machine.Step();
        Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0302), "PC should be at STA instruction");

        // Step 2: Execute STA $50
        machine.Step();
        Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0304), "PC should be at STP instruction");

        // ─── Assert: STA stored the value correctly ─────────────────────────────
        Assert.That(machine.Cpu.Read8(0x0050), Is.EqualTo(0x42),
            "STA should store accumulator value to zero page address $50");

        // Step 3: Execute STP
        machine.Step();
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should be halted after STP");
    }

    /// <summary>
    /// Demonstrates the clean syntax of the compositional pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test exists primarily for documentation purposes.
    /// It shows how the compositional pattern provides clean, readable code.
    /// Instructions compose cleanly with addressing modes without combinatorial explosion.
    /// </para>
    /// </remarks>
    [Test]
    public void Builder_DemonstratesCleanSyntax()
    {
        // Instructions compose cleanly with addressing modes:
        var handler1 = Instructions.LDA(AddressingModes.Immediate);
        var handler2 = Instructions.STA(AddressingModes.ZeroPage);

        // The pattern allows easy extension without combinatorial explosion
        Assert.That(handler1, Is.Not.Null);
        Assert.That(handler2, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that multiple instructions work together by executing
    /// a complete ML program that loads a value and stores it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test program at $0300:
    /// </para>
    /// <list type="number">
    /// <item><description>LDA #$42 - Loads $42 into A</description></item>
    /// <item><description>STA $50 - Stores A to zero page address $50</description></item>
    /// <item><description>LDA #$00 - Clears A</description></item>
    /// <item><description>LDA $50 - Loads from $50 back into A</description></item>
    /// <item><description>STP - Halts the CPU</description></item>
    /// </list>
    /// <para>
    /// This verifies the complete load-store-load round-trip works correctly.
    /// </para>
    /// </remarks>
    [Test]
    public void Builder_MultipleInstructions_WorkTogether()
    {
        // ─── Arrange: Build machine and write ML program ────────────────────────
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // The test program at $0300:
        // $0300: LDA #$42     ; Load $42 into A
        // $0302: STA $50      ; Store A to ZP address $50
        // $0304: LDA #$00     ; Clear A
        // $0306: LDA $50      ; Load from $50 back into A
        // $0308: STP          ; Halt the CPU
        ushort addr = TestProgramAddress;

        machine.Cpu.Write8(addr++, 0xA9);     // $0300: LDA immediate
        machine.Cpu.Write8(addr++, 0x42);     // $0301: Operand ($42)
        machine.Cpu.Write8(addr++, 0x85);     // $0302: STA zero page
        machine.Cpu.Write8(addr++, 0x50);     // $0303: ZP address ($50)
        machine.Cpu.Write8(addr++, 0xA9);     // $0304: LDA immediate
        machine.Cpu.Write8(addr++, 0x00);     // $0305: Operand ($00)
        machine.Cpu.Write8(addr++, 0xA5);     // $0306: LDA zero page
        machine.Cpu.Write8(addr++, 0x50);     // $0307: ZP address ($50)
        machine.Cpu.Write8(addr, 0xDB);       // $0308: STP

        // ─── Act: Execute the ML program ────────────────────────────────────────
        machine.Cpu.SetPC(TestProgramAddress);

        // Step 1: Execute LDA #$42
        machine.Step();
        Assert.That(machine.Cpu.Registers.A.GetByte(), Is.EqualTo(0x42),
            "A should be $42 after first LDA");

        // Step 2: Execute STA $50
        machine.Step();
        Assert.That(machine.Cpu.Read8(0x0050), Is.EqualTo(0x42),
            "Memory at $50 should be $42 after STA");

        // Step 3: Execute LDA #$00
        machine.Step();
        Assert.That(machine.Cpu.Registers.A.GetByte(), Is.EqualTo(0x00),
            "A should be $00 after clearing");

        // Step 4: Execute LDA $50
        machine.Step();

        // ─── Assert: Complete round-trip worked ─────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu.Registers.A.GetByte(), Is.EqualTo(0x42),
                "A should contain the stored value from $50");
            Assert.That(machine.Cpu.Read8(0x0050), Is.EqualTo(0x42),
                "Memory at $50 should still contain $42");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0308),
                "PC should be at STP instruction");
        });

        // Step 5: Execute STP
        machine.Step();
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should be halted after STP");
    }
}