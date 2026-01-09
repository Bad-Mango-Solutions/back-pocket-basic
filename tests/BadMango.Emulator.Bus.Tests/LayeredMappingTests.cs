// <copyright file="LayeredMappingTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for layered mapping support in <see cref="MainBus"/>.
/// </summary>
[TestFixture]
public class LayeredMappingTests
{
    private const int PageSize = 4096;

    /// <summary>
    /// Verifies that a layer can be created with a name and priority.
    /// </summary>
    [Test]
    public void CreateLayer_CreatesLayerWithCorrectProperties()
    {
        var bus = new MainBus();

        var layer = bus.CreateLayer("TestLayer", priority: 10);

        Assert.Multiple(() =>
        {
            Assert.That(layer.Name, Is.EqualTo("TestLayer"));
            Assert.That(layer.Priority, Is.EqualTo(10));
            Assert.That(layer.IsActive, Is.False, "New layers should be inactive by default");
        });
    }

    /// <summary>
    /// Verifies that creating a layer with a duplicate name throws.
    /// </summary>
    [Test]
    public void CreateLayer_DuplicateName_ThrowsArgumentException()
    {
        var bus = new MainBus();
        bus.CreateLayer("TestLayer", priority: 10);

        Assert.Throws<ArgumentException>(() => bus.CreateLayer("TestLayer", priority: 20));
    }

    /// <summary>
    /// Verifies that GetLayer returns the correct layer.
    /// </summary>
    [Test]
    public void GetLayer_ExistingLayer_ReturnsLayer()
    {
        var bus = new MainBus();
        bus.CreateLayer("TestLayer", priority: 10);

        var layer = bus.GetLayer("TestLayer");

        Assert.Multiple(() =>
        {
            Assert.That(layer, Is.Not.Null);
            Assert.That(layer!.Value.Name, Is.EqualTo("TestLayer"));
            Assert.That(layer.Value.Priority, Is.EqualTo(10));
        });
    }

    /// <summary>
    /// Verifies that GetLayer returns null for non-existent layer.
    /// </summary>
    [Test]
    public void GetLayer_NonExistentLayer_ReturnsNull()
    {
        var bus = new MainBus();

        var layer = bus.GetLayer("NonExistent");

        Assert.That(layer, Is.Null);
    }

    /// <summary>
    /// Verifies that ActivateLayer makes a layer active.
    /// </summary>
    [Test]
    public void ActivateLayer_MakesLayerActive()
    {
        var bus = new MainBus();
        bus.CreateLayer("TestLayer", priority: 10);

        bus.ActivateLayer("TestLayer");

        Assert.That(bus.IsLayerActive("TestLayer"), Is.True);
    }

    /// <summary>
    /// Verifies that DeactivateLayer makes a layer inactive.
    /// </summary>
    [Test]
    public void DeactivateLayer_MakesLayerInactive()
    {
        var bus = new MainBus();
        bus.CreateLayer("TestLayer", priority: 10);
        bus.ActivateLayer("TestLayer");

        bus.DeactivateLayer("TestLayer");

        Assert.That(bus.IsLayerActive("TestLayer"), Is.False);
    }

    /// <summary>
    /// Verifies that ActivateLayer throws for non-existent layer.
    /// </summary>
    [Test]
    public void ActivateLayer_NonExistentLayer_ThrowsKeyNotFoundException()
    {
        var bus = new MainBus();

        Assert.Throws<KeyNotFoundException>(() => bus.ActivateLayer("NonExistent"));
    }

    /// <summary>
    /// Verifies that DeactivateLayer throws for non-existent layer.
    /// </summary>
    [Test]
    public void DeactivateLayer_NonExistentLayer_ThrowsKeyNotFoundException()
    {
        var bus = new MainBus();

        Assert.Throws<KeyNotFoundException>(() => bus.DeactivateLayer("NonExistent"));
    }

    /// <summary>
    /// Verifies that IsLayerActive throws for non-existent layer.
    /// </summary>
    [Test]
    public void IsLayerActive_NonExistentLayer_ThrowsKeyNotFoundException()
    {
        var bus = new MainBus();

        Assert.Throws<KeyNotFoundException>(() => bus.IsLayerActive("NonExistent"));
    }

    /// <summary>
    /// Verifies that AddLayeredMapping adds a mapping that becomes effective on activation.
    /// </summary>
    [Test]
    public void AddLayeredMapping_ActivatingLayer_MakesMappingEffective()
    {
        var bus = new MainBus();
        var baseMemory = new PhysicalMemory(PageSize, "BaseRAM");
        var overlayMemory = new PhysicalMemory(PageSize, "OverlayRAM");
        baseMemory.AsSpan()[0x100] = 0x11;
        overlayMemory.AsSpan()[0x100] = 0x22;
        var baseTarget = new RamTarget(baseMemory.Slice(0, PageSize));
        var overlayTarget = new RamTarget(overlayMemory.Slice(0, PageSize));

        // Set up base mapping
        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, baseTarget, 0));
        bus.SaveBaseMapping(0);

        // Create layer and add mapping
        var layer = bus.CreateLayer("Overlay", priority: 10);
        var mapping = new LayeredMapping(
            VirtualBase: 0x0000,
            Size: PageSize,
            Layer: layer,
            DeviceId: 2,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadWrite,
            Caps: TargetCaps.SupportsPeek,
            Target: overlayTarget,
            PhysBase: 0);
        bus.AddLayeredMapping(mapping);

        // Verify base mapping is still in effect
        var access = CreateTestAccess(0x0100, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0x11), "Base mapping should be active initially");

        // Activate layer and verify overlay is now in effect
        bus.ActivateLayer("Overlay");
        Assert.That(bus.Read8(access), Is.EqualTo(0x22), "Overlay should be active after activation");
    }

    /// <summary>
    /// Verifies that deactivating a layer reveals the underlying layer.
    /// </summary>
    [Test]
    public void DeactivateLayer_RevealsUnderlyingLayer()
    {
        var bus = new MainBus();
        var baseMemory = new PhysicalMemory(PageSize, "BaseRAM");
        var overlayMemory = new PhysicalMemory(PageSize, "OverlayRAM");
        baseMemory.AsSpan()[0x100] = 0x11;
        overlayMemory.AsSpan()[0x100] = 0x22;
        var baseTarget = new RamTarget(baseMemory.Slice(0, PageSize));
        var overlayTarget = new RamTarget(overlayMemory.Slice(0, PageSize));

        // Set up base mapping
        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, baseTarget, 0));
        bus.SaveBaseMapping(0);

        // Create layer and add mapping
        var layer = bus.CreateLayer("Overlay", priority: 10);
        var mapping = new LayeredMapping(
            VirtualBase: 0x0000,
            Size: PageSize,
            Layer: layer,
            DeviceId: 2,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadWrite,
            Caps: TargetCaps.SupportsPeek,
            Target: overlayTarget,
            PhysBase: 0);
        bus.AddLayeredMapping(mapping);
        bus.ActivateLayer("Overlay");

        // Deactivate and verify base is revealed
        bus.DeactivateLayer("Overlay");
        var access = CreateTestAccess(0x0100, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0x11), "Base mapping should be revealed after deactivation");
    }

    /// <summary>
    /// Verifies that higher priority layers override lower priority layers.
    /// </summary>
    [Test]
    public void PriorityOrdering_HigherPriorityWins()
    {
        var bus = new MainBus();
        var lowMemory = new PhysicalMemory(PageSize, "LowPriorityRAM");
        var highMemory = new PhysicalMemory(PageSize, "HighPriorityRAM");
        var baseMemory = new PhysicalMemory(PageSize, "BaseRAM");
        lowMemory.AsSpan()[0x100] = 0x11;
        highMemory.AsSpan()[0x100] = 0x22;
        baseMemory.AsSpan()[0x100] = 0x00;
        var lowTarget = new RamTarget(lowMemory.Slice(0, PageSize));
        var highTarget = new RamTarget(highMemory.Slice(0, PageSize));
        var baseTarget = new RamTarget(baseMemory.Slice(0, PageSize));

        // Set up base mapping
        bus.MapPage(0, new(0, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, baseTarget, 0));
        bus.SaveBaseMapping(0);

        // Create low priority layer
        var lowLayer = bus.CreateLayer("LowPriority", priority: 5);
        bus.AddLayeredMapping(new(
            VirtualBase: 0x0000,
            Size: PageSize,
            Layer: lowLayer,
            DeviceId: 1,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadWrite,
            Caps: TargetCaps.SupportsPeek,
            Target: lowTarget,
            PhysBase: 0));

        // Create high priority layer
        var highLayer = bus.CreateLayer("HighPriority", priority: 10);
        bus.AddLayeredMapping(new(
            VirtualBase: 0x0000,
            Size: PageSize,
            Layer: highLayer,
            DeviceId: 2,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadWrite,
            Caps: TargetCaps.SupportsPeek,
            Target: highTarget,
            PhysBase: 0));

        // Activate both layers
        bus.ActivateLayer("LowPriority");
        bus.ActivateLayer("HighPriority");

        // Higher priority should win
        var access = CreateTestAccess(0x0100, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0x22), "Higher priority layer should take precedence");
    }

    /// <summary>
    /// Verifies that deactivating high priority layer reveals low priority layer.
    /// </summary>
    [Test]
    public void DeactivatingHighPriority_RevealsLowPriority()
    {
        var bus = new MainBus();
        var lowMemory = new PhysicalMemory(PageSize, "LowPriorityRAM");
        var highMemory = new PhysicalMemory(PageSize, "HighPriorityRAM");
        var baseMemory = new PhysicalMemory(PageSize, "BaseRAM");
        lowMemory.AsSpan()[0x100] = 0x11;
        highMemory.AsSpan()[0x100] = 0x22;
        baseMemory.AsSpan()[0x100] = 0x00;
        var lowTarget = new RamTarget(lowMemory.Slice(0, PageSize));
        var highTarget = new RamTarget(highMemory.Slice(0, PageSize));
        var baseTarget = new RamTarget(baseMemory.Slice(0, PageSize));

        // Set up base mapping
        bus.MapPage(0, new(0, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, baseTarget, 0));
        bus.SaveBaseMapping(0);

        // Create and activate layers
        var lowLayer = bus.CreateLayer("LowPriority", priority: 5);
        bus.AddLayeredMapping(new(0x0000, PageSize, lowLayer, 1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, lowTarget, 0));

        var highLayer = bus.CreateLayer("HighPriority", priority: 10);
        bus.AddLayeredMapping(new(0x0000, PageSize, highLayer, 2, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, highTarget, 0));

        bus.ActivateLayer("LowPriority");
        bus.ActivateLayer("HighPriority");

        // Deactivate high priority
        bus.DeactivateLayer("HighPriority");

        // Low priority should now be effective
        var access = CreateTestAccess(0x0100, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0x11), "Low priority layer should be revealed");
    }

    /// <summary>
    /// Verifies that GetEffectiveMapping returns the correct entry.
    /// </summary>
    [Test]
    public void GetEffectiveMapping_ReturnsCorrectEntry()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapPage(0, new(42, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target, 0));

        var entry = bus.GetEffectiveMapping(0x0100);

        Assert.That(entry.DeviceId, Is.EqualTo(42));
    }

    /// <summary>
    /// Verifies that GetAllMappingsAt returns all mappings at an address.
    /// </summary>
    [Test]
    public void GetAllMappingsAt_ReturnsAllMappings()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        // Create two layers with mappings at the same address
        var layer1 = bus.CreateLayer("Layer1", priority: 5);
        var layer2 = bus.CreateLayer("Layer2", priority: 10);

        bus.AddLayeredMapping(new(0x0000, PageSize, layer1, 1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, target, 0));
        bus.AddLayeredMapping(new(0x0000, PageSize, layer2, 2, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, target, 0));

        var mappings = bus.GetAllMappingsAt(0x0100).ToList();

        Assert.That(mappings, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Verifies that GetLayersAt returns layers in priority order.
    /// </summary>
    [Test]
    public void GetLayersAt_ReturnsLayersInPriorityOrder()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        // Create layers with different priorities
        var layer1 = bus.CreateLayer("Layer1", priority: 5);
        var layer2 = bus.CreateLayer("Layer2", priority: 15);
        var layer3 = bus.CreateLayer("Layer3", priority: 10);

        bus.AddLayeredMapping(new(0x0000, PageSize, layer1, 1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, target, 0));
        bus.AddLayeredMapping(new(0x0000, PageSize, layer2, 2, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, target, 0));
        bus.AddLayeredMapping(new(0x0000, PageSize, layer3, 3, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, target, 0));

        var layers = bus.GetLayersAt(0x0100).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(layers, Has.Count.EqualTo(3));
            Assert.That(layers[0].Priority, Is.EqualTo(15), "Highest priority first");
            Assert.That(layers[1].Priority, Is.EqualTo(10), "Medium priority second");
            Assert.That(layers[2].Priority, Is.EqualTo(5), "Lowest priority last");
        });
    }

    /// <summary>
    /// Verifies that SetLayerPermissions updates permissions.
    /// </summary>
    [Test]
    public void SetLayerPermissions_UpdatesPermissions()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        // Set up base mapping
        bus.MapPage(0, new(0, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target, 0));
        bus.SaveBaseMapping(0);

        // Create layer with ReadWrite permissions
        var layer = bus.CreateLayer("TestLayer", priority: 10);
        bus.AddLayeredMapping(new(0x0000, PageSize, layer, 1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target, 0));
        bus.ActivateLayer("TestLayer");

        // Change to ReadOnly
        bus.SetLayerPermissions("TestLayer", PagePerms.Read);

        // Verify permissions changed
        var entry = bus.GetEffectiveMapping(0x0000);
        Assert.That(entry.Perms, Is.EqualTo(PagePerms.Read));
    }

    /// <summary>
    /// Verifies that SetLayerPermissions throws for non-existent layer.
    /// </summary>
    [Test]
    public void SetLayerPermissions_NonExistentLayer_ThrowsKeyNotFoundException()
    {
        var bus = new MainBus();

        Assert.Throws<KeyNotFoundException>(() => bus.SetLayerPermissions("NonExistent", PagePerms.Read));
    }

    /// <summary>
    /// Verifies that AddLayeredMapping throws for non-existent layer.
    /// </summary>
    [Test]
    public void AddLayeredMapping_NonExistentLayer_ThrowsArgumentException()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var fakeLayer = new MappingLayer("FakeLayer", 10, false);

        Assert.Throws<ArgumentException>(() => bus.AddLayeredMapping(
            new(0x0000, PageSize, fakeLayer, 1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, target, 0)));
    }

    /// <summary>
    /// Verifies that AddLayeredMapping throws for non-page-aligned addresses.
    /// </summary>
    [Test]
    public void AddLayeredMapping_NonAlignedAddress_ThrowsArgumentException()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var layer = bus.CreateLayer("TestLayer", priority: 10);

        Assert.Throws<ArgumentException>(() => bus.AddLayeredMapping(
            new(0x0100, PageSize, layer, 1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, target, 0)));
    }

    /// <summary>
    /// Verifies that SaveBaseMapping stores the current mapping for restoration.
    /// </summary>
    [Test]
    public void SaveBaseMapping_StoresCurrentMapping()
    {
        var bus = new MainBus();
        var baseMemory = new PhysicalMemory(PageSize, "BaseRAM");
        var overlayMemory = new PhysicalMemory(PageSize, "OverlayRAM");
        baseMemory.AsSpan()[0x100] = 0x11;
        overlayMemory.AsSpan()[0x100] = 0x22;
        var baseTarget = new RamTarget(baseMemory.Slice(0, PageSize));
        var overlayTarget = new RamTarget(overlayMemory.Slice(0, PageSize));

        // Set up base mapping and save it
        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, baseTarget, 0));
        bus.SaveBaseMapping(0);

        // Create overlay
        var layer = bus.CreateLayer("Overlay", priority: 10);
        bus.AddLayeredMapping(new(0x0000, PageSize, layer, 2, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, overlayTarget, 0));

        // Activate and then deactivate
        bus.ActivateLayer("Overlay");
        bus.DeactivateLayer("Overlay");

        // Base should be restored
        var access = CreateTestAccess(0x0100, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0x11), "Base mapping should be restored after deactivation");
    }

    /// <summary>
    /// Verifies that SaveBaseMappingRange stores a range of mappings.
    /// </summary>
    [Test]
    public void SaveBaseMappingRange_StoresRangeOfMappings()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize * 2, "TestRAM");
        var target = new RamTarget(memory.Slice(0, (uint)(PageSize * 2)));

        bus.MapPageRange(0, 2, 1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target, 0);
        bus.SaveBaseMappingRange(0, 2);

        // Verify both pages saved (create overlay and restore)
        var overlayMemory = new PhysicalMemory(PageSize * 2, "OverlayRAM");
        var overlayTarget = new RamTarget(overlayMemory.Slice(0, (uint)(PageSize * 2)));

        var layer = bus.CreateLayer("Overlay", priority: 10);
        bus.AddLayeredMapping(new(0x0000, PageSize * 2, layer, 2, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, overlayTarget, 0));
        bus.ActivateLayer("Overlay");
        bus.DeactivateLayer("Overlay");

        // Both pages should be restored
        Assert.Multiple(() =>
        {
            Assert.That(bus.GetPageEntry(0x0000).DeviceId, Is.EqualTo(1), "Page 0 should be restored");
            Assert.That(bus.GetPageEntry(0x1000).DeviceId, Is.EqualTo(1), "Page 1 should be restored");
        });
    }

    /// <summary>
    /// Verifies multi-page overlay mappings work correctly.
    /// </summary>
    [Test]
    public void LayeredMapping_MultiplePages_MapsCorrectly()
    {
        var bus = new MainBus();
        var baseMemory = new PhysicalMemory(PageSize * 4, "BaseRAM");
        var overlayMemory = new PhysicalMemory(PageSize * 4, "OverlayRAM");

        // Set different values in each page
        baseMemory.AsSpan()[0x000] = 0x10; // Page 0
        baseMemory.AsSpan()[0x1000] = 0x11; // Page 1
        baseMemory.AsSpan()[0x2000] = 0x12; // Page 2
        baseMemory.AsSpan()[0x3000] = 0x13; // Page 3

        overlayMemory.AsSpan()[0x000] = 0x20;
        overlayMemory.AsSpan()[0x1000] = 0x21;
        overlayMemory.AsSpan()[0x2000] = 0x22;
        overlayMemory.AsSpan()[0x3000] = 0x23;

        var baseTarget = new RamTarget(baseMemory.Slice(0, (uint)(PageSize * 4)));
        var overlayTarget = new RamTarget(overlayMemory.Slice(0, (uint)(PageSize * 4)));

        // Set up base mapping
        bus.MapPageRange(0, 4, 1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, baseTarget, 0);
        bus.SaveBaseMappingRange(0, 4);

        // Create overlay for pages 1-2 only
        var layer = bus.CreateLayer("PartialOverlay", priority: 10);
        bus.AddLayeredMapping(new(
            VirtualBase: 0x1000, // Page 1
            Size: PageSize * 2, // Pages 1-2
            Layer: layer,
            DeviceId: 2,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadWrite,
            Caps: TargetCaps.SupportsPeek,
            Target: overlayTarget,
            PhysBase: 0x1000)); // Offset in overlay memory

        bus.ActivateLayer("PartialOverlay");

        Assert.Multiple(() =>
        {
            Assert.That(bus.Read8(CreateTestAccess(0x0000, AccessIntent.DataRead)), Is.EqualTo(0x10), "Page 0: Base");
            Assert.That(bus.Read8(CreateTestAccess(0x1000, AccessIntent.DataRead)), Is.EqualTo(0x21), "Page 1: Overlay");
            Assert.That(bus.Read8(CreateTestAccess(0x2000, AccessIntent.DataRead)), Is.EqualTo(0x22), "Page 2: Overlay");
            Assert.That(bus.Read8(CreateTestAccess(0x3000, AccessIntent.DataRead)), Is.EqualTo(0x13), "Page 3: Base");
        });
    }

    /// <summary>
    /// Verifies that LayeredMapping helper methods work correctly.
    /// </summary>
    [Test]
    public void LayeredMapping_HelperMethods_WorkCorrectly()
    {
        var layer = new MappingLayer("Test", 10, false);
        var mapping = new LayeredMapping(
            VirtualBase: 0x4000,
            Size: 0x3000, // 3 pages
            Layer: layer,
            DeviceId: 1,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadWrite,
            Caps: TargetCaps.None,
            Target: null!,
            PhysBase: 0);

        Assert.Multiple(() =>
        {
            Assert.That(mapping.VirtualEnd, Is.EqualTo(0x7000u), "VirtualEnd should be VirtualBase + Size");
            Assert.That(mapping.ContainsAddress(0x3FFF), Is.False, "Address before range");
            Assert.That(mapping.ContainsAddress(0x4000), Is.True, "Address at start");
            Assert.That(mapping.ContainsAddress(0x5000), Is.True, "Address in middle");
            Assert.That(mapping.ContainsAddress(0x6FFF), Is.True, "Address at end");
            Assert.That(mapping.ContainsAddress(0x7000), Is.False, "Address after range");
            Assert.That(mapping.GetStartPage(12), Is.EqualTo(4), "Start page index");
            Assert.That(mapping.GetPageCount(12), Is.EqualTo(3), "Page count");
        });
    }

    /// <summary>
    /// Verifies that MappingLayer WithActive creates correct copy.
    /// </summary>
    [Test]
    public void MappingLayer_WithActive_CreatesCorrectCopy()
    {
        var layer = new MappingLayer("Test", 10, false);

        var activeLayer = layer.WithActive(true);

        Assert.Multiple(() =>
        {
            Assert.That(activeLayer.Name, Is.EqualTo("Test"));
            Assert.That(activeLayer.Priority, Is.EqualTo(10));
            Assert.That(activeLayer.IsActive, Is.True);
            Assert.That(layer.IsActive, Is.False, "Original should be unchanged");
        });
    }

    /// <summary>
    /// Simulates Language Card behavior: ROM/RAM overlay at D000-FFFF.
    /// </summary>
    [Test]
    public void LanguageCardSimulation_RomRamOverlay()
    {
        var bus = new MainBus();

        // Create ROM (12KB: D000-FFFF)
        var romMemory = new PhysicalMemory(0x3000, "ROM");
        for (int i = 0; i < 0x3000; i++)
        {
            romMemory.AsSpan()[i] = 0xFF; // ROM typically reads as FF
        }

        var romTarget = new RomTarget(romMemory.Slice(0, 0x3000));

        // Create Language Card RAM (12KB)
        var lcRamMemory = new PhysicalMemory(0x3000, "LanguageCardRAM");
        for (int i = 0; i < 0x3000; i++)
        {
            lcRamMemory.AsSpan()[i] = 0x00; // RAM starts as 00
        }

        var lcRamTarget = new RamTarget(lcRamMemory.Slice(0, 0x3000));

        // Map ROM as base at D000-FFFF (3 pages: D, E, F)
        bus.MapPageRange(0xD, 3, 1, RegionTag.Rom, PagePerms.ReadExecute, TargetCaps.SupportsPeek, romTarget, 0);
        bus.SaveBaseMappingRange(0xD, 3);

        // Create Language Card layer
        var lcLayer = bus.CreateLayer("LanguageCard", priority: 10);
        bus.AddLayeredMapping(new(
            VirtualBase: 0xD000,
            Size: 0x3000,
            Layer: lcLayer,
            DeviceId: 2,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.All,
            Caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke | TargetCaps.SupportsWide,
            Target: lcRamTarget,
            PhysBase: 0));

        // Initially, ROM should be visible
        var accessD000 = CreateTestAccess(0xD000, AccessIntent.DataRead);
        Assert.That(bus.Read8(accessD000), Is.EqualTo(0xFF), "ROM should be visible initially");

        // Activate Language Card - RAM should now be visible
        bus.ActivateLayer("LanguageCard");
        Assert.That(bus.Read8(accessD000), Is.EqualTo(0x00), "RAM should be visible after LC activation");

        // Write to Language Card RAM
        var writeAccess = CreateTestAccess(0xD000, AccessIntent.DataWrite);
        bus.Write8(writeAccess, 0x42);
        Assert.That(bus.Read8(accessD000), Is.EqualTo(0x42), "RAM write should work");

        // Deactivate Language Card - ROM should be visible again
        bus.DeactivateLayer("LanguageCard");
        Assert.That(bus.Read8(accessD000), Is.EqualTo(0xFF), "ROM should be visible after LC deactivation");

        // Reactivate - RAM value should be preserved
        bus.ActivateLayer("LanguageCard");
        Assert.That(bus.Read8(accessD000), Is.EqualTo(0x42), "RAM value should be preserved");
    }

    /// <summary>
    /// Helper method to create test bus access structures.
    /// </summary>
    private static BusAccess CreateTestAccess(
        Addr address,
        AccessIntent intent,
        BusAccessMode mode = BusAccessMode.Decomposed,
        byte widthBits = 8,
        AccessFlags flags = AccessFlags.None)
    {
        return new(
            Address: address,
            Value: 0,
            WidthBits: widthBits,
            Mode: mode,
            EmulationFlag: mode == BusAccessMode.Decomposed,
            Intent: intent,
            SourceId: 0,
            Cycle: 0,
            Flags: flags);
    }
}