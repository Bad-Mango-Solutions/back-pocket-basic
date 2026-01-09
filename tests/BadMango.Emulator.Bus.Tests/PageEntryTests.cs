// <copyright file="PageEntryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="PageEntry"/> struct.
/// </summary>
[TestFixture]
public class PageEntryTests
{
    /// <summary>
    /// Verifies that PageEntry can be created with all properties.
    /// </summary>
    [Test]
    public void PageEntry_CanBeCreatedWithProperties()
    {
        var mockTarget = new Mock<IBusTarget>();
        mockTarget.Setup(t => t.Capabilities).Returns(TargetCaps.SupportsPeek | TargetCaps.SupportsWide);

        var entry = new PageEntry(
            DeviceId: 1,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadWrite,
            Caps: TargetCaps.SupportsPeek | TargetCaps.SupportsWide,
            Target: mockTarget.Object,
            PhysicalBase: 0x10000u);

        Assert.Multiple(() =>
        {
            Assert.That(entry.DeviceId, Is.EqualTo(1));
            Assert.That(entry.RegionTag, Is.EqualTo(RegionTag.Ram));
            Assert.That(entry.Perms, Is.EqualTo(PagePerms.ReadWrite));
            Assert.That(entry.Caps, Is.EqualTo(TargetCaps.SupportsPeek | TargetCaps.SupportsWide));
            Assert.That(entry.Target, Is.SameAs(mockTarget.Object));
            Assert.That(entry.PhysicalBase, Is.EqualTo(0x10000u));
        });
    }

    /// <summary>
    /// Verifies SupportsPeek returns true when flag is set.
    /// </summary>
    [Test]
    public void PageEntry_SupportsPeek_TrueWhenFlagSet()
    {
        var entry = CreateEntry(TargetCaps.SupportsPeek);
        Assert.That(entry.SupportsPeek, Is.True);
    }

    /// <summary>
    /// Verifies SupportsPeek returns false when flag is not set.
    /// </summary>
    [Test]
    public void PageEntry_SupportsPeek_FalseWhenFlagNotSet()
    {
        var entry = CreateEntry(TargetCaps.None);
        Assert.That(entry.SupportsPeek, Is.False);
    }

    /// <summary>
    /// Verifies SupportsPoke returns true when flag is set.
    /// </summary>
    [Test]
    public void PageEntry_SupportsPoke_TrueWhenFlagSet()
    {
        var entry = CreateEntry(TargetCaps.SupportsPoke);
        Assert.That(entry.SupportsPoke, Is.True);
    }

    /// <summary>
    /// Verifies SupportsWide returns true when flag is set.
    /// </summary>
    [Test]
    public void PageEntry_SupportsWide_TrueWhenFlagSet()
    {
        var entry = CreateEntry(TargetCaps.SupportsWide);
        Assert.That(entry.SupportsWide, Is.True);
    }

    /// <summary>
    /// Verifies HasSideEffects returns true when flag is set.
    /// </summary>
    [Test]
    public void PageEntry_HasSideEffects_TrueWhenFlagSet()
    {
        var entry = CreateEntry(TargetCaps.HasSideEffects);
        Assert.That(entry.HasSideEffects, Is.True);
    }

    /// <summary>
    /// Verifies IsTimingSensitive returns true when flag is set.
    /// </summary>
    [Test]
    public void PageEntry_IsTimingSensitive_TrueWhenFlagSet()
    {
        var entry = CreateEntry(TargetCaps.TimingSensitive);
        Assert.That(entry.IsTimingSensitive, Is.True);
    }

    /// <summary>
    /// Verifies CanRead returns true when Read permission is set.
    /// </summary>
    [Test]
    public void PageEntry_CanRead_TrueWhenReadPermSet()
    {
        var entry = CreateEntryWithPerms(PagePerms.Read);
        Assert.That(entry.CanRead, Is.True);
    }

    /// <summary>
    /// Verifies CanRead returns false when Read permission is not set.
    /// </summary>
    [Test]
    public void PageEntry_CanRead_FalseWhenReadPermNotSet()
    {
        var entry = CreateEntryWithPerms(PagePerms.None);
        Assert.That(entry.CanRead, Is.False);
    }

    /// <summary>
    /// Verifies CanWrite returns true when Write permission is set.
    /// </summary>
    [Test]
    public void PageEntry_CanWrite_TrueWhenWritePermSet()
    {
        var entry = CreateEntryWithPerms(PagePerms.Write);
        Assert.That(entry.CanWrite, Is.True);
    }

    /// <summary>
    /// Verifies CanWrite returns false when Write permission is not set.
    /// </summary>
    [Test]
    public void PageEntry_CanWrite_FalseWhenWritePermNotSet()
    {
        var entry = CreateEntryWithPerms(PagePerms.Read);
        Assert.That(entry.CanWrite, Is.False);
    }

    /// <summary>
    /// Verifies CanExecute returns true when Execute permission is set.
    /// </summary>
    [Test]
    public void PageEntry_CanExecute_TrueWhenExecutePermSet()
    {
        var entry = CreateEntryWithPerms(PagePerms.Execute);
        Assert.That(entry.CanExecute, Is.True);
    }

    /// <summary>
    /// Verifies CanExecute returns false when Execute permission is not set.
    /// </summary>
    [Test]
    public void PageEntry_CanExecute_FalseWhenExecutePermNotSet()
    {
        var entry = CreateEntryWithPerms(PagePerms.ReadWrite);
        Assert.That(entry.CanExecute, Is.False);
    }

    /// <summary>
    /// Verifies IsNx returns true when Execute permission is not set.
    /// </summary>
    [Test]
    public void PageEntry_IsNx_TrueWhenExecutePermNotSet()
    {
        var entry = CreateEntryWithPerms(PagePerms.ReadWrite);
        Assert.That(entry.IsNx, Is.True);
    }

    /// <summary>
    /// Verifies IsNx returns false when Execute permission is set.
    /// </summary>
    [Test]
    public void PageEntry_IsNx_FalseWhenExecutePermSet()
    {
        var entry = CreateEntryWithPerms(PagePerms.All);
        Assert.That(entry.IsNx, Is.False);
    }

    /// <summary>
    /// Verifies record equality works correctly.
    /// </summary>
    [Test]
    public void PageEntry_RecordEquality_Works()
    {
        var mockTarget = new Mock<IBusTarget>();

        var entry1 = new PageEntry(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, mockTarget.Object, 0x1000u);
        var entry2 = new PageEntry(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, mockTarget.Object, 0x1000u);
        var entry3 = new PageEntry(2, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, mockTarget.Object, 0x1000u);

        Assert.Multiple(() =>
        {
            Assert.That(entry1, Is.EqualTo(entry2));
            Assert.That(entry1, Is.Not.EqualTo(entry3));
        });
    }

    /// <summary>
    /// Verifies that PageEntry has default privilege levels of Ring0.
    /// </summary>
    [Test]
    public void PageEntry_PrivilegeLevels_DefaultToRing0()
    {
        var mockTarget = new Mock<IBusTarget>();
        var entry = new PageEntry(1, RegionTag.Ram, PagePerms.All, TargetCaps.None, mockTarget.Object, 0);

        Assert.Multiple(() =>
        {
            Assert.That(entry.MinReadPrivilege, Is.EqualTo(PrivilegeLevel.Ring0));
            Assert.That(entry.MinWritePrivilege, Is.EqualTo(PrivilegeLevel.Ring0));
            Assert.That(entry.MinExecutePrivilege, Is.EqualTo(PrivilegeLevel.Ring0));
            Assert.That(entry.IsSealed, Is.False);
        });
    }

    /// <summary>
    /// Verifies that PageEntry can be created with custom privilege levels.
    /// </summary>
    [Test]
    public void PageEntry_CanBeCreatedWithCustomPrivilegeLevels()
    {
        var mockTarget = new Mock<IBusTarget>();
        var entry = new PageEntry(
            DeviceId: 1,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.All,
            Caps: TargetCaps.None,
            Target: mockTarget.Object,
            PhysicalBase: 0,
            MinReadPrivilege: PrivilegeLevel.Ring3,
            MinWritePrivilege: PrivilegeLevel.Ring1,
            MinExecutePrivilege: PrivilegeLevel.Ring2,
            IsSealed: true);

        Assert.Multiple(() =>
        {
            Assert.That(entry.MinReadPrivilege, Is.EqualTo(PrivilegeLevel.Ring3));
            Assert.That(entry.MinWritePrivilege, Is.EqualTo(PrivilegeLevel.Ring1));
            Assert.That(entry.MinExecutePrivilege, Is.EqualTo(PrivilegeLevel.Ring2));
            Assert.That(entry.IsSealed, Is.True);
        });
    }

    /// <summary>
    /// Verifies that multiple page entries can share slices of one physical memory.
    /// </summary>
    [Test]
    public void PageEntry_MultipleEntries_CanSharePhysicalMemorySlices()
    {
        // Create a single physical memory pool
        var physicalMemory = new PhysicalMemory(8192, "Main RAM");

        // Create two RamTargets pointing to overlapping slices
        var ramTarget1 = new RamTarget(physicalMemory.Slice(0, 4096)); // Page 0
        var ramTarget2 = new RamTarget(physicalMemory.Slice(2048, 4096)); // Overlaps with page 0

        // Create page entries for each target
        var entry1 = new PageEntry(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, ramTarget1, 0);
        var entry2 = new PageEntry(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, ramTarget2, 0);

        var access = new BusAccess(0, 0, 8, BusAccessMode.Decomposed, true, AccessIntent.DataWrite, 0, 0, AccessFlags.None);

        // Write via entry1's target at offset 2048
        entry1.Target.Write8(2048, 0xAA, in access);

        // Read via entry2's target at offset 0 (which maps to physical offset 2048)
        byte value = entry2.Target.Read8(0, in access);

        Assert.That(value, Is.EqualTo(0xAA), "Multiple page entries should share the same physical memory");
    }

    private static PageEntry CreateEntry(TargetCaps caps)
    {
        var mockTarget = new Mock<IBusTarget>();
        return new(1, RegionTag.Ram, PagePerms.All, caps, mockTarget.Object, 0);
    }

    private static PageEntry CreateEntryWithPerms(PagePerms perms)
    {
        var mockTarget = new Mock<IBusTarget>();
        return new(1, RegionTag.Ram, perms, TargetCaps.None, mockTarget.Object, 0);
    }
}