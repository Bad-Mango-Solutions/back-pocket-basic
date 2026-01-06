// <copyright file="HexParserTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Core.Configuration;

/// <summary>
/// Unit tests for the <see cref="HexParser"/> class.
/// </summary>
[TestFixture]
public class HexParserTests
{
    /// <summary>
    /// Verifies that ParseUInt32 correctly parses hex values with 0x prefix.
    /// </summary>
    /// <param name="input">The hex string to parse.</param>
    /// <param name="expected">The expected result.</param>
    [Test]
    [TestCase("0x0000", 0x0000u)]
    [TestCase("0x1000", 0x1000u)]
    [TestCase("0x10000", 0x10000u)]
    [TestCase("0xFFFF", 0xFFFFu)]
    [TestCase("0xffffffff", 0xFFFFFFFFu)]
    [TestCase("0xC000", 0xC000u)]
    public void ParseUInt32_WithPrefix_ReturnsCorrectValue(string input, uint expected)
    {
        var result = HexParser.ParseUInt32(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that ParseUInt32 correctly parses hex values without 0x prefix.
    /// </summary>
    /// <param name="input">The hex string to parse.</param>
    /// <param name="expected">The expected result.</param>
    [Test]
    [TestCase("0000", 0x0000u)]
    [TestCase("1000", 0x1000u)]
    [TestCase("FFFF", 0xFFFFu)]
    [TestCase("c000", 0xC000u)]
    public void ParseUInt32_WithoutPrefix_ReturnsCorrectValue(string input, uint expected)
    {
        var result = HexParser.ParseUInt32(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that ParseUInt32 handles whitespace correctly.
    /// </summary>
    [Test]
    public void ParseUInt32_WithWhitespace_ReturnsCorrectValue()
    {
        var result = HexParser.ParseUInt32("  0x1000  ");
        Assert.That(result, Is.EqualTo(0x1000u));
    }

    /// <summary>
    /// Verifies that ParseUInt32 throws on null input.
    /// </summary>
    [Test]
    public void ParseUInt32_NullInput_ThrowsArgumentException()
    {
        Assert.That(() => HexParser.ParseUInt32(null!), Throws.InstanceOf<ArgumentException>());
    }

    /// <summary>
    /// Verifies that ParseUInt32 throws on empty input.
    /// </summary>
    [Test]
    public void ParseUInt32_EmptyInput_ThrowsArgumentException()
    {
        Assert.That(() => HexParser.ParseUInt32(string.Empty), Throws.TypeOf<ArgumentException>());
    }

    /// <summary>
    /// Verifies that ParseUInt32 throws on invalid hex string.
    /// </summary>
    [Test]
    public void ParseUInt32_InvalidHex_ThrowsFormatException()
    {
        Assert.That(() => HexParser.ParseUInt32("0xGHIJ"), Throws.TypeOf<FormatException>());
    }

    /// <summary>
    /// Verifies that ParseByte correctly parses byte values.
    /// </summary>
    /// <param name="input">The hex string to parse.</param>
    /// <param name="expected">The expected byte value.</param>
    [Test]
    [TestCase("0x00", (byte)0x00)]
    [TestCase("0xFF", (byte)0xFF)]
    [TestCase("0xAA", (byte)0xAA)]
    [TestCase("FF", (byte)0xFF)]
    public void ParseByte_ValidInput_ReturnsCorrectValue(string input, byte expected)
    {
        var result = HexParser.ParseByte(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that ParseByte throws when value exceeds byte range.
    /// </summary>
    [Test]
    public void ParseByte_ValueExceedsByteRange_ThrowsFormatException()
    {
        Assert.That(() => HexParser.ParseByte("0x100"), Throws.TypeOf<FormatException>());
    }

    /// <summary>
    /// Verifies that TryParseUInt32 returns true for valid input.
    /// </summary>
    [Test]
    public void TryParseUInt32_ValidInput_ReturnsTrueAndCorrectValue()
    {
        bool success = HexParser.TryParseUInt32("0x1000", out uint result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(0x1000u));
    }

    /// <summary>
    /// Verifies that TryParseUInt32 returns false for invalid input.
    /// </summary>
    [Test]
    public void TryParseUInt32_InvalidInput_ReturnsFalse()
    {
        bool success = HexParser.TryParseUInt32("not-hex", out uint result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.EqualTo(0u));
    }

    /// <summary>
    /// Verifies that TryParseUInt32 returns false for null input.
    /// </summary>
    [Test]
    public void TryParseUInt32_NullInput_ReturnsFalse()
    {
        bool success = HexParser.TryParseUInt32(null, out uint result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.EqualTo(0u));
    }

    /// <summary>
    /// Verifies that TryParseByte returns true for valid input.
    /// </summary>
    [Test]
    public void TryParseByte_ValidInput_ReturnsTrueAndCorrectValue()
    {
        bool success = HexParser.TryParseByte("0xAA", out byte result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo((byte)0xAA));
    }

    /// <summary>
    /// Verifies that TryParseByte returns false when value exceeds byte range.
    /// </summary>
    [Test]
    public void TryParseByte_ValueExceedsByteRange_ReturnsFalse()
    {
        bool success = HexParser.TryParseByte("0x100", out byte result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.EqualTo((byte)0));
    }
}