// <copyright file="AccessIntentTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="AccessIntent"/> enum.
/// </summary>
[TestFixture]
public class AccessIntentTests
{
    /// <summary>
    /// Verifies that all expected intent values exist.
    /// </summary>
    [Test]
    public void AccessIntent_ContainsExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.IsDefined(AccessIntent.DataRead), Is.True);
            Assert.That(Enum.IsDefined(AccessIntent.DataWrite), Is.True);
            Assert.That(Enum.IsDefined(AccessIntent.InstructionFetch), Is.True);
            Assert.That(Enum.IsDefined(AccessIntent.DebugRead), Is.True);
            Assert.That(Enum.IsDefined(AccessIntent.DebugWrite), Is.True);
            Assert.That(Enum.IsDefined(AccessIntent.DmaRead), Is.True);
            Assert.That(Enum.IsDefined(AccessIntent.DmaWrite), Is.True);
        });
    }

    /// <summary>
    /// Verifies that the enum has exactly seven values.
    /// </summary>
    [Test]
    public void AccessIntent_HasSevenValues()
    {
        var values = Enum.GetValues<AccessIntent>();
        Assert.That(values, Has.Length.EqualTo(7));
    }

    /// <summary>
    /// Verifies that intent values have sequential values starting from 0.
    /// </summary>
    [Test]
    public void AccessIntent_ValuesAreSequential()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)AccessIntent.DataRead, Is.EqualTo(0));
            Assert.That((int)AccessIntent.DataWrite, Is.EqualTo(1));
            Assert.That((int)AccessIntent.InstructionFetch, Is.EqualTo(2));
            Assert.That((int)AccessIntent.DebugRead, Is.EqualTo(3));
            Assert.That((int)AccessIntent.DebugWrite, Is.EqualTo(4));
            Assert.That((int)AccessIntent.DmaRead, Is.EqualTo(5));
            Assert.That((int)AccessIntent.DmaWrite, Is.EqualTo(6));
        });
    }
}