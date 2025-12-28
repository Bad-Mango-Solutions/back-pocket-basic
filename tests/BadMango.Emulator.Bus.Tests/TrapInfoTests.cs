// <copyright file="TrapInfoTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="TrapInfo"/> struct.
/// </summary>
[TestFixture]
public class TrapInfoTests
{
    /// <summary>
    /// Verifies that the constructor sets all properties.
    /// </summary>
    [Test]
    public void Constructor_SetsAllProperties()
    {
        var info = new TrapInfo(
            Address: 0xFC58,
            Name: "HOME",
            Category: TrapCategory.Monitor,
            Description: "Clear screen and home cursor",
            Enabled: true);

        Assert.Multiple(() =>
        {
            Assert.That(info.Address, Is.EqualTo(0xFC58u));
            Assert.That(info.Name, Is.EqualTo("HOME"));
            Assert.That(info.Category, Is.EqualTo(TrapCategory.Monitor));
            Assert.That(info.Description, Is.EqualTo("Clear screen and home cursor"));
            Assert.That(info.Enabled, Is.True);
        });
    }

    /// <summary>
    /// Verifies that Description can be null.
    /// </summary>
    [Test]
    public void Constructor_AllowsNullDescription()
    {
        var info = new TrapInfo(
            Address: 0xFDED,
            Name: "COUT",
            Category: TrapCategory.Monitor,
            Description: null,
            Enabled: true);

        Assert.That(info.Description, Is.Null);
    }

    /// <summary>
    /// Verifies that record struct equality works correctly.
    /// </summary>
    [Test]
    public void Equality_WorksCorrectly()
    {
        var info1 = new TrapInfo(0xFC58, "HOME", TrapCategory.Monitor, "desc", true);
        var info2 = new TrapInfo(0xFC58, "HOME", TrapCategory.Monitor, "desc", true);
        var info3 = new TrapInfo(0xFC58, "HOME", TrapCategory.Monitor, "desc", false);

        Assert.Multiple(() =>
        {
            Assert.That(info1, Is.EqualTo(info2));
            Assert.That(info1, Is.Not.EqualTo(info3));
        });
    }
}