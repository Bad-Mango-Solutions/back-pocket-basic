// <copyright file="InstructionsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Emulation.Cpu;

/// <summary>
/// Unit tests for the <see cref="Instructions"/> class.
/// </summary>
[TestFixture]
public class InstructionsTests
{
    /// <summary>
    /// Verifies that LDA loads the accumulator and sets flags correctly.
    /// </summary>
    [Test]
    public void LDA_LoadsAccumulatorAndSetsFlags()
    {
        // Arrange
        byte a = 0;
        byte p = 0;

        // Act
        Instructions.LDA(0x42, ref a, ref p);

        // Assert
        Assert.That(a, Is.EqualTo(0x42));
        Assert.That(p & 0x02, Is.EqualTo(0)); // Zero flag clear
        Assert.That(p & 0x80, Is.EqualTo(0)); // Negative flag clear
    }

    /// <summary>
    /// Verifies that LDA sets the Zero flag when loading zero.
    /// </summary>
    [Test]
    public void LDA_SetsZeroFlag()
    {
        // Arrange
        byte a = 0xFF;
        byte p = 0;

        // Act
        Instructions.LDA(0x00, ref a, ref p);

        // Assert
        Assert.That(a, Is.EqualTo(0x00));
        Assert.That(p & 0x02, Is.EqualTo(0x02)); // Zero flag set
        Assert.That(p & 0x80, Is.EqualTo(0)); // Negative flag clear
    }

    /// <summary>
    /// Verifies that LDA sets the Negative flag when loading a negative value.
    /// </summary>
    [Test]
    public void LDA_SetsNegativeFlag()
    {
        // Arrange
        byte a = 0;
        byte p = 0;

        // Act
        Instructions.LDA(0xFF, ref a, ref p);

        // Assert
        Assert.That(a, Is.EqualTo(0xFF));
        Assert.That(p & 0x02, Is.EqualTo(0)); // Zero flag clear
        Assert.That(p & 0x80, Is.EqualTo(0x80)); // Negative flag set
    }

    /// <summary>
    /// Verifies that LDX loads the X register and sets flags correctly.
    /// </summary>
    [Test]
    public void LDX_LoadsXRegisterAndSetsFlags()
    {
        // Arrange
        byte x = 0;
        byte p = 0;

        // Act
        Instructions.LDX(0x55, ref x, ref p);

        // Assert
        Assert.That(x, Is.EqualTo(0x55));
        Assert.That(p & 0x02, Is.EqualTo(0)); // Zero flag clear
        Assert.That(p & 0x80, Is.EqualTo(0)); // Negative flag clear
    }

    /// <summary>
    /// Verifies that LDX sets the Zero flag when loading zero.
    /// </summary>
    [Test]
    public void LDX_SetsZeroFlag()
    {
        // Arrange
        byte x = 0xFF;
        byte p = 0;

        // Act
        Instructions.LDX(0x00, ref x, ref p);

        // Assert
        Assert.That(x, Is.EqualTo(0x00));
        Assert.That(p & 0x02, Is.EqualTo(0x02)); // Zero flag set
    }

    /// <summary>
    /// Verifies that LDY loads the Y register and sets flags correctly.
    /// </summary>
    [Test]
    public void LDY_LoadsYRegisterAndSetsFlags()
    {
        // Arrange
        byte y = 0;
        byte p = 0;

        // Act
        Instructions.LDY(0xAA, ref y, ref p);

        // Assert
        Assert.That(y, Is.EqualTo(0xAA));
        Assert.That(p & 0x02, Is.EqualTo(0)); // Zero flag clear
        Assert.That(p & 0x80, Is.EqualTo(0x80)); // Negative flag set
    }

    /// <summary>
    /// Verifies that LDY sets the Zero flag when loading zero.
    /// </summary>
    [Test]
    public void LDY_SetsZeroFlag()
    {
        // Arrange
        byte y = 0xFF;
        byte p = 0;

        // Act
        Instructions.LDY(0x00, ref y, ref p);

        // Assert
        Assert.That(y, Is.EqualTo(0x00));
        Assert.That(p & 0x02, Is.EqualTo(0x02)); // Zero flag set
    }

    /// <summary>
    /// Verifies that flags from previous operations are correctly cleared.
    /// </summary>
    [Test]
    public void LDA_ClearsPreviousFlags()
    {
        // Arrange
        byte a = 0;
        byte p = 0x82; // Z and N flags set

        // Act
        Instructions.LDA(0x01, ref a, ref p);

        // Assert
        Assert.That(a, Is.EqualTo(0x01));
        Assert.That(p & 0x02, Is.EqualTo(0)); // Zero flag cleared
        Assert.That(p & 0x80, Is.EqualTo(0)); // Negative flag cleared
    }
}