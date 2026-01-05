// <copyright file="CpuPeekPokeTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Systems;

/// <summary>
/// Unit tests for CPU Peek8 and Poke8 debug memory access methods.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that Peek8 reads memory without side effects and that
/// Poke8 can write to ROM by bypassing normal write protection.
/// </para>
/// </remarks>
[TestFixture]
public class CpuPeekPokeTests
{
    /// <summary>
    /// ROM address for testing (COUT at $FDED).
    /// </summary>
    private const ushort CoutAddress = 0xFDED;

    /// <summary>
    /// RAM address for testing.
    /// </summary>
    private const ushort RamAddress = 0x0300;

    /// <summary>
    /// Verifies that Poke8 can write to ROM addresses.
    /// </summary>
    /// <remarks>
    /// Normal Write8 calls to ROM are silently ignored (ROM behavior).
    /// Poke8 uses AccessIntent.DebugWrite to bypass write protection.
    /// </remarks>
    [Test]
    public void Poke8_WritesToRom_BypassingWriteProtection()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // Verify initial value (stub ROM is filled with NOP = $EA)
        var before = machine.Cpu.Peek8(CoutAddress);
        Assert.That(before, Is.EqualTo(0xEA), "Stub ROM should contain NOP initially");

        // Act - Write RTS ($60) to ROM using Poke8
        machine.Cpu.Poke8(CoutAddress, 0x60);

        // Assert - Value should be changed
        var after = machine.Cpu.Peek8(CoutAddress);
        Assert.That(after, Is.EqualTo(0x60), "Poke8 should write to ROM");
    }

    /// <summary>
    /// Verifies that normal Write8 does NOT write to ROM.
    /// </summary>
    /// <remarks>
    /// This test confirms that normal write operations to ROM are ignored,
    /// which is the expected behavior for real ROM hardware.
    /// </remarks>
    [Test]
    public void Write8_ToRom_IsIgnored()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // Verify initial value (stub ROM is filled with NOP = $EA)
        var before = machine.Cpu.Peek8(CoutAddress);
        Assert.That(before, Is.EqualTo(0xEA), "Stub ROM should contain NOP initially");

        // Act - Try to write to ROM using normal Write8
        machine.Cpu.Write8(CoutAddress, 0x60);

        // Assert - Value should NOT be changed (ROM ignores writes)
        var after = machine.Cpu.Peek8(CoutAddress);
        Assert.That(after, Is.EqualTo(0xEA), "Write8 to ROM should be ignored");
    }

    /// <summary>
    /// Verifies that Peek8 reads memory without triggering side effects.
    /// </summary>
    [Test]
    public void Peek8_ReadsWithoutSideEffects()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // Write a test value to RAM
        machine.Cpu.Write8(RamAddress, 0x42);

        // Act - Read using Peek8
        var value = machine.Cpu.Peek8(RamAddress);

        // Assert
        Assert.That(value, Is.EqualTo(0x42), "Peek8 should read the correct value");
    }

    /// <summary>
    /// Verifies that Poke8 writes to RAM correctly.
    /// </summary>
    [Test]
    public void Poke8_WritesToRam()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // Act - Write using Poke8
        machine.Cpu.Poke8(RamAddress, 0x99);

        // Assert - Both Peek8 and Read8 should see the value
        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu.Peek8(RamAddress), Is.EqualTo(0x99), "Peek8 should read poked value");
            Assert.That(machine.Cpu.Read8(RamAddress), Is.EqualTo(0x99), "Read8 should read poked value");
        });
    }

    /// <summary>
    /// Verifies that Poke8 can be used to patch ROM for test scenarios.
    /// </summary>
    /// <remarks>
    /// This integration test demonstrates the intended use case: patching
    /// ROM content to insert stub routines (like RTS) for testing.
    /// </remarks>
    [Test]
    public void Poke8_CanPatchRomForTestScenarios()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // Patch the ROM at COUT to have an RTS instruction
        machine.Cpu.Poke8(CoutAddress, 0x60); // RTS

        // Write a simple program: JSR $FDED, STP
        machine.Cpu.Write8(0x0300, 0x20);     // JSR
        machine.Cpu.Write8(0x0301, 0xED);     // Low byte of $FDED
        machine.Cpu.Write8(0x0302, 0xFD);     // High byte of $FDED
        machine.Cpu.Write8(0x0303, 0xDB);     // STP

        machine.Cpu.SetPC(0x0300);

        // Act - Execute JSR
        machine.Step(); // JSR - PC now at $FDED

        // Assert - PC should be at COUT
        Assert.That(machine.Cpu.GetPC(), Is.EqualTo(CoutAddress), "PC should be at COUT after JSR");

        // Act - Execute RTS (the patched instruction)
        machine.Step(); // RTS - returns to $0303

        // Assert - PC should be back after the JSR
        Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0303), "PC should return after RTS");

        // Execute STP to verify complete execution
        machine.Step();
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should be halted after STP");
    }
}