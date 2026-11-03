// <copyright file="BusFaultRingTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Unit tests for the <see cref="BusFaultRing"/> ring buffer of bus faults.
/// </summary>
[TestFixture]
public class BusFaultRingTests
{
    /// <summary>
    /// Verifies that a brand-new ring reports zero counts and no last fault.
    /// </summary>
    [Test]
    public void NewRing_IsEmpty()
    {
        var ring = new BusFaultRing(capacity: 4);

        Assert.Multiple(() =>
        {
            Assert.That(ring.Capacity, Is.EqualTo(4));
            Assert.That(ring.Count, Is.EqualTo(0));
            Assert.That(ring.TotalFaults, Is.EqualTo(0ul));
            Assert.That(ring.Last, Is.Null);
            Assert.That(ring.Snapshot(), Is.Empty);
        });
    }

    /// <summary>
    /// Verifies that the constructor rejects non-positive capacities.
    /// </summary>
    [Test]
    public void Constructor_RejectsZeroOrNegativeCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BusFaultRing(capacity: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BusFaultRing(capacity: -1));
    }

    /// <summary>
    /// Verifies that recording a non-fault value is silently ignored.
    /// </summary>
    [Test]
    public void Record_IgnoresNonFault()
    {
        var ring = new BusFaultRing(capacity: 4);
        var none = default(BusFault); // Kind == None

        ring.Record(in none);

        Assert.Multiple(() =>
        {
            Assert.That(ring.Count, Is.EqualTo(0));
            Assert.That(ring.TotalFaults, Is.EqualTo(0ul));
            Assert.That(ring.Last, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that recording faults appends them in order and updates counters.
    /// </summary>
    [Test]
    public void Record_AppendsFaultsInOrder()
    {
        var ring = new BusFaultRing(capacity: 4);
        var f1 = MakeFault(0x1000, 100);
        var f2 = MakeFault(0x2000, 200);

        ring.Record(in f1);
        ring.Record(in f2);

        var snap = ring.Snapshot();
        Assert.Multiple(() =>
        {
            Assert.That(ring.Count, Is.EqualTo(2));
            Assert.That(ring.TotalFaults, Is.EqualTo(2ul));
            Assert.That(ring.Last, Is.Not.Null);
            Assert.That(ring.Last!.Value.Address, Is.EqualTo(0x2000u));
            Assert.That(snap.Length, Is.EqualTo(2));
            Assert.That(snap[0].Address, Is.EqualTo(0x1000u));
            Assert.That(snap[1].Address, Is.EqualTo(0x2000u));
        });
    }

    /// <summary>
    /// Verifies that the ring overwrites the oldest entry when full and that
    /// <see cref="IBusFaultRing.TotalFaults"/> keeps the dropped fault counted.
    /// </summary>
    [Test]
    public void Record_WrapsWhenFullAndPreservesTotalFaults()
    {
        var ring = new BusFaultRing(capacity: 3);
        for (uint i = 0; i < 5; i++)
        {
            var f = MakeFault(0x1000u + (i * 0x100u), i);
            ring.Record(in f);
        }

        var snap = ring.Snapshot();
        Assert.Multiple(() =>
        {
            Assert.That(ring.Count, Is.EqualTo(3));
            Assert.That(ring.TotalFaults, Is.EqualTo(5ul));
            Assert.That(snap.Length, Is.EqualTo(3));

            // Oldest retained should be the third fault (index 2, address $1200)
            Assert.That(snap[0].Address, Is.EqualTo(0x1200u));
            Assert.That(snap[1].Address, Is.EqualTo(0x1300u));
            Assert.That(snap[2].Address, Is.EqualTo(0x1400u));
        });
    }

    /// <summary>
    /// Verifies that <see cref="BusFaultRing.Clear"/> resets all state.
    /// </summary>
    [Test]
    public void Clear_ResetsAllState()
    {
        var ring = new BusFaultRing(capacity: 4);
        var f = MakeFault(0x1000, 1);
        ring.Record(in f);

        ring.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(ring.Count, Is.EqualTo(0));
            Assert.That(ring.TotalFaults, Is.EqualTo(0ul));
            Assert.That(ring.Last, Is.Null);
            Assert.That(ring.Snapshot(), Is.Empty);
        });
    }

    private static BusFault MakeFault(Addr address, ulong cycle) =>
        new(
            Kind: FaultKind.Unmapped,
            Address: address,
            WidthBits: 8,
            Intent: AccessIntent.DataRead,
            Mode: BusAccessMode.Atomic,
            SourceId: 0,
            DeviceId: -1,
            RegionTag: RegionTag.Unknown,
            Cycle: cycle);
}