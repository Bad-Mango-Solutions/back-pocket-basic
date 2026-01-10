// <copyright file="BootRomTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

/// <summary>
/// Unit tests for <see cref="BootRom"/>.
/// </summary>
[TestFixture]
public class BootRomTests
{
    /// <summary>
    /// Tears down each test by clearing the cached ROM.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        BootRom.ClearCache();
    }

    /// <summary>
    /// Tests that GetRomData returns a byte array.
    /// </summary>
    [Test]
    public void GetRomData_ReturnsNonNullArray()
    {
        // Act
        var rom = BootRom.GetRomData();

        // Assert
        Assert.That(rom, Is.Not.Null);
    }

    /// <summary>
    /// Tests that GetRomData returns correct size.
    /// </summary>
    [Test]
    public void GetRomData_ReturnsCorrectSize()
    {
        // Act
        var rom = BootRom.GetRomData();

        // Assert - should be 2KB (2048 bytes)
        Assert.That(rom.Length, Is.EqualTo(BootRom.Size));
        Assert.That(rom.Length, Is.EqualTo(0x0800));
    }

    /// <summary>
    /// Tests that LoadAddress constant is correct.
    /// </summary>
    [Test]
    public void LoadAddress_IsCorrect()
    {
        // Assert - boot ROM loads at $F800
        Assert.That(BootRom.LoadAddress, Is.EqualTo(0xF800));
    }

    /// <summary>
    /// Tests that Size constant is correct.
    /// </summary>
    [Test]
    public void Size_IsCorrect()
    {
        // Assert - boot ROM is 2KB
        Assert.That(BootRom.Size, Is.EqualTo(2048));
    }

    /// <summary>
    /// Tests that RESET vector points to correct address.
    /// </summary>
    [Test]
    public void ResetVector_PointsToResetHandler()
    {
        // Arrange
        var rom = BootRom.GetRomData();

        // RESET vector is at $FFFC-$FFFD (offset 0x07FC-0x07FD in ROM)
        int lowByte = rom[0x07FC];
        int highByte = rom[0x07FD];
        int resetAddress = (highByte << 8) | lowByte;

        // Assert - should point to $F800 (start of boot ROM)
        Assert.That(resetAddress, Is.EqualTo(0xF800));
    }

    /// <summary>
    /// Tests that NMI vector is set correctly.
    /// </summary>
    [Test]
    public void NmiVector_PointsToInterruptHandler()
    {
        // Arrange
        var rom = BootRom.GetRomData();

        // NMI vector is at $FFFA-$FFFB (offset 0x07FA-0x07FB in ROM)
        int lowByte = rom[0x07FA];
        int highByte = rom[0x07FB];
        int nmiAddress = (highByte << 8) | lowByte;

        // Assert - should point to interrupt handler at $F810
        Assert.That(nmiAddress, Is.EqualTo(0xF810));
    }

    /// <summary>
    /// Tests that IRQ vector is set correctly.
    /// </summary>
    [Test]
    public void IrqVector_PointsToInterruptHandler()
    {
        // Arrange
        var rom = BootRom.GetRomData();

        // IRQ vector is at $FFFE-$FFFF (offset 0x07FE-0x07FF in ROM)
        int lowByte = rom[0x07FE];
        int highByte = rom[0x07FF];
        int irqAddress = (highByte << 8) | lowByte;

        // Assert - should point to interrupt handler at $F810
        Assert.That(irqAddress, Is.EqualTo(0xF810));
    }

    /// <summary>
    /// Tests that RESET handler starts with CLD.
    /// </summary>
    [Test]
    public void ResetHandler_StartsWithCLD()
    {
        // Arrange
        var rom = BootRom.GetRomData();

        // Assert - first byte should be CLD (0xD8)
        Assert.That(rom[0], Is.EqualTo(0xD8), "RESET handler should start with CLD");
    }

    /// <summary>
    /// Tests that RESET handler contains WAI instruction.
    /// </summary>
    [Test]
    public void ResetHandler_ContainsWAI()
    {
        // Arrange
        var rom = BootRom.GetRomData();

        // The WAI instruction (0xCB) should be at offset 6 after: CLD, SEI, LDX #$FF, TXS, CLI
        Assert.That(rom[6], Is.EqualTo(0xCB), "WAI instruction should be in RESET handler");
    }

    /// <summary>
    /// Tests that RESET handler contains JMP for idle loop.
    /// </summary>
    [Test]
    public void ResetHandler_ContainsJmpToWaiLoop()
    {
        // Arrange
        var rom = BootRom.GetRomData();

        // JMP instruction (0x4C) should be at offset 7
        Assert.That(rom[7], Is.EqualTo(0x4C), "JMP instruction should follow WAI");

        // JMP target should be back to WAI at $F806
        int targetLow = rom[8];
        int targetHigh = rom[9];
        int jmpTarget = (targetHigh << 8) | targetLow;
        Assert.That(jmpTarget, Is.EqualTo(0xF806), "JMP should loop back to WAI");
    }

    /// <summary>
    /// Tests that interrupt handler contains RTI.
    /// </summary>
    [Test]
    public void InterruptHandler_ContainsRTI()
    {
        // Arrange
        var rom = BootRom.GetRomData();

        // RTI instruction (0x40) should be at interrupt handler ($F810 - $F800 = offset 0x10)
        Assert.That(rom[0x10], Is.EqualTo(0x40), "Interrupt handler should contain RTI");
    }

    /// <summary>
    /// Tests that ROM is cached after first load.
    /// </summary>
    [Test]
    public void GetRomData_ReturnsCachedInstance()
    {
        // Act
        var rom1 = BootRom.GetRomData();
        var rom2 = BootRom.GetRomData();

        // Assert - should be same instance (cached)
        Assert.That(rom1, Is.SameAs(rom2));
    }

    /// <summary>
    /// Tests that ClearCache allows regeneration.
    /// </summary>
    [Test]
    public void ClearCache_AllowsRegeneration()
    {
        // Arrange
        var rom1 = BootRom.GetRomData();

        // Act
        BootRom.ClearCache();
        var rom2 = BootRom.GetRomData();

        // Assert - should be different instance (regenerated)
        Assert.That(rom1, Is.Not.SameAs(rom2));

        // But content should be the same
        Assert.That(rom1, Is.EqualTo(rom2));
    }

    /// <summary>
    /// Tests that RESET handler initializes stack pointer.
    /// </summary>
    [Test]
    public void ResetHandler_InitializesStackPointer()
    {
        // Arrange
        var rom = BootRom.GetRomData();

        // Expected sequence: CLD(D8), SEI(78), LDX #$FF(A2 FF), TXS(9A), CLI(58)
        Assert.Multiple(() =>
        {
            Assert.That(rom[0], Is.EqualTo(0xD8), "CLD");
            Assert.That(rom[1], Is.EqualTo(0x78), "SEI");
            Assert.That(rom[2], Is.EqualTo(0xA2), "LDX immediate");
            Assert.That(rom[3], Is.EqualTo(0xFF), "LDX operand #$FF");
            Assert.That(rom[4], Is.EqualTo(0x9A), "TXS");
            Assert.That(rom[5], Is.EqualTo(0x58), "CLI");
        });
    }

    /// <summary>
    /// Tests that unused ROM area is filled with NOP.
    /// </summary>
    [Test]
    public void UnusedRomArea_IsFilledWithNOP()
    {
        // Arrange
        var rom = BootRom.GetRomData();

        // Check that most of the ROM is filled with NOP (0xEA)
        // The code area is small, so most bytes should be NOP
        // Skip code at start (0x00-0x1F) and vectors at end (0x7FA-0x7FF)
        int nopCount = 0;
        for (int i = 0x20; i < 0x07FA; i++)
        {
            if (rom[i] == 0xEA)
            {
                nopCount++;
            }
        }

        // Should be mostly NOPs (at least 95% of the checked area)
        int checkedArea = 0x07FA - 0x20;
        Assert.That(nopCount, Is.GreaterThan(checkedArea * 0.95), "Unused ROM area should be filled with NOP");
    }
}