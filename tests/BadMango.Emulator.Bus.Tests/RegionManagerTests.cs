// <copyright file="RegionManagerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

/// <summary>
/// Unit tests for the <see cref="RegionManager"/> class.
/// </summary>
[TestFixture]
public class RegionManagerTests
{
    /// <summary>
    /// Verifies that regions can be registered.
    /// </summary>
    [Test]
    public void RegisterRegion_AddsRegion()
    {
        var manager = new RegionManager();
        var region = CreateTestRegion("test1", 0x1000);

        manager.RegisterRegion(region);

        Assert.Multiple(() =>
        {
            Assert.That(manager.RegionCount, Is.EqualTo(1));
            Assert.That(manager.GetRegion("test1"), Is.SameAs(region));
        });
    }

    /// <summary>
    /// Verifies that duplicate region IDs throw.
    /// </summary>
    [Test]
    public void RegisterRegion_ThrowsForDuplicateId()
    {
        var manager = new RegionManager();
        var region1 = CreateTestRegion("test1", 0x1000);
        var region2 = CreateTestRegion("test1", 0x2000);

        manager.RegisterRegion(region1);

        Assert.Throws<ArgumentException>(() => manager.RegisterRegion(region2));
    }

    /// <summary>
    /// Verifies that regions can be unregistered.
    /// </summary>
    [Test]
    public void UnregisterRegion_RemovesRegion()
    {
        var manager = new RegionManager();
        var region = CreateTestRegion("test1", 0x1000);

        manager.RegisterRegion(region);
        var result = manager.UnregisterRegion("test1");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(manager.RegionCount, Is.EqualTo(0));
            Assert.That(manager.GetRegion("test1"), Is.Null);
        });
    }

    /// <summary>
    /// Verifies that GetRegion returns null for non-existent region.
    /// </summary>
    [Test]
    public void GetRegion_ReturnsNullForNonExistent()
    {
        var manager = new RegionManager();

        var result = manager.GetRegion("nonexistent");

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies that GetRegionsInRange finds overlapping regions.
    /// </summary>
    [Test]
    public void GetRegionsInRange_FindsOverlappingRegions()
    {
        var manager = new RegionManager();
        var region1 = CreateTestRegion("test1", 0x1000);  // 0x1000-0x1FFF
        var region2 = CreateTestRegion("test2", 0x2000);  // 0x2000-0x2FFF
        var region3 = CreateTestRegion("test3", 0x5000);  // 0x5000-0x5FFF

        manager.RegisterRegion(region1);
        manager.RegisterRegion(region2);
        manager.RegisterRegion(region3);

        var result = manager.GetRegionsInRange(0x1800, 0x1000).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Any(r => r.Id == "test1"), Is.True);
            Assert.That(result.Any(r => r.Id == "test2"), Is.True);
        });
    }

    /// <summary>
    /// Verifies that MapRegionAtPreferred creates mapping stack.
    /// </summary>
    [Test]
    public void MapRegionAtPreferred_CreatesMappingStack()
    {
        var manager = new RegionManager();
        var region = CreateTestRegion("test1", 0x1000);

        manager.MapRegionAtPreferred(region);

        var stack = manager.GetMappingStack(0x1000);
        Assert.Multiple(() =>
        {
            Assert.That(stack, Is.Not.Null);
            Assert.That(stack!.ActiveEntry, Is.Not.Null);
            Assert.That(stack.ActiveEntry!.Value.Region.Id, Is.EqualTo("test1"));
        });
    }

    /// <summary>
    /// Verifies that MapRegionAt with different address works for relocatable regions.
    /// </summary>
    [Test]
    public void MapRegionAt_WorksForRelocatableRegion()
    {
        var manager = new RegionManager();
        var region = CreateTestRegion("test1", 0x1000);

        manager.MapRegionAt(region, 0x5000);

        var stack = manager.GetMappingStack(0x5000);
        Assert.That(stack, Is.Not.Null);
        Assert.That(stack!.ActiveEntry!.Value.Region.Id, Is.EqualTo("test1"));
    }

    /// <summary>
    /// Verifies that MapRegionAt throws for non-relocatable region at wrong address.
    /// </summary>
    [Test]
    public void MapRegionAt_ThrowsForNonRelocatableAtWrongAddress()
    {
        var manager = new RegionManager();
        var memory = new PhysicalMemory(4096, "ROM");
        var region = MemoryRegion.CreateRom("rom", "Boot ROM", 0x0000, memory);

        Assert.Throws<InvalidOperationException>(() => manager.MapRegionAt(region, 0x5000));
    }

    /// <summary>
    /// Verifies that ActivateRegion activates a mapped region.
    /// </summary>
    [Test]
    public void ActivateRegion_ActivatesMappedRegion()
    {
        var manager = new RegionManager();
        var region = CreateTestRegion("test1", 0x1000);

        manager.MapRegionAtPreferred(region, active: false);
        var result = manager.ActivateRegion("test1");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            var stack = manager.GetMappingStack(0x1000);
            Assert.That(stack!.ActiveEntry, Is.Not.Null);
        });
    }

    /// <summary>
    /// Verifies that DeactivateRegion deactivates a mapped region.
    /// </summary>
    [Test]
    public void DeactivateRegion_DeactivatesMappedRegion()
    {
        var manager = new RegionManager();
        var region = CreateTestRegion("test1", 0x1000);

        manager.MapRegionAtPreferred(region, active: true);
        var result = manager.DeactivateRegion("test1");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            var stack = manager.GetMappingStack(0x1000);
            Assert.That(stack!.ActiveEntry, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that SwitchBank switches between regions.
    /// </summary>
    [Test]
    public void SwitchBank_SwitchesBetweenRegions()
    {
        var manager = new RegionManager();
        var region1 = CreateTestRegion("bank0", 0x1000);
        var region2 = CreateTestRegion("bank1", 0x1000);

        manager.MapRegionAtPreferred(region1, active: true);
        manager.MapRegionAt(region2, 0x1000, active: false);

        var result = manager.SwitchBank("bank1", "bank0");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            var stack = manager.GetMappingStack(0x1000);

            // bank1 should now be active, bank0 should be inactive
            Assert.That(stack!.Entries.Any(e => e.Region.Id == "bank1" && e.IsActive), Is.True);
            Assert.That(stack.Entries.Any(e => e.Region.Id == "bank0" && !e.IsActive), Is.True);
        });
    }

    /// <summary>
    /// Verifies that snapshot and restore work correctly.
    /// </summary>
    [Test]
    public void CreateSnapshot_And_RestoreSnapshot_PreservesState()
    {
        var manager = new RegionManager();
        var region1 = CreateTestRegion("test1", 0x1000);
        var region2 = CreateTestRegion("test2", 0x2000);

        manager.MapRegionAtPreferred(region1);
        manager.MapRegionAtPreferred(region2);

        var snapshot = manager.CreateSnapshot();

        // Modify state
        manager.DeactivateRegion("test1");

        // Restore
        manager.RestoreSnapshot(snapshot);

        var stack = manager.GetMappingStack(0x1000);
        Assert.That(stack!.ActiveEntry!.Value.Region.Id, Is.EqualTo("test1"));
    }

    /// <summary>
    /// Verifies that RestoreSnapshot throws for invalid snapshot.
    /// </summary>
    [Test]
    public void RestoreSnapshot_ThrowsForInvalidSnapshot()
    {
        var manager = new RegionManager();

        Assert.Throws<ArgumentException>(() => manager.RestoreSnapshot("invalid"));
    }

    /// <summary>
    /// Verifies that multiple regions can be overlaid at the same address.
    /// </summary>
    [Test]
    public void MappingStacks_SupportOverlays()
    {
        var manager = new RegionManager();
        var ramRegion = CreateTestRegion("ram", 0xC000);
        var romRegion = CreateRomRegion("rom", 0xC000);

        // Map both at same address
        manager.MapRegionAtPreferred(ramRegion, active: true);
        manager.MapRegionAt(romRegion, 0xC000, active: false);

        var stack = manager.GetMappingStack(0xC000);

        Assert.Multiple(() =>
        {
            Assert.That(stack!.Count, Is.EqualTo(2));

            // Initially RAM is active
            Assert.That(stack.ActiveEntry!.Value.Region.Id, Is.EqualTo("ram"));

            // Activate ROM overlay
            stack.SetActive("rom", true);
            stack.SetActive("ram", false);

            // Now ROM is active
            Assert.That(stack.ActiveEntry!.Value.Region.Id, Is.EqualTo("rom"));
        });
    }

    private static IMemoryRegion CreateTestRegion(string id, uint baseAddress)
    {
        var memory = new PhysicalMemory(4096, $"Memory for {id}");
        return MemoryRegion.CreateRam(id, $"Region {id}", baseAddress, memory);
    }

    private static IMemoryRegion CreateRomRegion(string id, uint baseAddress)
    {
        var memory = new PhysicalMemory(4096, $"Memory for {id}");
        return MemoryRegion.CreateRom(id, $"Region {id}", baseAddress, memory);
    }
}