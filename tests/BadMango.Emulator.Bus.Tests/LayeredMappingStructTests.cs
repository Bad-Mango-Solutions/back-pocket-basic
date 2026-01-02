// <copyright file="LayeredMappingStructTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="LayeredMapping"/> record struct.
/// </summary>
[TestFixture]
public class LayeredMappingStructTests
{
    private const int PageSize = 4096;
    private const int PageShift = 12;

    /// <summary>
    /// Verifies that LayeredMapping is created with correct properties.
    /// </summary>
    [Test]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var layer = new MappingLayer("TestLayer", 10, false);
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var mapping = new LayeredMapping(
            VirtualBase: 0x4000,
            Size: 0x2000,
            Layer: layer,
            DeviceId: 42,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadWrite,
            Caps: TargetCaps.SupportsPeek,
            Target: target,
            PhysBase: 0x1000);

        Assert.Multiple(() =>
        {
            Assert.That(mapping.VirtualBase, Is.EqualTo(0x4000u));
            Assert.That(mapping.Size, Is.EqualTo(0x2000u));
            Assert.That(mapping.Layer.Name, Is.EqualTo("TestLayer"));
            Assert.That(mapping.DeviceId, Is.EqualTo(42));
            Assert.That(mapping.RegionTag, Is.EqualTo(RegionTag.Ram));
            Assert.That(mapping.Perms, Is.EqualTo(PagePerms.ReadWrite));
            Assert.That(mapping.Caps, Is.EqualTo(TargetCaps.SupportsPeek));
            Assert.That(mapping.Target, Is.SameAs(target));
            Assert.That(mapping.PhysBase, Is.EqualTo(0x1000u));
        });
    }

    /// <summary>
    /// Verifies VirtualEnd calculation.
    /// </summary>
    [Test]
    public void VirtualEnd_ReturnsCorrectValue()
    {
        var layer = new MappingLayer("Test", 0, false);
        var mapping = new LayeredMapping(
            VirtualBase: 0x1000,
            Size: 0x3000,
            Layer: layer,
            DeviceId: 0,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.None,
            Caps: TargetCaps.None,
            Target: null!,
            PhysBase: 0);

        Assert.That(mapping.VirtualEnd, Is.EqualTo(0x4000u));
    }

    /// <summary>
    /// Verifies ContainsAddress for addresses within range.
    /// </summary>
    [Test]
    public void ContainsAddress_AddressWithinRange_ReturnsTrue()
    {
        var layer = new MappingLayer("Test", 0, false);
        var mapping = new LayeredMapping(
            VirtualBase: 0x2000,
            Size: 0x2000,
            Layer: layer,
            DeviceId: 0,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.None,
            Caps: TargetCaps.None,
            Target: null!,
            PhysBase: 0);

        Assert.Multiple(() =>
        {
            Assert.That(mapping.ContainsAddress(0x2000), Is.True, "Start address");
            Assert.That(mapping.ContainsAddress(0x2001), Is.True, "Just after start");
            Assert.That(mapping.ContainsAddress(0x3000), Is.True, "Middle");
            Assert.That(mapping.ContainsAddress(0x3FFF), Is.True, "Last byte");
        });
    }

    /// <summary>
    /// Verifies ContainsAddress for addresses outside range.
    /// </summary>
    [Test]
    public void ContainsAddress_AddressOutsideRange_ReturnsFalse()
    {
        var layer = new MappingLayer("Test", 0, false);
        var mapping = new LayeredMapping(
            VirtualBase: 0x2000,
            Size: 0x2000,
            Layer: layer,
            DeviceId: 0,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.None,
            Caps: TargetCaps.None,
            Target: null!,
            PhysBase: 0);

        Assert.Multiple(() =>
        {
            Assert.That(mapping.ContainsAddress(0x1FFF), Is.False, "Just before start");
            Assert.That(mapping.ContainsAddress(0x4000), Is.False, "At end (exclusive)");
            Assert.That(mapping.ContainsAddress(0x4001), Is.False, "After end");
            Assert.That(mapping.ContainsAddress(0x0000), Is.False, "Far before");
            Assert.That(mapping.ContainsAddress(0xFFFF), Is.False, "Far after");
        });
    }

    /// <summary>
    /// Verifies GetStartPage calculation.
    /// </summary>
    [Test]
    public void GetStartPage_ReturnsCorrectPageIndex()
    {
        var layer = new MappingLayer("Test", 0, false);

        // Mapping at page 4 (0x4000)
        var mapping1 = new LayeredMapping(0x4000, PageSize, layer, 0, RegionTag.Ram, PagePerms.None, TargetCaps.None, null!, 0);
        Assert.That(mapping1.GetStartPage(PageShift), Is.EqualTo(4));

        // Mapping at page 0 (0x0000)
        var mapping2 = new LayeredMapping(0x0000, PageSize, layer, 0, RegionTag.Ram, PagePerms.None, TargetCaps.None, null!, 0);
        Assert.That(mapping2.GetStartPage(PageShift), Is.EqualTo(0));

        // Mapping at page 13 (0xD000)
        var mapping3 = new LayeredMapping(0xD000, PageSize, layer, 0, RegionTag.Ram, PagePerms.None, TargetCaps.None, null!, 0);
        Assert.That(mapping3.GetStartPage(PageShift), Is.EqualTo(13));
    }

    /// <summary>
    /// Verifies GetPageCount calculation.
    /// </summary>
    [Test]
    public void GetPageCount_ReturnsCorrectCount()
    {
        var layer = new MappingLayer("Test", 0, false);

        // Single page mapping
        var mapping1 = new LayeredMapping(0x0000, PageSize, layer, 0, RegionTag.Ram, PagePerms.None, TargetCaps.None, null!, 0);
        Assert.That(mapping1.GetPageCount(PageShift), Is.EqualTo(1));

        // 4 page mapping
        var mapping2 = new LayeredMapping(0x0000, PageSize * 4, layer, 0, RegionTag.Ram, PagePerms.None, TargetCaps.None, null!, 0);
        Assert.That(mapping2.GetPageCount(PageShift), Is.EqualTo(4));

        // 16 page mapping (full 64KB)
        var mapping3 = new LayeredMapping(0x0000, 0x10000, layer, 0, RegionTag.Ram, PagePerms.None, TargetCaps.None, null!, 0);
        Assert.That(mapping3.GetPageCount(PageShift), Is.EqualTo(16));
    }

    /// <summary>
    /// Verifies record equality.
    /// </summary>
    [Test]
    public void Equality_SameValues_AreEqual()
    {
        var layer = new MappingLayer("Test", 10, false);
        var mapping1 = new LayeredMapping(0x1000, 0x1000, layer, 1, RegionTag.Ram, PagePerms.Read, TargetCaps.None, null!, 0);
        var mapping2 = new LayeredMapping(0x1000, 0x1000, layer, 1, RegionTag.Ram, PagePerms.Read, TargetCaps.None, null!, 0);

        Assert.That(mapping1, Is.EqualTo(mapping2));
    }

    /// <summary>
    /// Verifies record inequality for different values.
    /// </summary>
    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var layer = new MappingLayer("Test", 10, false);
        var baseline = new LayeredMapping(0x1000, 0x1000, layer, 1, RegionTag.Ram, PagePerms.Read, TargetCaps.None, null!, 0);

        var differentBase = baseline with { VirtualBase = 0x2000 };
        var differentSize = baseline with { Size = 0x2000 };
        var differentDevice = baseline with { DeviceId = 2 };
        var differentTag = baseline with { RegionTag = RegionTag.Rom };

        Assert.Multiple(() =>
        {
            Assert.That(baseline, Is.Not.EqualTo(differentBase), "Different VirtualBase");
            Assert.That(baseline, Is.Not.EqualTo(differentSize), "Different Size");
            Assert.That(baseline, Is.Not.EqualTo(differentDevice), "Different DeviceId");
            Assert.That(baseline, Is.Not.EqualTo(differentTag), "Different RegionTag");
        });
    }

    /// <summary>
    /// Verifies with expression creates modified copy.
    /// </summary>
    [Test]
    public void WithExpression_CreatesModifiedCopy()
    {
        var layer = new MappingLayer("Test", 10, false);
        var original = new LayeredMapping(0x1000, 0x1000, layer, 1, RegionTag.Ram, PagePerms.Read, TargetCaps.None, null!, 0);

        var modified = original with { Perms = PagePerms.ReadWrite };

        Assert.Multiple(() =>
        {
            Assert.That(modified.VirtualBase, Is.EqualTo(original.VirtualBase));
            Assert.That(modified.Size, Is.EqualTo(original.Size));
            Assert.That(modified.Perms, Is.EqualTo(PagePerms.ReadWrite));
            Assert.That(original.Perms, Is.EqualTo(PagePerms.Read), "Original unchanged");
        });
    }
}