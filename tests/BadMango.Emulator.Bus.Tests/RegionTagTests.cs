// <copyright file="RegionTagTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="RegionTag"/> enum.
/// </summary>
[TestFixture]
public class RegionTagTests
{
    /// <summary>
    /// Verifies that Unknown has value 0.
    /// </summary>
    [Test]
    public void RegionTag_Unknown_HasValueZero()
    {
        Assert.That((ushort)RegionTag.Unknown, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that all expected region tags exist.
    /// </summary>
    [Test]
    public void RegionTag_ContainsExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.IsDefined(RegionTag.Unknown), Is.True);
            Assert.That(Enum.IsDefined(RegionTag.Ram), Is.True);
            Assert.That(Enum.IsDefined(RegionTag.Rom), Is.True);
            Assert.That(Enum.IsDefined(RegionTag.Io), Is.True);
            Assert.That(Enum.IsDefined(RegionTag.Slot), Is.True);
            Assert.That(Enum.IsDefined(RegionTag.Shadow), Is.True);
            Assert.That(Enum.IsDefined(RegionTag.Unmapped), Is.True);
            Assert.That(Enum.IsDefined(RegionTag.Video), Is.True);
            Assert.That(Enum.IsDefined(RegionTag.ZeroPage), Is.True);
            Assert.That(Enum.IsDefined(RegionTag.Stack), Is.True);
        });
    }

    /// <summary>
    /// Verifies that region tags have expected values.
    /// </summary>
    [Test]
    public void RegionTag_ValuesAreSequential()
    {
        Assert.Multiple(() =>
        {
            Assert.That((ushort)RegionTag.Unknown, Is.EqualTo(0));
            Assert.That((ushort)RegionTag.Ram, Is.EqualTo(1));
            Assert.That((ushort)RegionTag.Rom, Is.EqualTo(2));
            Assert.That((ushort)RegionTag.Io, Is.EqualTo(3));
            Assert.That((ushort)RegionTag.Slot, Is.EqualTo(4));
            Assert.That((ushort)RegionTag.Shadow, Is.EqualTo(5));
            Assert.That((ushort)RegionTag.Unmapped, Is.EqualTo(6));
            Assert.That((ushort)RegionTag.Video, Is.EqualTo(7));
            Assert.That((ushort)RegionTag.ZeroPage, Is.EqualTo(8));
            Assert.That((ushort)RegionTag.Stack, Is.EqualTo(9));
        });
    }
}