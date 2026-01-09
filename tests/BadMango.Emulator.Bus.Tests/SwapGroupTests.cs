// <copyright file="SwapGroupTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for swap group support in <see cref="MainBus"/>.
/// </summary>
[TestFixture]
public class SwapGroupTests
{
    private const int PageSize = 4096;

    /// <summary>
    /// Verifies that a swap group can be created with a name and address range.
    /// </summary>
    [Test]
    public void CreateSwapGroup_CreatesGroupWithCorrectProperties()
    {
        var bus = new MainBus();

        uint groupId = bus.CreateSwapGroup("TestGroup", virtualBase: 0xD000, size: 0x1000);

        Assert.Multiple(() =>
        {
            Assert.That(groupId, Is.EqualTo(0u), "First group ID should be 0");
            Assert.That(bus.GetSwapGroupId("TestGroup"), Is.EqualTo(groupId));
            Assert.That(bus.GetActiveSwapVariant(groupId), Is.Null, "No variant active by default");
        });
    }

    /// <summary>
    /// Verifies that creating a swap group with a duplicate name throws.
    /// </summary>
    [Test]
    public void CreateSwapGroup_DuplicateName_ThrowsArgumentException()
    {
        var bus = new MainBus();
        bus.CreateSwapGroup("TestGroup", virtualBase: 0xD000, size: 0x1000);

        Assert.Throws<ArgumentException>(() => bus.CreateSwapGroup("TestGroup", virtualBase: 0xE000, size: 0x1000));
    }

    /// <summary>
    /// Verifies that creating a swap group with non-page-aligned address throws.
    /// </summary>
    [Test]
    public void CreateSwapGroup_NonAlignedAddress_ThrowsArgumentException()
    {
        var bus = new MainBus();

        Assert.Throws<ArgumentException>(() => bus.CreateSwapGroup("TestGroup", virtualBase: 0xD100, size: 0x1000));
    }

    /// <summary>
    /// Verifies that creating a swap group with non-page-aligned size throws.
    /// </summary>
    [Test]
    public void CreateSwapGroup_NonAlignedSize_ThrowsArgumentException()
    {
        var bus = new MainBus();

        Assert.Throws<ArgumentException>(() => bus.CreateSwapGroup("TestGroup", virtualBase: 0xD000, size: 0x100));
    }

    /// <summary>
    /// Verifies that creating a swap group exceeding address space throws.
    /// </summary>
    [Test]
    public void CreateSwapGroup_ExceedsAddressSpace_ThrowsArgumentOutOfRangeException()
    {
        var bus = new MainBus(); // 16-bit = 64KB

        Assert.Throws<ArgumentOutOfRangeException>(() => bus.CreateSwapGroup("TestGroup", virtualBase: 0xF000, size: 0x2000));
    }

    /// <summary>
    /// Verifies that GetSwapGroupId throws for non-existent group.
    /// </summary>
    [Test]
    public void GetSwapGroupId_NonExistentGroup_ThrowsKeyNotFoundException()
    {
        var bus = new MainBus();

        Assert.Throws<KeyNotFoundException>(() => bus.GetSwapGroupId("NonExistent"));
    }

    /// <summary>
    /// Verifies that AddSwapVariant adds a variant to the group.
    /// </summary>
    [Test]
    public void AddSwapVariant_AddsVariantToGroup()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "Bank1");
        var target = new RamTarget(memory.Slice(0, PageSize));

        uint groupId = bus.CreateSwapGroup("TestGroup", virtualBase: 0xD000, size: 0x1000);

        Assert.DoesNotThrow(() => bus.AddSwapVariant(groupId, "BANK1", target, physBase: 0, perms: PagePerms.ReadWrite));
    }

    /// <summary>
    /// Verifies that AddSwapVariant throws for non-existent group.
    /// </summary>
    [Test]
    public void AddSwapVariant_NonExistentGroup_ThrowsKeyNotFoundException()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "Bank1");
        var target = new RamTarget(memory.Slice(0, PageSize));

        Assert.Throws<KeyNotFoundException>(() => bus.AddSwapVariant(999, "BANK1", target, physBase: 0, perms: PagePerms.ReadWrite));
    }

    /// <summary>
    /// Verifies that AddSwapVariant throws for duplicate variant name.
    /// </summary>
    [Test]
    public void AddSwapVariant_DuplicateName_ThrowsArgumentException()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "Bank1");
        var target = new RamTarget(memory.Slice(0, PageSize));

        uint groupId = bus.CreateSwapGroup("TestGroup", virtualBase: 0xD000, size: 0x1000);
        bus.AddSwapVariant(groupId, "BANK1", target, physBase: 0, perms: PagePerms.ReadWrite);

        Assert.Throws<ArgumentException>(() => bus.AddSwapVariant(groupId, "BANK1", target, physBase: 0, perms: PagePerms.ReadWrite));
    }

    /// <summary>
    /// Verifies that SelectSwapVariant switches the active variant.
    /// </summary>
    [Test]
    public void SelectSwapVariant_SwitchesActiveVariant()
    {
        var bus = new MainBus();
        var bank1Memory = new PhysicalMemory(PageSize, "Bank1");
        var bank2Memory = new PhysicalMemory(PageSize, "Bank2");
        bank1Memory.AsSpan()[0] = 0x11;
        bank2Memory.AsSpan()[0] = 0x22;
        var bank1Target = new RamTarget(bank1Memory.Slice(0, PageSize));
        var bank2Target = new RamTarget(bank2Memory.Slice(0, PageSize));

        // Set up initial page mapping
        bus.MapPage(0xD, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, bank1Target, 0));

        uint groupId = bus.CreateSwapGroup("LanguageCard", virtualBase: 0xD000, size: 0x1000);
        bus.AddSwapVariant(groupId, "BANK1", bank1Target, physBase: 0, perms: PagePerms.ReadWrite);
        bus.AddSwapVariant(groupId, "BANK2", bank2Target, physBase: 0, perms: PagePerms.ReadWrite);

        // Select BANK1 and verify
        bus.SelectSwapVariant(groupId, "BANK1");
        Assert.That(bus.GetActiveSwapVariant(groupId), Is.EqualTo("BANK1"));

        var access = CreateTestAccess(0xD000, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0x11), "BANK1 should be active");

        // Switch to BANK2 and verify
        bus.SelectSwapVariant(groupId, "BANK2");
        Assert.That(bus.GetActiveSwapVariant(groupId), Is.EqualTo("BANK2"));
        Assert.That(bus.Read8(access), Is.EqualTo(0x22), "BANK2 should be active after switch");
    }

    /// <summary>
    /// Verifies that SelectSwapVariant throws for non-existent group.
    /// </summary>
    [Test]
    public void SelectSwapVariant_NonExistentGroup_ThrowsKeyNotFoundException()
    {
        var bus = new MainBus();

        Assert.Throws<KeyNotFoundException>(() => bus.SelectSwapVariant(999, "BANK1"));
    }

    /// <summary>
    /// Verifies that SelectSwapVariant throws for non-existent variant.
    /// </summary>
    [Test]
    public void SelectSwapVariant_NonExistentVariant_ThrowsKeyNotFoundException()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "Bank1");
        var target = new RamTarget(memory.Slice(0, PageSize));

        uint groupId = bus.CreateSwapGroup("TestGroup", virtualBase: 0xD000, size: 0x1000);
        bus.AddSwapVariant(groupId, "BANK1", target, physBase: 0, perms: PagePerms.ReadWrite);

        Assert.Throws<KeyNotFoundException>(() => bus.SelectSwapVariant(groupId, "NonExistent"));
    }

    /// <summary>
    /// Verifies that GetActiveSwapVariant throws for non-existent group.
    /// </summary>
    [Test]
    public void GetActiveSwapVariant_NonExistentGroup_ThrowsKeyNotFoundException()
    {
        var bus = new MainBus();

        Assert.Throws<KeyNotFoundException>(() => bus.GetActiveSwapVariant(999));
    }

    /// <summary>
    /// Verifies that swap groups can have multiple pages.
    /// </summary>
    [Test]
    public void SwapGroup_MultiplePages_SwitchesAllPages()
    {
        var bus = new MainBus();
        var bank1Memory = new PhysicalMemory(PageSize * 4, "Bank1");
        var bank2Memory = new PhysicalMemory(PageSize * 4, "Bank2");

        // Mark each page distinctly
        bank1Memory.AsSpan()[0x0000] = 0x10; // Page 0xD
        bank1Memory.AsSpan()[0x1000] = 0x11; // Page 0xE
        bank1Memory.AsSpan()[0x2000] = 0x12; // Page 0xF
        bank1Memory.AsSpan()[0x3000] = 0x13; // Page 0x10 (this test uses larger range)

        bank2Memory.AsSpan()[0x0000] = 0x20;
        bank2Memory.AsSpan()[0x1000] = 0x21;
        bank2Memory.AsSpan()[0x2000] = 0x22;
        bank2Memory.AsSpan()[0x3000] = 0x23;

        var bank1Target = new RamTarget(bank1Memory.Slice(0, (uint)(PageSize * 4)));
        var bank2Target = new RamTarget(bank2Memory.Slice(0, (uint)(PageSize * 4)));

        // Set up initial mappings for D000-FFFF (3 pages: D, E, F)
        bus.MapPageRange(0xD, 3, 1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, bank1Target, 0);

        uint groupId = bus.CreateSwapGroup("LanguageCard", virtualBase: 0xD000, size: 0x3000);
        bus.AddSwapVariant(groupId, "BANK1", bank1Target, physBase: 0, perms: PagePerms.ReadWrite);
        bus.AddSwapVariant(groupId, "BANK2", bank2Target, physBase: 0, perms: PagePerms.ReadWrite);

        // Select BANK1
        bus.SelectSwapVariant(groupId, "BANK1");

        Assert.Multiple(() =>
        {
            Assert.That(bus.Read8(CreateTestAccess(0xD000, AccessIntent.DataRead)), Is.EqualTo(0x10), "Page D - BANK1");
            Assert.That(bus.Read8(CreateTestAccess(0xE000, AccessIntent.DataRead)), Is.EqualTo(0x11), "Page E - BANK1");
            Assert.That(bus.Read8(CreateTestAccess(0xF000, AccessIntent.DataRead)), Is.EqualTo(0x12), "Page F - BANK1");
        });

        // Switch to BANK2
        bus.SelectSwapVariant(groupId, "BANK2");

        Assert.Multiple(() =>
        {
            Assert.That(bus.Read8(CreateTestAccess(0xD000, AccessIntent.DataRead)), Is.EqualTo(0x20), "Page D - BANK2");
            Assert.That(bus.Read8(CreateTestAccess(0xE000, AccessIntent.DataRead)), Is.EqualTo(0x21), "Page E - BANK2");
            Assert.That(bus.Read8(CreateTestAccess(0xF000, AccessIntent.DataRead)), Is.EqualTo(0x22), "Page F - BANK2");
        });
    }

    /// <summary>
    /// Verifies that swap group preserves page metadata when switching.
    /// </summary>
    [Test]
    public void SwapGroup_PreservesPageMetadata()
    {
        var bus = new MainBus();
        var bank1Memory = new PhysicalMemory(PageSize, "Bank1");
        var bank2Memory = new PhysicalMemory(PageSize, "Bank2");
        var bank1Target = new RamTarget(bank1Memory.Slice(0, PageSize));
        var bank2Target = new RamTarget(bank2Memory.Slice(0, PageSize));

        // Set up initial mapping with specific device ID and region tag
        bus.MapPage(0xD, new(42, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, bank1Target, 0));

        uint groupId = bus.CreateSwapGroup("TestGroup", virtualBase: 0xD000, size: 0x1000);
        bus.AddSwapVariant(groupId, "BANK1", bank1Target, physBase: 0, perms: PagePerms.ReadWrite);
        bus.AddSwapVariant(groupId, "BANK2", bank2Target, physBase: 0, perms: PagePerms.ReadExecute);

        // Select BANK2
        bus.SelectSwapVariant(groupId, "BANK2");

        var entry = bus.GetPageEntry(0xD000);
        Assert.Multiple(() =>
        {
            Assert.That(entry.DeviceId, Is.EqualTo(42), "Device ID should be preserved");
            Assert.That(entry.RegionTag, Is.EqualTo(RegionTag.Ram), "Region tag should be preserved");
            Assert.That(entry.Target, Is.SameAs(bank2Target), "Target should be from variant");
            Assert.That(entry.Perms, Is.EqualTo(PagePerms.ReadExecute), "Permissions should be from variant");
        });
    }

    /// <summary>
    /// Verifies that multiple swap groups can be created independently.
    /// </summary>
    [Test]
    public void MultipleSwapGroups_OperateIndependently()
    {
        var bus = new MainBus();
        var group1Bank1Memory = new PhysicalMemory(PageSize, "Group1Bank1");
        var group1Bank2Memory = new PhysicalMemory(PageSize, "Group1Bank2");
        var group2Bank1Memory = new PhysicalMemory(PageSize, "Group2Bank1");
        var group2Bank2Memory = new PhysicalMemory(PageSize, "Group2Bank2");

        group1Bank1Memory.AsSpan()[0] = 0x11;
        group1Bank2Memory.AsSpan()[0] = 0x12;
        group2Bank1Memory.AsSpan()[0] = 0x21;
        group2Bank2Memory.AsSpan()[0] = 0x22;

        var g1b1 = new RamTarget(group1Bank1Memory.Slice(0, PageSize));
        var g1b2 = new RamTarget(group1Bank2Memory.Slice(0, PageSize));
        var g2b1 = new RamTarget(group2Bank1Memory.Slice(0, PageSize));
        var g2b2 = new RamTarget(group2Bank2Memory.Slice(0, PageSize));

        // Set up initial mappings
        bus.MapPage(0xD, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, g1b1, 0));
        bus.MapPage(0xE, new(2, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, g2b1, 0));

        // Create two separate swap groups
        uint group1Id = bus.CreateSwapGroup("Group1", virtualBase: 0xD000, size: 0x1000);
        uint group2Id = bus.CreateSwapGroup("Group2", virtualBase: 0xE000, size: 0x1000);

        bus.AddSwapVariant(group1Id, "BANK1", g1b1, physBase: 0, perms: PagePerms.ReadWrite);
        bus.AddSwapVariant(group1Id, "BANK2", g1b2, physBase: 0, perms: PagePerms.ReadWrite);
        bus.AddSwapVariant(group2Id, "BANK1", g2b1, physBase: 0, perms: PagePerms.ReadWrite);
        bus.AddSwapVariant(group2Id, "BANK2", g2b2, physBase: 0, perms: PagePerms.ReadWrite);

        // Set Group1 to BANK2, Group2 to BANK1
        bus.SelectSwapVariant(group1Id, "BANK2");
        bus.SelectSwapVariant(group2Id, "BANK1");

        Assert.Multiple(() =>
        {
            Assert.That(bus.GetActiveSwapVariant(group1Id), Is.EqualTo("BANK2"));
            Assert.That(bus.GetActiveSwapVariant(group2Id), Is.EqualTo("BANK1"));
            Assert.That(bus.Read8(CreateTestAccess(0xD000, AccessIntent.DataRead)), Is.EqualTo(0x12), "Group1 BANK2");
            Assert.That(bus.Read8(CreateTestAccess(0xE000, AccessIntent.DataRead)), Is.EqualTo(0x21), "Group2 BANK1");
        });

        // Now switch Group2 to BANK2 - Group1 should remain unchanged
        bus.SelectSwapVariant(group2Id, "BANK2");

        Assert.Multiple(() =>
        {
            Assert.That(bus.GetActiveSwapVariant(group1Id), Is.EqualTo("BANK2"), "Group1 should remain unchanged");
            Assert.That(bus.GetActiveSwapVariant(group2Id), Is.EqualTo("BANK2"), "Group2 should be updated");
            Assert.That(bus.Read8(CreateTestAccess(0xD000, AccessIntent.DataRead)), Is.EqualTo(0x12), "Group1 unchanged");
            Assert.That(bus.Read8(CreateTestAccess(0xE000, AccessIntent.DataRead)), Is.EqualTo(0x22), "Group2 updated");
        });
    }

    /// <summary>
    /// Simulates Language Card D000 bank switching pattern with two 4KB banks.
    /// </summary>
    [Test]
    public void LanguageCardSimulation_TwoBanksForD000()
    {
        var bus = new MainBus();

        // Create two 4KB banks for D000-DFFF (Language Card bank 1 and bank 2)
        var lcBank1Memory = new PhysicalMemory(PageSize, "LCBank1");
        var lcBank2Memory = new PhysicalMemory(PageSize, "LCBank2");

        // Initialize with different values to distinguish banks
        for (int i = 0; i < PageSize; i++)
        {
            lcBank1Memory.AsSpan()[i] = 0xB1; // Bank 1 pattern
            lcBank2Memory.AsSpan()[i] = 0xB2; // Bank 2 pattern
        }

        var lcBank1Target = new RamTarget(lcBank1Memory.Slice(0, PageSize));
        var lcBank2Target = new RamTarget(lcBank2Memory.Slice(0, PageSize));

        // Set up initial mapping (page 0xD = D000-DFFF)
        bus.MapPage(0xD, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek | TargetCaps.SupportsPoke, lcBank1Target, 0));

        // Create swap group for the D000 bank switching
        uint groupId = bus.CreateSwapGroup("D000Banks", virtualBase: 0xD000, size: 0x1000);
        bus.AddSwapVariant(groupId, "BANK1", lcBank1Target, physBase: 0, perms: PagePerms.ReadWrite);
        bus.AddSwapVariant(groupId, "BANK2", lcBank2Target, physBase: 0, perms: PagePerms.ReadWrite);

        // Initially select BANK1
        bus.SelectSwapVariant(groupId, "BANK1");
        Assert.That(bus.GetActiveSwapVariant(groupId), Is.EqualTo("BANK1"));

        // Read from multiple locations in D000 page - should all be BANK1 pattern
        var access = CreateTestAccess(0xD000, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0xB1), "Reading D000 should show BANK1");

        access = CreateTestAccess(0xD800, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0xB1), "Reading D800 should show BANK1");

        access = CreateTestAccess(0xDFFF, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0xB1), "Reading DFFF should show BANK1");

        // Write a value to BANK1
        var writeAccess = CreateTestAccess(0xD100, AccessIntent.DataWrite);
        bus.Write8(writeAccess, 0x42);
        Assert.That(lcBank1Memory.AsSpan()[0x100], Is.EqualTo(0x42), "Write should go to BANK1");

        // Switch to BANK2
        bus.SelectSwapVariant(groupId, "BANK2");
        Assert.That(bus.GetActiveSwapVariant(groupId), Is.EqualTo("BANK2"));

        // Read should now show BANK2 pattern
        access = CreateTestAccess(0xD000, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0xB2), "Reading D000 should show BANK2");

        // The value we wrote should NOT be visible in BANK2
        access = CreateTestAccess(0xD100, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0xB2), "D100 should show BANK2, not our written value");

        // Write to BANK2
        writeAccess = CreateTestAccess(0xD200, AccessIntent.DataWrite);
        bus.Write8(writeAccess, 0x99);
        Assert.That(lcBank2Memory.AsSpan()[0x200], Is.EqualTo(0x99), "Write should go to BANK2");

        // Switch back to BANK1 and verify our original write is still there
        bus.SelectSwapVariant(groupId, "BANK1");
        access = CreateTestAccess(0xD100, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0x42), "Our BANK1 write should still be there");

        // BANK2's write should NOT be visible
        access = CreateTestAccess(0xD200, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0xB1), "BANK2 write should not be visible in BANK1");
    }

    /// <summary>
    /// Verifies that swap group IDs are assigned sequentially.
    /// </summary>
    [Test]
    public void CreateSwapGroup_AssignsSequentialIds()
    {
        var bus = new MainBus();

        uint group1Id = bus.CreateSwapGroup("Group1", virtualBase: 0x0000, size: 0x1000);
        uint group2Id = bus.CreateSwapGroup("Group2", virtualBase: 0x1000, size: 0x1000);
        uint group3Id = bus.CreateSwapGroup("Group3", virtualBase: 0x2000, size: 0x1000);

        Assert.Multiple(() =>
        {
            Assert.That(group1Id, Is.EqualTo(0u));
            Assert.That(group2Id, Is.EqualTo(1u));
            Assert.That(group3Id, Is.EqualTo(2u));
        });
    }

    /// <summary>
    /// Verifies that swap variant can use different physical base addresses.
    /// </summary>
    [Test]
    public void SwapVariant_DifferentPhysicalBase_MapsCorrectly()
    {
        var bus = new MainBus();

        // Single large memory bank, but use different physical bases
        var memory = new PhysicalMemory(PageSize * 2, "Memory");
        memory.AsSpan()[0x0000] = 0xAA; // First 4KB
        memory.AsSpan()[0x1000] = 0xBB; // Second 4KB

        var target = new RamTarget(memory.Slice(0, (uint)(PageSize * 2)));

        bus.MapPage(0xD, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target, 0));

        uint groupId = bus.CreateSwapGroup("TestGroup", virtualBase: 0xD000, size: 0x1000);

        // BANK1 points to first 4KB, BANK2 points to second 4KB
        bus.AddSwapVariant(groupId, "BANK1", target, physBase: 0, perms: PagePerms.ReadWrite);
        bus.AddSwapVariant(groupId, "BANK2", target, physBase: 0x1000, perms: PagePerms.ReadWrite);

        bus.SelectSwapVariant(groupId, "BANK1");
        Assert.That(bus.Read8(CreateTestAccess(0xD000, AccessIntent.DataRead)), Is.EqualTo(0xAA), "BANK1 maps to first 4KB");

        bus.SelectSwapVariant(groupId, "BANK2");
        Assert.That(bus.Read8(CreateTestAccess(0xD000, AccessIntent.DataRead)), Is.EqualTo(0xBB), "BANK2 maps to second 4KB");
    }

    /// <summary>
    /// Verifies that swap variant permissions override page permissions.
    /// </summary>
    [Test]
    public void SwapVariant_PermissionsOverridePage()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "Memory");
        var target = new RamTarget(memory.Slice(0, PageSize));

        // Initial mapping with ReadWrite
        bus.MapPage(0xD, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target, 0));

        uint groupId = bus.CreateSwapGroup("TestGroup", virtualBase: 0xD000, size: 0x1000);

        // BANK1 is ReadWrite, BANK2 is ReadOnly
        bus.AddSwapVariant(groupId, "BANK1", target, physBase: 0, perms: PagePerms.ReadWrite);
        bus.AddSwapVariant(groupId, "BANK2", target, physBase: 0, perms: PagePerms.Read);

        // BANK1 should allow writes
        bus.SelectSwapVariant(groupId, "BANK1");
        var entry = bus.GetPageEntry(0xD000);
        Assert.That(entry.CanWrite, Is.True, "BANK1 should be writable");

        // BANK2 should be read-only
        bus.SelectSwapVariant(groupId, "BANK2");
        entry = bus.GetPageEntry(0xD000);
        Assert.That(entry.CanWrite, Is.False, "BANK2 should be read-only");

        // Verify write protection is enforced
        var writeAccess = CreateTestAccess(0xD000, AccessIntent.DataWrite);
        var result = bus.TryWrite8(writeAccess, 0x42);
        Assert.That(result.Failed, Is.True, "Write to read-only variant should fail");
        Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.Permission), "Should be permission fault");
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