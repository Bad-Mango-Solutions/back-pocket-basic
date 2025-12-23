// <copyright file="PrivilegeLevelTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="PrivilegeLevel"/> enum.
/// </summary>
[TestFixture]
public class PrivilegeLevelTests
{
    /// <summary>
    /// Verifies that Ring0 has value 0 (highest privilege).
    /// </summary>
    [Test]
    public void PrivilegeLevel_Ring0_HasValueZero()
    {
        Assert.That((byte)PrivilegeLevel.Ring0, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that Ring3 has the highest numeric value (lowest privilege).
    /// </summary>
    [Test]
    public void PrivilegeLevel_Ring3_HasHighestNumericValue()
    {
        Assert.That((byte)PrivilegeLevel.Ring3, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that rings are ordered correctly.
    /// </summary>
    [Test]
    public void PrivilegeLevel_RingsAreOrderedCorrectly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PrivilegeLevel.Ring0, Is.LessThan(PrivilegeLevel.Ring1));
            Assert.That(PrivilegeLevel.Ring1, Is.LessThan(PrivilegeLevel.Ring2));
            Assert.That(PrivilegeLevel.Ring2, Is.LessThan(PrivilegeLevel.Ring3));
        });
    }

    /// <summary>
    /// Verifies that all expected privilege levels exist.
    /// </summary>
    [Test]
    public void PrivilegeLevel_ContainsAllExpectedValues()
    {
        var values = Enum.GetValues<PrivilegeLevel>();

        Assert.That(values, Has.Length.EqualTo(4));
        Assert.That(values, Contains.Item(PrivilegeLevel.Ring0));
        Assert.That(values, Contains.Item(PrivilegeLevel.Ring1));
        Assert.That(values, Contains.Item(PrivilegeLevel.Ring2));
        Assert.That(values, Contains.Item(PrivilegeLevel.Ring3));
    }
}