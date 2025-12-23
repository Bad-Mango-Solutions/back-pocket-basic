// <copyright file="AccessFlagsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="AccessFlags"/> enum.
/// </summary>
[TestFixture]
public class AccessFlagsTests
{
    /// <summary>
    /// Verifies that None has value 0.
    /// </summary>
    [Test]
    public void AccessFlags_None_HasValueZero()
    {
        Assert.That((uint)AccessFlags.None, Is.EqualTo(0u));
    }

    /// <summary>
    /// Verifies that all flags are distinct powers of 2.
    /// </summary>
    [Test]
    public void AccessFlags_AllFlagsArePowersOfTwo()
    {
        Assert.Multiple(() =>
        {
            Assert.That((uint)AccessFlags.NoSideEffects, Is.EqualTo(1u));
            Assert.That((uint)AccessFlags.LittleEndian, Is.EqualTo(2u));
            Assert.That((uint)AccessFlags.Atomic, Is.EqualTo(4u));
            Assert.That((uint)AccessFlags.Decompose, Is.EqualTo(8u));
        });
    }

    /// <summary>
    /// Verifies that flags can be combined with bitwise OR.
    /// </summary>
    [Test]
    public void AccessFlags_CanBeCombined()
    {
        var combined = AccessFlags.NoSideEffects | AccessFlags.Atomic;

        Assert.Multiple(() =>
        {
            Assert.That(combined.HasFlag(AccessFlags.NoSideEffects), Is.True);
            Assert.That(combined.HasFlag(AccessFlags.Atomic), Is.True);
            Assert.That(combined.HasFlag(AccessFlags.LittleEndian), Is.False);
            Assert.That(combined.HasFlag(AccessFlags.Decompose), Is.False);
        });
    }

    /// <summary>
    /// Verifies that flags can be checked with bitwise AND.
    /// </summary>
    [Test]
    public void AccessFlags_BitwiseAndWorks()
    {
        var flags = AccessFlags.LittleEndian | AccessFlags.Decompose;

        Assert.Multiple(() =>
        {
            Assert.That((flags & AccessFlags.LittleEndian) != 0, Is.True);
            Assert.That((flags & AccessFlags.Decompose) != 0, Is.True);
            Assert.That((flags & AccessFlags.NoSideEffects) != 0, Is.False);
            Assert.That((flags & AccessFlags.Atomic) != 0, Is.False);
        });
    }
}