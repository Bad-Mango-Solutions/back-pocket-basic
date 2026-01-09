// <copyright file="AddressParserTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Debug.Infrastructure.Commands;

using Moq;

/// <summary>
/// Unit tests for <see cref="AddressParser"/>.
/// </summary>
[TestFixture]
public class AddressParserTests
{
    /// <summary>
    /// Verifies that dollar-prefixed hex addresses are parsed correctly.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="expected">The expected parsed address.</param>
    [Test]
    [TestCase("$0000", 0x0000u)]
    [TestCase("$1234", 0x1234u)]
    [TestCase("$C000", 0xC000u)]
    [TestCase("$FFFF", 0xFFFFu)]
    [TestCase("$c030", 0xC030u)]
    public void TryParse_DollarPrefixedHex_ReturnsCorrectAddress(string input, uint expected)
    {
        var result = AddressParser.TryParse(input, out uint address);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Should parse successfully");
            Assert.That(address, Is.EqualTo(expected), $"Expected {expected:X4}");
        });
    }

    /// <summary>
    /// Verifies that 0x-prefixed hex addresses are parsed correctly.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="expected">The expected parsed address.</param>
    [Test]
    [TestCase("0x0000", 0x0000u)]
    [TestCase("0x1234", 0x1234u)]
    [TestCase("0xC000", 0xC000u)]
    [TestCase("0xFFFF", 0xFFFFu)]
    [TestCase("0xc030", 0xC030u)]
    public void TryParse_ZeroXPrefixedHex_ReturnsCorrectAddress(string input, uint expected)
    {
        var result = AddressParser.TryParse(input, out uint address);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Should parse successfully");
            Assert.That(address, Is.EqualTo(expected), $"Expected {expected:X4}");
        });
    }

    /// <summary>
    /// Verifies that decimal addresses are parsed correctly.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="expected">The expected parsed address.</param>
    [Test]
    [TestCase("0", 0u)]
    [TestCase("768", 768u)]
    [TestCase("49152", 49152u)]
    [TestCase("65535", 65535u)]
    public void TryParse_Decimal_ReturnsCorrectAddress(string input, uint expected)
    {
        var result = AddressParser.TryParse(input, out uint address);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Should parse successfully");
            Assert.That(address, Is.EqualTo(expected), $"Expected {expected}");
        });
    }

    /// <summary>
    /// Verifies that soft switch names are resolved from ISoftSwitchProvider.
    /// </summary>
    [Test]
    public void TryParse_SoftSwitchName_ResolvesFromProvider()
    {
        // Arrange
        var mockMachine = new Mock<IMachine>();
        var mockProvider = new Mock<ISoftSwitchProvider>();

        mockProvider.Setup(p => p.GetSoftSwitchStates())
            .Returns(new List<SoftSwitchState>
            {
                new("SPEAKER", 0xC030, false, "Speaker toggle"),
                new("KBD", 0xC000, false, "Keyboard data"),
            });

        mockMachine.Setup(m => m.GetComponents<ISoftSwitchProvider>())
            .Returns(new[] { mockProvider.Object });

        // Act
        var result = AddressParser.TryParse("SPEAKER", mockMachine.Object, out uint address);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Should parse SPEAKER successfully");
            Assert.That(address, Is.EqualTo(0xC030u), "Should resolve to $C030");
        });
    }

    /// <summary>
    /// Verifies that soft switch name lookup is case-insensitive.
    /// </summary>
    /// <param name="input">The input string to parse (various casings of SPEAKER).</param>
    [Test]
    [TestCase("SPEAKER")]
    [TestCase("Speaker")]
    [TestCase("speaker")]
    [TestCase("SpEaKeR")]
    public void TryParse_SoftSwitchName_IsCaseInsensitive(string input)
    {
        // Arrange
        var mockMachine = new Mock<IMachine>();
        var mockProvider = new Mock<ISoftSwitchProvider>();

        mockProvider.Setup(p => p.GetSoftSwitchStates())
            .Returns(new List<SoftSwitchState>
            {
                new("SPEAKER", 0xC030, false, "Speaker toggle"),
            });

        mockMachine.Setup(m => m.GetComponents<ISoftSwitchProvider>())
            .Returns(new[] { mockProvider.Object });

        // Act
        var result = AddressParser.TryParse(input, mockMachine.Object, out uint address);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, $"Should parse '{input}' successfully");
            Assert.That(address, Is.EqualTo(0xC030u));
        });
    }

    /// <summary>
    /// Verifies that unknown soft switch names fall through to decimal parsing.
    /// </summary>
    [Test]
    public void TryParse_UnknownSwitchName_FallsThroughToDecimal()
    {
        // Arrange
        var mockMachine = new Mock<IMachine>();
        mockMachine.Setup(m => m.GetComponents<ISoftSwitchProvider>())
            .Returns(Array.Empty<ISoftSwitchProvider>());

        // Act - "UNKNOWN" is not a valid decimal, so it should fail
        var result = AddressParser.TryParse("UNKNOWN", mockMachine.Object, out _);

        // Assert
        Assert.That(result, Is.False, "Should fail to parse unknown name");
    }

    /// <summary>
    /// Verifies that invalid inputs return false.
    /// </summary>
    /// <param name="input">The invalid input string to parse.</param>
    [Test]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("invalid")]
    [TestCase("$GGGG")]
    [TestCase("0xGGGG")]
    public void TryParse_InvalidInput_ReturnsFalse(string input)
    {
        var result = AddressParser.TryParse(input, out _);

        Assert.That(result, Is.False, $"Should fail to parse '{input}'");
    }

    /// <summary>
    /// Verifies that null input returns false.
    /// </summary>
    [Test]
    public void TryParse_NullInput_ReturnsFalse()
    {
        var result = AddressParser.TryParse(null!, out _);

        Assert.That(result, Is.False, "Should fail to parse null");
    }

    /// <summary>
    /// Verifies that count parsing works with various formats.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="expected">The expected parsed count.</param>
    [Test]
    [TestCase("16", 16)]
    [TestCase("$10", 16)]
    [TestCase("0x10", 16)]
    [TestCase("256", 256)]
    public void TryParseCount_ValidInput_ReturnsCorrectCount(string input, int expected)
    {
        var result = AddressParser.TryParseCount(input, out int count);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Should parse successfully");
            Assert.That(count, Is.EqualTo(expected));
        });
    }

    /// <summary>
    /// Verifies that GetFormatDescription returns a non-empty string.
    /// </summary>
    [Test]
    public void GetFormatDescription_ReturnsNonEmptyString()
    {
        var description = AddressParser.GetFormatDescription();

        Assert.Multiple(() =>
        {
            Assert.That(description, Is.Not.Null);
            Assert.That(description, Is.Not.Empty);
            Assert.That(description, Does.Contain("hex"));
            Assert.That(description, Does.Contain("soft switch"));
        });
    }

    /// <summary>
    /// Verifies that soft switch lookup works with multiple providers.
    /// </summary>
    [Test]
    public void TryParse_MultipleProviders_SearchesAll()
    {
        // Arrange
        var mockMachine = new Mock<IMachine>();
        var mockProvider1 = new Mock<ISoftSwitchProvider>();
        var mockProvider2 = new Mock<ISoftSwitchProvider>();

        mockProvider1.Setup(p => p.GetSoftSwitchStates())
            .Returns(new List<SoftSwitchState>
            {
                new("SWITCH1", 0xC001, false),
            });

        mockProvider2.Setup(p => p.GetSoftSwitchStates())
            .Returns(new List<SoftSwitchState>
            {
                new("SWITCH2", 0xC002, false),
            });

        mockMachine.Setup(m => m.GetComponents<ISoftSwitchProvider>())
            .Returns(new[] { mockProvider1.Object, mockProvider2.Object });

        // Act
        var result1 = AddressParser.TryParse("SWITCH1", mockMachine.Object, out uint address1);
        var result2 = AddressParser.TryParse("SWITCH2", mockMachine.Object, out uint address2);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.True);
            Assert.That(address1, Is.EqualTo(0xC001u));
            Assert.That(result2, Is.True);
            Assert.That(address2, Is.EqualTo(0xC002u));
        });
    }

    /// <summary>
    /// Verifies that hex addresses take precedence over switch names.
    /// </summary>
    [Test]
    public void TryParse_HexPrefixTakesPrecedence_OverSwitchName()
    {
        // Arrange - even if "SPEAKER" existed, "$SPEAKER" would fail as invalid hex
        var mockMachine = new Mock<IMachine>();
        var mockProvider = new Mock<ISoftSwitchProvider>();

        mockProvider.Setup(p => p.GetSoftSwitchStates())
            .Returns(new List<SoftSwitchState>
            {
                new("SPEAKER", 0xC030, false),
            });

        mockMachine.Setup(m => m.GetComponents<ISoftSwitchProvider>())
            .Returns(new[] { mockProvider.Object });

        // Act - "$C030" should parse as hex, not look up "C030" as a switch name
        var result = AddressParser.TryParse("$C030", mockMachine.Object, out uint address);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(address, Is.EqualTo(0xC030u));
        });
    }

    /// <summary>
    /// Verifies that parsing without a machine still works for hex and decimal.
    /// </summary>
    [Test]
    public void TryParse_WithoutMachine_ParsesHexAndDecimal()
    {
        // Act
        var hexResult = AddressParser.TryParse("$C030", null, out uint hexAddress);
        var decimalResult = AddressParser.TryParse("49200", null, out uint decimalAddress);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(hexResult, Is.True);
            Assert.That(hexAddress, Is.EqualTo(0xC030u));
            Assert.That(decimalResult, Is.True);
            Assert.That(decimalAddress, Is.EqualTo(49200u));
        });
    }
}