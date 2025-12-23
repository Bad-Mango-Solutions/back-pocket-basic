// <copyright file="TargetCapsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="TargetCaps"/> enum.
/// </summary>
[TestFixture]
public class TargetCapsTests
{
    /// <summary>
    /// Verifies that None has value 0.
    /// </summary>
    [Test]
    public void TargetCaps_None_HasValueZero()
    {
        Assert.That((uint)TargetCaps.None, Is.EqualTo(0u));
    }

    /// <summary>
    /// Verifies that all flags are distinct powers of 2.
    /// </summary>
    [Test]
    public void TargetCaps_AllFlagsArePowersOfTwo()
    {
        Assert.Multiple(() =>
        {
            Assert.That((uint)TargetCaps.SupportsPeek, Is.EqualTo(1u));
            Assert.That((uint)TargetCaps.SupportsPoke, Is.EqualTo(2u));
            Assert.That((uint)TargetCaps.SupportsWide, Is.EqualTo(4u));
            Assert.That((uint)TargetCaps.HasSideEffects, Is.EqualTo(8u));
            Assert.That((uint)TargetCaps.TimingSensitive, Is.EqualTo(16u));
        });
    }

    /// <summary>
    /// Verifies that flags can be combined.
    /// </summary>
    [Test]
    public void TargetCaps_CanBeCombined()
    {
        var ramCaps = TargetCaps.SupportsPeek | TargetCaps.SupportsPoke | TargetCaps.SupportsWide;

        Assert.Multiple(() =>
        {
            Assert.That(ramCaps.HasFlag(TargetCaps.SupportsPeek), Is.True);
            Assert.That(ramCaps.HasFlag(TargetCaps.SupportsPoke), Is.True);
            Assert.That(ramCaps.HasFlag(TargetCaps.SupportsWide), Is.True);
            Assert.That(ramCaps.HasFlag(TargetCaps.HasSideEffects), Is.False);
            Assert.That(ramCaps.HasFlag(TargetCaps.TimingSensitive), Is.False);
        });
    }

    /// <summary>
    /// Verifies typical I/O region capabilities.
    /// </summary>
    [Test]
    public void TargetCaps_IoRegionTypicalCaps()
    {
        var ioCaps = TargetCaps.HasSideEffects | TargetCaps.TimingSensitive;

        Assert.Multiple(() =>
        {
            Assert.That(ioCaps.HasFlag(TargetCaps.HasSideEffects), Is.True);
            Assert.That(ioCaps.HasFlag(TargetCaps.TimingSensitive), Is.True);
            Assert.That(ioCaps.HasFlag(TargetCaps.SupportsPeek), Is.False);
            Assert.That(ioCaps.HasFlag(TargetCaps.SupportsWide), Is.False);
        });
    }
}