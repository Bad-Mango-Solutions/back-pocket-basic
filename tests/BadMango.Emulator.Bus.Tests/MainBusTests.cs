// <copyright file="MainBusTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="MainBus"/> class.
/// </summary>
[TestFixture]
public class MainBusTests
{
    private const int DefaultAddressSpaceBits = 16;
    private const int PageSize = 4096;

    /// <summary>
    /// Verifies the bus is created with default 64KB address space.
    /// </summary>
    [Test]
    public void Constructor_DefaultAddressSpace_Has16Pages()
    {
        var bus = new MainBus();

        Assert.Multiple(() =>
        {
            Assert.That(bus.PageCount, Is.EqualTo(16), "64KB / 4KB = 16 pages");
            Assert.That(bus.PageShift, Is.EqualTo(12), "4KB pages = 12-bit shift");
            Assert.That(bus.PageMask, Is.EqualTo(0xFFFu), "4KB page mask");
        });
    }

    /// <summary>
    /// Verifies the bus is created with specified address space.
    /// </summary>
    [Test]
    public void Constructor_128KBAddressSpace_Has32Pages()
    {
        var bus = new MainBus(addressSpaceBits: 17);

        Assert.That(bus.PageCount, Is.EqualTo(32), "128KB / 4KB = 32 pages");
    }

    /// <summary>
    /// Verifies constructor throws for address space smaller than one page.
    /// </summary>
    [Test]
    public void Constructor_TooSmallAddressSpace_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MainBus(addressSpaceBits: 11));
    }

    /// <summary>
    /// Verifies constructor throws for address space larger than 32 bits.
    /// </summary>
    [Test]
    public void Constructor_TooBigAddressSpace_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MainBus(addressSpaceBits: 33));
    }

    /// <summary>
    /// Verifies MapPage correctly maps a page.
    /// </summary>
    [Test]
    public void MapPage_SinglePage_GetPageEntryReturnsCorrectEntry()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var entry = new PageEntry(
            DeviceId: 1,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadWrite,
            Caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke | TargetCaps.SupportsWide,
            Target: target,
            PhysicalBase: 0);

        bus.MapPage(0, entry);

        var retrievedEntry = bus.GetPageEntry(0x0000);
        Assert.Multiple(() =>
        {
            Assert.That(retrievedEntry.DeviceId, Is.EqualTo(1));
            Assert.That(retrievedEntry.RegionTag, Is.EqualTo(RegionTag.Ram));
            Assert.That(retrievedEntry.Target, Is.SameAs(target));
        });
    }

    /// <summary>
    /// Verifies MapPage throws for invalid page index.
    /// </summary>
    [Test]
    public void MapPage_InvalidPageIndex_ThrowsArgumentOutOfRangeException()
    {
        var bus = new MainBus();

        Assert.Throws<ArgumentOutOfRangeException>(() => bus.MapPage(100, default));
    }

    /// <summary>
    /// Verifies MapPageRange correctly maps multiple pages.
    /// </summary>
    [Test]
    public void MapPageRange_MultiplePages_AllPagesMapped()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize * 4, "TestRAM");
        var target = new RamTarget(memory.Slice(0, (uint)(PageSize * 4)));

        bus.MapPageRange(
            startPage: 0,
            pageCount: 4,
            deviceId: 1,
            regionTag: RegionTag.Ram,
            perms: PagePerms.ReadWrite,
            caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke,
            target: target,
            physicalBase: 0);

        Assert.Multiple(() =>
        {
            for (int i = 0; i < 4; i++)
            {
                var entry = bus.GetPageEntry((Addr)(i * PageSize));
                Assert.That(entry.DeviceId, Is.EqualTo(1), $"Page {i} DeviceId");
                Assert.That(entry.Target, Is.SameAs(target), $"Page {i} Target");
                Assert.That(entry.PhysicalBase, Is.EqualTo((Addr)(i * PageSize)), $"Page {i} PhysicalBase");
            }
        });
    }

    /// <summary>
    /// Verifies Read8 returns correct value from mapped RAM.
    /// </summary>
    [Test]
    public void Read8_MappedRam_ReturnsCorrectValue()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        memory.AsSpan()[0x100] = 0x42;
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.DataRead);
        byte value = bus.Read8(access);

        Assert.That(value, Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies Write8 stores value to mapped RAM.
    /// </summary>
    [Test]
    public void Write8_MappedRam_StoresValue()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPoke, target, 0));

        var access = CreateTestAccess(0x0200, AccessIntent.DataWrite);
        bus.Write8(access, 0x55);

        Assert.That(memory.AsSpan()[0x200], Is.EqualTo(0x55));
    }

    /// <summary>
    /// Verifies TryRead8 returns unmapped fault for unmapped address.
    /// </summary>
    [Test]
    public void TryRead8_UnmappedAddress_ReturnsUnmappedFault()
    {
        var bus = new MainBus();

        var access = CreateTestAccess(0x5000, AccessIntent.DataRead);
        var result = bus.TryRead8(access);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.Unmapped));
        });
    }

    /// <summary>
    /// Verifies TryWrite8 returns unmapped fault for unmapped address.
    /// </summary>
    [Test]
    public void TryWrite8_UnmappedAddress_ReturnsUnmappedFault()
    {
        var bus = new MainBus();

        var access = CreateTestAccess(0x5000, AccessIntent.DataWrite);
        var result = bus.TryWrite8(access, 0x00);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.Unmapped));
        });
    }

    /// <summary>
    /// Verifies TryRead8 returns permission fault when read not allowed.
    /// </summary>
    [Test]
    public void TryRead8_NoReadPermission_ReturnsPermissionFault()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestROM");
        var target = new RomTarget(memory.Slice(0, PageSize));

        // Map with no read permission (Write only)
        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.Write, TargetCaps.None, target, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.DataRead);
        var result = bus.TryRead8(access);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.Permission));
        });
    }

    /// <summary>
    /// Verifies TryWrite8 returns permission fault when write not allowed.
    /// </summary>
    [Test]
    public void TryWrite8_NoWritePermission_ReturnsPermissionFault()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestROM");
        var target = new RomTarget(memory.Slice(0, PageSize));

        // Map with read-only permission
        bus.MapPage(0, new(1, RegionTag.Rom, PagePerms.Read, TargetCaps.SupportsPeek, target, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.DataWrite);
        var result = bus.TryWrite8(access, 0x42);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.Permission));
        });
    }

    /// <summary>
    /// Verifies TryRead8 returns NX fault for instruction fetch on non-executable page in Atomic mode.
    /// </summary>
    [Test]
    public void TryRead8_InstructionFetchOnNxPage_AtomicMode_ReturnsNxFault()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        // Map with read/write but no execute
        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.InstructionFetch, BusAccessMode.Atomic);
        var result = bus.TryRead8(access);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.Nx));
        });
    }

    /// <summary>
    /// Verifies TryRead8 ignores NX in Decomposed mode (Apple II behavior).
    /// </summary>
    [Test]
    public void TryRead8_InstructionFetchOnNxPage_DecomposedMode_Succeeds()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        memory.AsSpan()[0x100] = 0xEA; // NOP opcode
        var target = new RamTarget(memory.Slice(0, PageSize));

        // Map with read/write but no execute
        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.InstructionFetch, BusAccessMode.Decomposed);
        var result = bus.TryRead8(access);

        Assert.Multiple(() =>
        {
            Assert.That(result.Ok, Is.True, "Decomposed mode should ignore NX");
            Assert.That(result.Value, Is.EqualTo(0xEA));
        });
    }

    /// <summary>
    /// Verifies Read16 decomposes in Decomposed mode.
    /// </summary>
    [Test]
    public void Read16_DecomposedMode_DecomposesIntoByteReads()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        memory.AsSpan()[0x100] = 0x34;  // Low byte
        memory.AsSpan()[0x101] = 0x12;  // High byte
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsWide, target, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.DataRead, BusAccessMode.Decomposed, 16);
        Word value = bus.Read16(access);

        Assert.That(value, Is.EqualTo((Word)0x1234));
    }

    /// <summary>
    /// Verifies Read16 uses atomic access in Atomic mode when target supports wide.
    /// </summary>
    [Test]
    public void Read16_AtomicMode_SupportsWide_UsesAtomicAccess()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        memory.AsSpan()[0x100] = 0x78;  // Low byte
        memory.AsSpan()[0x101] = 0x56;  // High byte
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsWide, target, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.DataRead, BusAccessMode.Atomic, 16);
        Word value = bus.Read16(access);

        Assert.That(value, Is.EqualTo((Word)0x5678));
    }

    /// <summary>
    /// Verifies Read16 decomposes when crossing page boundary.
    /// </summary>
    [Test]
    public void Read16_CrossPageBoundary_DecomposesIntoByteReads()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize * 2, "TestRAM");
        memory.AsSpan()[0xFFF] = 0xCD;  // Last byte of page 0
        memory.AsSpan()[0x1000] = 0xAB; // First byte of page 1
        var target = new RamTarget(memory.Slice(0, (uint)(PageSize * 2)));

        bus.MapPageRange(0, 2, 1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsWide, target, 0);

        var access = CreateTestAccess(0x0FFF, AccessIntent.DataRead, BusAccessMode.Atomic, 16);
        Word value = bus.Read16(access);

        Assert.That(value, Is.EqualTo((Word)0xABCD));
    }

    /// <summary>
    /// Verifies Read16 decomposes when Decompose flag is set.
    /// </summary>
    [Test]
    public void Read16_DecomposeFlag_ForcesDecomposition()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        memory.AsSpan()[0x100] = 0xEF;  // Low byte
        memory.AsSpan()[0x101] = 0xBE;  // High byte
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsWide, target, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.DataRead, BusAccessMode.Atomic, 16, AccessFlags.Decompose);
        Word value = bus.Read16(access);

        Assert.That(value, Is.EqualTo((Word)0xBEEF));
    }

    /// <summary>
    /// Verifies Read32 works correctly.
    /// </summary>
    [Test]
    public void Read32_MappedRam_ReturnsCorrectValue()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        memory.AsSpan()[0x100] = 0x78;  // Byte 0
        memory.AsSpan()[0x101] = 0x56;  // Byte 1
        memory.AsSpan()[0x102] = 0x34;  // Byte 2
        memory.AsSpan()[0x103] = 0x12;  // Byte 3
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsWide, target, 0));

        var access = CreateTestAccess(0x0100, AccessIntent.DataRead, BusAccessMode.Atomic, 32);
        DWord value = bus.Read32(access);

        Assert.That(value, Is.EqualTo(0x12345678u));
    }

    /// <summary>
    /// Verifies Write16 stores value correctly.
    /// </summary>
    [Test]
    public void Write16_MappedRam_StoresValue()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsWide, target, 0));

        var access = CreateTestAccess(0x0200, AccessIntent.DataWrite, BusAccessMode.Atomic, 16);
        bus.Write16(access, 0xCAFE);

        Assert.Multiple(() =>
        {
            Assert.That(memory.AsSpan()[0x200], Is.EqualTo(0xFE), "Low byte");
            Assert.That(memory.AsSpan()[0x201], Is.EqualTo(0xCA), "High byte");
        });
    }

    /// <summary>
    /// Verifies Write32 stores value correctly.
    /// </summary>
    [Test]
    public void Write32_MappedRam_StoresValue()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsWide, target, 0));

        var access = CreateTestAccess(0x0300, AccessIntent.DataWrite, BusAccessMode.Atomic, 32);
        bus.Write32(access, 0xDEADBEEFu);

        Assert.Multiple(() =>
        {
            Assert.That(memory.AsSpan()[0x300], Is.EqualTo(0xEF), "Byte 0");
            Assert.That(memory.AsSpan()[0x301], Is.EqualTo(0xBE), "Byte 1");
            Assert.That(memory.AsSpan()[0x302], Is.EqualTo(0xAD), "Byte 2");
            Assert.That(memory.AsSpan()[0x303], Is.EqualTo(0xDE), "Byte 3");
        });
    }

    /// <summary>
    /// Verifies TryRead16 returns correct fault for cross-page access where second page is unmapped.
    /// </summary>
    [Test]
    public void TryRead16_CrossPageWithUnmappedSecondPage_ReturnsUnmappedFault()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        // Only map page 0, leave page 1 unmapped
        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target, 0));

        var access = CreateTestAccess(0x0FFF, AccessIntent.DataRead, BusAccessMode.Atomic, 16);
        var result = bus.TryRead16(access);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.Unmapped));
            Assert.That(result.Fault.Address, Is.EqualTo(0x1000u), "Fault should be on second byte address");
        });
    }

    /// <summary>
    /// Verifies RemapPage changes the page target.
    /// </summary>
    [Test]
    public void RemapPage_ChangesTarget()
    {
        var bus = new MainBus();
        var memory1 = new PhysicalMemory(PageSize, "RAM1");
        var memory2 = new PhysicalMemory(PageSize, "RAM2");
        memory1.AsSpan()[0x100] = 0x11;
        memory2.AsSpan()[0x100] = 0x22;
        var target1 = new RamTarget(memory1.Slice(0, PageSize));
        var target2 = new RamTarget(memory2.Slice(0, PageSize));

        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target1, 0));

        // Verify initial mapping
        var access = CreateTestAccess(0x0100, AccessIntent.DataRead);
        Assert.That(bus.Read8(access), Is.EqualTo(0x11), "Initial value from RAM1");

        // Remap to target2
        bus.RemapPage(0, target2, 0);

        // Verify new mapping
        Assert.That(bus.Read8(access), Is.EqualTo(0x22), "Value from RAM2 after remap");
    }

    /// <summary>
    /// Verifies RemapPage with full entry replaces the entire page entry.
    /// </summary>
    [Test]
    public void RemapPage_WithFullEntry_ReplacesEntireEntry()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapPage(0, new(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, target, 0));

        var newEntry = new PageEntry(2, RegionTag.Rom, PagePerms.ReadExecute, TargetCaps.SupportsWide, target, 0x100);
        bus.RemapPage(0, newEntry);

        var retrievedEntry = bus.GetPageEntry(0);
        Assert.Multiple(() =>
        {
            Assert.That(retrievedEntry.DeviceId, Is.EqualTo(2));
            Assert.That(retrievedEntry.RegionTag, Is.EqualTo(RegionTag.Rom));
            Assert.That(retrievedEntry.Perms, Is.EqualTo(PagePerms.ReadExecute));
            Assert.That(retrievedEntry.PhysicalBase, Is.EqualTo(0x100u));
        });
    }

    /// <summary>
    /// Verifies RemapPageRange changes multiple pages.
    /// </summary>
    [Test]
    public void RemapPageRange_ChangesMultiplePages()
    {
        var bus = new MainBus();
        var memory1 = new PhysicalMemory(PageSize * 4, "RAM1");
        var memory2 = new PhysicalMemory(PageSize * 4, "RAM2");
        var target1 = new RamTarget(memory1.Slice(0, (uint)(PageSize * 4)));
        var target2 = new RamTarget(memory2.Slice(0, (uint)(PageSize * 4)));

        bus.MapPageRange(0, 4, 1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, target1, 0);
        bus.RemapPageRange(1, 2, target2, 0);

        Assert.Multiple(() =>
        {
            Assert.That(bus.GetPageEntry(0x0000).Target, Is.SameAs(target1), "Page 0 unchanged");
            Assert.That(bus.GetPageEntry(0x1000).Target, Is.SameAs(target2), "Page 1 remapped");
            Assert.That(bus.GetPageEntry(0x2000).Target, Is.SameAs(target2), "Page 2 remapped");
            Assert.That(bus.GetPageEntry(0x3000).Target, Is.SameAs(target1), "Page 3 unchanged");
        });
    }

    /// <summary>
    /// Verifies GetPageEntryByIndex returns correct entry.
    /// </summary>
    [Test]
    public void GetPageEntryByIndex_ReturnsCorrectEntry()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapPage(5, new(42, RegionTag.Stack, PagePerms.ReadWrite, TargetCaps.None, target, 0));

        var entry = bus.GetPageEntryByIndex(5);
        Assert.Multiple(() =>
        {
            Assert.That(entry.DeviceId, Is.EqualTo(42));
            Assert.That(entry.RegionTag, Is.EqualTo(RegionTag.Stack));
        });
    }

    /// <summary>
    /// Verifies GetPageEntryByIndex throws for invalid index.
    /// </summary>
    [Test]
    public void GetPageEntryByIndex_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        var bus = new MainBus();

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = bus.GetPageEntryByIndex(100));
    }

    /// <summary>
    /// Verifies ValidateAlignment throws for non-page-aligned address.
    /// </summary>
    [Test]
    public void ValidateAlignment_NonAlignedAddress_ThrowsArgumentException()
    {
        var bus = new MainBus();

        var ex = Assert.Throws<ArgumentException>(() => bus.ValidateAlignment(0x0100, 0x1000));
        Assert.That(ex.ParamName, Is.EqualTo("address"));
    }

    /// <summary>
    /// Verifies ValidateAlignment throws for non-page-aligned size.
    /// </summary>
    [Test]
    public void ValidateAlignment_NonAlignedSize_ThrowsArgumentException()
    {
        var bus = new MainBus();

        var ex = Assert.Throws<ArgumentException>(() => bus.ValidateAlignment(0x1000, 0x0100));
        Assert.That(ex.ParamName, Is.EqualTo("size"));
    }

    /// <summary>
    /// Verifies ValidateAlignment succeeds for page-aligned values.
    /// </summary>
    [Test]
    public void ValidateAlignment_AlignedValues_DoesNotThrow()
    {
        var bus = new MainBus();

        Assert.DoesNotThrow(() => bus.ValidateAlignment(0x1000, 0x2000));
    }

    /// <summary>
    /// Verifies MapRegion maps the I/O page correctly at 0xC000.
    /// </summary>
    [Test]
    public void MapRegion_IoPage_MapsCorrectly()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestIO");
        var target = new RamTarget(memory.Slice(0, PageSize));

        bus.MapRegion(
            virtualBase: 0xC000,
            size: 0x1000,
            deviceId: 10,
            regionTag: RegionTag.Io,
            perms: PagePerms.ReadWrite,
            caps: TargetCaps.HasSideEffects,
            target: target,
            physicalBase: 0);

        var entry = bus.GetPageEntry(0xC000);
        Assert.Multiple(() =>
        {
            Assert.That(entry.DeviceId, Is.EqualTo(10));
            Assert.That(entry.RegionTag, Is.EqualTo(RegionTag.Io));
            Assert.That(entry.Target, Is.SameAs(target));
        });
    }

    /// <summary>
    /// Verifies MapRegion throws for non-page-aligned virtual base.
    /// </summary>
    [Test]
    public void MapRegion_NonAlignedVirtualBase_ThrowsArgumentException()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var ex = Assert.Throws<ArgumentException>(() => bus.MapRegion(
            virtualBase: 0xC100,
            size: 0x1000,
            deviceId: 1,
            regionTag: RegionTag.Ram,
            perms: PagePerms.ReadWrite,
            caps: TargetCaps.None,
            target: target,
            physicalBase: 0));
        Assert.That(ex.ParamName, Is.EqualTo("address"));
    }

    /// <summary>
    /// Verifies MapRegion throws for non-page-aligned size.
    /// </summary>
    [Test]
    public void MapRegion_NonAlignedSize_ThrowsArgumentException()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var ex = Assert.Throws<ArgumentException>(() => bus.MapRegion(
            virtualBase: 0xC000,
            size: 0x0100,
            deviceId: 1,
            regionTag: RegionTag.Ram,
            perms: PagePerms.ReadWrite,
            caps: TargetCaps.None,
            target: target,
            physicalBase: 0));
        Assert.That(ex.ParamName, Is.EqualTo("size"));
    }

    /// <summary>
    /// Verifies MapRegion throws when region exceeds address space.
    /// </summary>
    [Test]
    public void MapRegion_ExceedsAddressSpace_ThrowsArgumentOutOfRangeException()
    {
        var bus = new MainBus(); // 16-bit address space = 16 pages
        var memory = new PhysicalMemory(PageSize * 2, "TestRAM");
        var target = new RamTarget(memory.Slice(0, (uint)(PageSize * 2)));

        Assert.Throws<ArgumentOutOfRangeException>(() => bus.MapRegion(
            virtualBase: 0xF000,
            size: 0x2000, // Would go beyond 64KB
            deviceId: 1,
            regionTag: RegionTag.Ram,
            perms: PagePerms.ReadWrite,
            caps: TargetCaps.None,
            target: target,
            physicalBase: 0));
    }

    /// <summary>
    /// Verifies MapRegion maps multiple pages with incrementing physical addresses.
    /// </summary>
    [Test]
    public void MapRegion_MultiplePages_IncrementsPhysicalBase()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize * 4, "TestRAM");
        var target = new RamTarget(memory.Slice(0, (uint)(PageSize * 4)));

        bus.MapRegion(
            virtualBase: 0x4000,
            size: 0x4000, // 4 pages
            deviceId: 1,
            regionTag: RegionTag.Ram,
            perms: PagePerms.ReadWrite,
            caps: TargetCaps.None,
            target: target,
            physicalBase: 0x1000);

        Assert.Multiple(() =>
        {
            Assert.That(bus.GetPageEntry(0x4000).PhysicalBase, Is.EqualTo(0x1000u));
            Assert.That(bus.GetPageEntry(0x5000).PhysicalBase, Is.EqualTo(0x2000u));
            Assert.That(bus.GetPageEntry(0x6000).PhysicalBase, Is.EqualTo(0x3000u));
            Assert.That(bus.GetPageEntry(0x7000).PhysicalBase, Is.EqualTo(0x4000u));
        });
    }

    /// <summary>
    /// Verifies MapPageAt maps page 0x0D correctly at address 0xD000.
    /// </summary>
    [Test]
    public void MapPageAt_PageD_MapsCorrectly()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var entry = new PageEntry(
            DeviceId: 42,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadWrite,
            Caps: TargetCaps.SupportsPeek,
            Target: target,
            PhysicalBase: 0);

        bus.MapPageAt(0xD000, entry);

        var retrievedEntry = bus.GetPageEntry(0xD000);
        Assert.Multiple(() =>
        {
            Assert.That(retrievedEntry.DeviceId, Is.EqualTo(42));
            Assert.That(retrievedEntry.Target, Is.SameAs(target));
        });

        // Verify page index is correct (0xD = 13)
        var byIndex = bus.GetPageEntryByIndex(0x0D);
        Assert.That(byIndex.DeviceId, Is.EqualTo(42));
    }

    /// <summary>
    /// Verifies MapPageAt throws for non-page-aligned address.
    /// </summary>
    [Test]
    public void MapPageAt_NonAlignedAddress_ThrowsArgumentException()
    {
        var bus = new MainBus();
        var entry = new PageEntry(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, null!, 0);

        var ex = Assert.Throws<ArgumentException>(() => bus.MapPageAt(0xD100, entry));
        Assert.That(ex.ParamName, Is.EqualTo("virtualAddress"));
    }

    /// <summary>
    /// Verifies MapPageAt throws for address beyond address space.
    /// </summary>
    [Test]
    public void MapPageAt_BeyondAddressSpace_ThrowsArgumentOutOfRangeException()
    {
        var bus = new MainBus(); // 16-bit = 64KB = 16 pages
        var entry = new PageEntry(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, null!, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => bus.MapPageAt(0x10000, entry));
    }

    /// <summary>
    /// Verifies SetPageEntry is functionally equivalent to MapPage.
    /// </summary>
    [Test]
    public void SetPageEntry_EquivalentToMapPage()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var entry = new PageEntry(
            DeviceId: 99,
            RegionTag: RegionTag.Stack,
            Perms: PagePerms.ReadWrite,
            Caps: TargetCaps.None,
            Target: target,
            PhysicalBase: 0);

        bus.SetPageEntry(3, entry);

        var retrievedEntry = bus.GetPageEntryByIndex(3);
        Assert.Multiple(() =>
        {
            Assert.That(retrievedEntry.DeviceId, Is.EqualTo(99));
            Assert.That(retrievedEntry.RegionTag, Is.EqualTo(RegionTag.Stack));
            Assert.That(retrievedEntry.Target, Is.SameAs(target));
        });
    }

    /// <summary>
    /// Verifies SetPageEntry throws for invalid page index.
    /// </summary>
    [Test]
    public void SetPageEntry_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        var bus = new MainBus();
        var entry = new PageEntry(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, null!, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => bus.SetPageEntry(100, entry));
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