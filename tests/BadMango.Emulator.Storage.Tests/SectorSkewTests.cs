// <copyright file="SectorSkewTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Tests;

/// <summary>
/// DOS 3.3 / ProDOS sector-skew table tests (PRD §6.1 row 3).
/// </summary>
[TestFixture]
public class SectorSkewTests
{
    /// <summary>
    /// Verifies that for every order, both directions of the skew table are inverses.
    /// </summary>
    /// <param name="order">Sector order under test.</param>
    [TestCase(SectorOrder.Dos33)]
    [TestCase(SectorOrder.ProDos)]
    public void Skew_RoundTrip(SectorOrder order)
    {
        for (var phys = 0; phys < 16; phys++)
        {
            var logical = SectorSkew.PhysicalToLogical(order, phys);
            var roundtrip = SectorSkew.LogicalToPhysical(order, logical);
            Assert.That(roundtrip, Is.EqualTo(phys));
        }

        for (var logical = 0; logical < 16; logical++)
        {
            var phys = SectorSkew.LogicalToPhysical(order, logical);
            var roundtrip = SectorSkew.PhysicalToLogical(order, phys);
            Assert.That(roundtrip, Is.EqualTo(logical));
        }
    }

    /// <summary>
    /// Verifies that sector 0 and sector 15 are fixed in both schemes.
    /// </summary>
    /// <param name="order">Sector order.</param>
    [TestCase(SectorOrder.Dos33)]
    [TestCase(SectorOrder.ProDos)]
    public void Skew_FixedEndpoints(SectorOrder order)
    {
        Assert.That(SectorSkew.PhysicalToLogical(order, 0), Is.EqualTo(0));
        Assert.That(SectorSkew.PhysicalToLogical(order, 15), Is.EqualTo(15));
    }

    /// <summary>
    /// Verifies that DOS and ProDOS skews differ in the middle sectors (i.e. the two
    /// orderings are not equivalent — otherwise <c>.dsk</c> sniffing would be moot).
    /// </summary>
    [Test]
    public void Skew_DosAndProDosDiffer()
    {
        var anyDifferent = false;
        for (var i = 1; i < 15; i++)
        {
            if (SectorSkew.PhysicalToLogical(SectorOrder.Dos33, i) != SectorSkew.PhysicalToLogical(SectorOrder.ProDos, i))
            {
                anyDifferent = true;
                break;
            }
        }

        Assert.That(anyDifferent, Is.True);
    }

    /// <summary>
    /// Out-of-range arguments throw.
    /// </summary>
    [Test]
    public void Skew_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SectorSkew.PhysicalToLogical(SectorOrder.Dos33, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => SectorSkew.PhysicalToLogical(SectorOrder.Dos33, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => SectorSkew.LogicalToPhysical(SectorOrder.ProDos, 16));
    }
}