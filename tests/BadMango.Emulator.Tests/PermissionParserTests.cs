// <copyright file="PermissionParserTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Bus;

/// <summary>
/// Unit tests for the <see cref="PermissionParser"/> class.
/// </summary>
[TestFixture]
public class PermissionParserTests
{
    /// <summary>
    /// Verifies that Parse returns All for null input.
    /// </summary>
    [Test]
    public void Parse_NullInput_ReturnsAll()
    {
        var result = PermissionParser.Parse(null);
        Assert.That(result, Is.EqualTo(PagePerms.All));
    }

    /// <summary>
    /// Verifies that Parse returns All for empty input.
    /// </summary>
    [Test]
    public void Parse_EmptyInput_ReturnsAll()
    {
        var result = PermissionParser.Parse(string.Empty);
        Assert.That(result, Is.EqualTo(PagePerms.All));
    }

    /// <summary>
    /// Verifies that Parse correctly parses permission strings.
    /// </summary>
    /// <param name="input">The permission string to parse.</param>
    /// <param name="expected">The expected PagePerms value.</param>
    [Test]
    [TestCase("rwx", PagePerms.All)]
    [TestCase("r", PagePerms.Read)]
    [TestCase("w", PagePerms.Write)]
    [TestCase("x", PagePerms.Execute)]
    [TestCase("rw", PagePerms.ReadWrite)]
    [TestCase("rx", PagePerms.ReadExecute)]
    [TestCase("wx", PagePerms.Write | PagePerms.Execute)]
    [TestCase("RWX", PagePerms.All)]
    [TestCase("r-x", PagePerms.ReadExecute)]
    [TestCase("---", PagePerms.None)]
    [TestCase("-w-", PagePerms.Write)]
    public void Parse_ValidInput_ReturnsCorrectValue(string input, PagePerms expected)
    {
        var result = PermissionParser.Parse(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that Parse throws FormatException for invalid characters.
    /// </summary>
    [Test]
    public void Parse_InvalidCharacter_ThrowsFormatException()
    {
        Assert.That(() => PermissionParser.Parse("rwz"), Throws.TypeOf<FormatException>());
    }

    /// <summary>
    /// Verifies that TryParse returns true and correct value for valid input.
    /// </summary>
    [Test]
    public void TryParse_ValidInput_ReturnsTrueAndCorrectValue()
    {
        bool success = PermissionParser.TryParse("rwx", out var result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(PagePerms.All));
    }

    /// <summary>
    /// Verifies that TryParse returns false for invalid input.
    /// </summary>
    [Test]
    public void TryParse_InvalidInput_ReturnsFalse()
    {
        bool success = PermissionParser.TryParse("invalid", out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.EqualTo(PagePerms.None));
    }

    /// <summary>
    /// Verifies that TryParse returns true and All for null input.
    /// </summary>
    [Test]
    public void TryParse_NullInput_ReturnsTrueAndAll()
    {
        bool success = PermissionParser.TryParse(null, out var result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(PagePerms.All));
    }

    /// <summary>
    /// Verifies that ToString correctly converts permissions to string.
    /// </summary>
    /// <param name="perms">The permissions to convert.</param>
    /// <param name="expected">The expected string.</param>
    [Test]
    [TestCase(PagePerms.All, "rwx")]
    [TestCase(PagePerms.ReadExecute, "r-x")]
    [TestCase(PagePerms.ReadWrite, "rw-")]
    [TestCase(PagePerms.Read, "r--")]
    [TestCase(PagePerms.Write, "-w-")]
    [TestCase(PagePerms.Execute, "--x")]
    [TestCase(PagePerms.None, "---")]
    public void ToString_ValidInput_ReturnsCorrectString(PagePerms perms, string expected)
    {
        var result = PermissionParser.ToString(perms);
        Assert.That(result, Is.EqualTo(expected));
    }
}