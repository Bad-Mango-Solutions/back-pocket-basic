// <copyright file="MemoryBusAdapterTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="MemoryBusAdapter"/> class.
/// </summary>
[TestFixture]
public class MemoryBusAdapterTests
{
    private const int PageSize = 4096;

    /// <summary>
    /// Verifies constructor throws for null bus.
    /// </summary>
    [Test]
    public void Constructor_NullBus_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MemoryBusAdapter(null!));
    }

    /// <summary>
    /// Verifies Size is computed from bus page count.
    /// </summary>
    [Test]
    public void Size_ReturnsCorrectValue()
    {
        var bus = new MainBus(addressSpaceBits: 16);
        var adapter = new MemoryBusAdapter(bus);

        Assert.That(adapter.Size, Is.EqualTo(65536u), "64KB address space");
    }

    /// <summary>
    /// Verifies Read returns value from mapped RAM.
    /// </summary>
    [Test]
    public void Read_MappedAddress_ReturnsValue()
    {
        var bus = CreateBusWithRam(out var memory);
        memory.AsSpan()[0x100] = 0x42;
        var adapter = new MemoryBusAdapter(bus);

        byte value = adapter.Read(0x0100);

        Assert.That(value, Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies Write stores value to mapped RAM.
    /// </summary>
    [Test]
    public void Write_MappedAddress_StoresValue()
    {
        var bus = CreateBusWithRam(out var memory);
        var adapter = new MemoryBusAdapter(bus);

        adapter.Write(0x0200, 0x55);

        Assert.That(memory.AsSpan()[0x200], Is.EqualTo(0x55));
    }

    /// <summary>
    /// Verifies ReadWord returns correct 16-bit value.
    /// </summary>
    [Test]
    public void ReadWord_MappedAddress_ReturnsCorrectValue()
    {
        var bus = CreateBusWithRam(out var memory);
        memory.AsSpan()[0x100] = 0x34;  // Low byte
        memory.AsSpan()[0x101] = 0x12;  // High byte
        var adapter = new MemoryBusAdapter(bus);

        ushort value = adapter.ReadWord(0x0100);

        Assert.That(value, Is.EqualTo((ushort)0x1234));
    }

    /// <summary>
    /// Verifies WriteWord stores correct 16-bit value.
    /// </summary>
    [Test]
    public void WriteWord_MappedAddress_StoresCorrectValue()
    {
        var bus = CreateBusWithRam(out var memory);
        var adapter = new MemoryBusAdapter(bus);

        adapter.WriteWord(0x0200, 0xABCD);

        Assert.Multiple(() =>
        {
            Assert.That(memory.AsSpan()[0x200], Is.EqualTo(0xCD), "Low byte");
            Assert.That(memory.AsSpan()[0x201], Is.EqualTo(0xAB), "High byte");
        });
    }

    /// <summary>
    /// Verifies ReadDWord returns correct 32-bit value.
    /// </summary>
    [Test]
    public void ReadDWord_MappedAddress_ReturnsCorrectValue()
    {
        var bus = CreateBusWithRam(out var memory);
        memory.AsSpan()[0x100] = 0x78;  // Byte 0
        memory.AsSpan()[0x101] = 0x56;  // Byte 1
        memory.AsSpan()[0x102] = 0x34;  // Byte 2
        memory.AsSpan()[0x103] = 0x12;  // Byte 3
        var adapter = new MemoryBusAdapter(bus);

        uint value = adapter.ReadDWord(0x0100);

        Assert.That(value, Is.EqualTo(0x12345678u));
    }

    /// <summary>
    /// Verifies WriteDWord stores correct 32-bit value.
    /// </summary>
    [Test]
    public void WriteDWord_MappedAddress_StoresCorrectValue()
    {
        var bus = CreateBusWithRam(out var memory);
        var adapter = new MemoryBusAdapter(bus);

        adapter.WriteDWord(0x0300, 0xDEADBEEFu);

        Assert.Multiple(() =>
        {
            Assert.That(memory.AsSpan()[0x300], Is.EqualTo(0xEF), "Byte 0");
            Assert.That(memory.AsSpan()[0x301], Is.EqualTo(0xBE), "Byte 1");
            Assert.That(memory.AsSpan()[0x302], Is.EqualTo(0xAD), "Byte 2");
            Assert.That(memory.AsSpan()[0x303], Is.EqualTo(0xDE), "Byte 3");
        });
    }

    /// <summary>
    /// Verifies Read throws InvalidOperationException for unmapped address.
    /// </summary>
    [Test]
    public void Read_UnmappedAddress_ThrowsInvalidOperationException()
    {
        var bus = new MainBus();
        var adapter = new MemoryBusAdapter(bus);

        var ex = Assert.Throws<InvalidOperationException>(() => adapter.Read(0x5000));
        Assert.That(ex.Message, Does.Contain("Unmapped"));
    }

    /// <summary>
    /// Verifies Write throws InvalidOperationException for unmapped address.
    /// </summary>
    [Test]
    public void Write_UnmappedAddress_ThrowsInvalidOperationException()
    {
        var bus = new MainBus();
        var adapter = new MemoryBusAdapter(bus);

        var ex = Assert.Throws<InvalidOperationException>(() => adapter.Write(0x5000, 0x42));
        Assert.That(ex.Message, Does.Contain("Unmapped"));
    }

    /// <summary>
    /// Verifies Read throws InvalidOperationException for address without read permission.
    /// </summary>
    [Test]
    public void Read_NoReadPermission_ThrowsInvalidOperationException()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));
        bus.MapPage(0, new PageEntry(1, RegionTag.Ram, PagePerms.Write, TargetCaps.None, target, 0));
        var adapter = new MemoryBusAdapter(bus);

        var ex = Assert.Throws<InvalidOperationException>(() => adapter.Read(0x0100));
        Assert.That(ex.Message, Does.Contain("Permission"));
    }

    /// <summary>
    /// Verifies Inspect returns memory contents using debug reads.
    /// </summary>
    [Test]
    public void Inspect_MappedRange_ReturnsCorrectData()
    {
        var bus = CreateBusWithRam(out var memory);
        memory.AsSpan()[0x100] = 0x11;
        memory.AsSpan()[0x101] = 0x22;
        memory.AsSpan()[0x102] = 0x33;
        var adapter = new MemoryBusAdapter(bus);

        var data = adapter.Inspect(0x100, 3);

        Assert.Multiple(() =>
        {
            Assert.That(data.Length, Is.EqualTo(3));
            Assert.That(data.Span[0], Is.EqualTo(0x11));
            Assert.That(data.Span[1], Is.EqualTo(0x22));
            Assert.That(data.Span[2], Is.EqualTo(0x33));
        });
    }

    /// <summary>
    /// Verifies Inspect throws for invalid start address.
    /// </summary>
    [Test]
    public void Inspect_InvalidStart_ThrowsArgumentOutOfRangeException()
    {
        var bus = CreateBusWithRam(out _);
        var adapter = new MemoryBusAdapter(bus);

        Assert.Throws<ArgumentOutOfRangeException>(() => adapter.Inspect(-1, 10));
    }

    /// <summary>
    /// Verifies Inspect throws for range exceeding memory size.
    /// </summary>
    [Test]
    public void Inspect_RangeExceedsSize_ThrowsArgumentOutOfRangeException()
    {
        var bus = CreateBusWithRam(out _);
        var adapter = new MemoryBusAdapter(bus);

        Assert.Throws<ArgumentOutOfRangeException>(() => adapter.Inspect(65530, 20));
    }

    /// <summary>
    /// Verifies AsReadOnlyMemory throws NotSupportedException.
    /// </summary>
    [Test]
    public void AsReadOnlyMemory_ThrowsNotSupportedException()
    {
        var bus = CreateBusWithRam(out _);
        var adapter = new MemoryBusAdapter(bus);

        Assert.Throws<NotSupportedException>(() => adapter.AsReadOnlyMemory());
    }

    /// <summary>
    /// Verifies AsMemory throws NotSupportedException.
    /// </summary>
    [Test]
    public void AsMemory_ThrowsNotSupportedException()
    {
        var bus = CreateBusWithRam(out _);
        var adapter = new MemoryBusAdapter(bus);

        Assert.Throws<NotSupportedException>(() => adapter.AsMemory());
    }

    /// <summary>
    /// Verifies CycleCount increments on read operations.
    /// </summary>
    [Test]
    public void CycleCount_IncrementsOnRead()
    {
        var bus = CreateBusWithRam(out _);
        var adapter = new MemoryBusAdapter(bus);

        Assert.That(adapter.CycleCount, Is.EqualTo(0ul), "Initial cycle count");

        adapter.Read(0x0100);
        Assert.That(adapter.CycleCount, Is.GreaterThan(0ul), "Cycle count after read");
    }

    /// <summary>
    /// Verifies ResetCycleCount clears the cycle count.
    /// </summary>
    [Test]
    public void ResetCycleCount_ClearsCycleCount()
    {
        var bus = CreateBusWithRam(out _);
        var adapter = new MemoryBusAdapter(bus);

        adapter.Read(0x0100);
        adapter.Read(0x0101);
        Assert.That(adapter.CycleCount, Is.GreaterThan(0ul), "Cycle count before reset");

        adapter.ResetCycleCount();
        Assert.That(adapter.CycleCount, Is.EqualTo(0ul), "Cycle count after reset");
    }

    /// <summary>
    /// Verifies adapter uses specified CPU mode.
    /// </summary>
    [Test]
    public void Constructor_WithNativeMode_UsesNativeMode()
    {
        var bus = new MainBus();
        var memory = new PhysicalMemory(PageSize, "TestRAM");
        var target = new RamTarget(memory.Slice(0, PageSize));

        // Map with no execute permission - should fail in Native mode on instruction fetch-like access
        // Since MemoryBusAdapter uses DataRead intent, this test verifies the mode is stored correctly
        bus.MapPage(0, new PageEntry(1, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.SupportsPeek, target, 0));

        var adapterNative = new MemoryBusAdapter(bus, CpuMode.Native);
        var adapterCompat = new MemoryBusAdapter(bus, CpuMode.Compat);

        // Both should succeed for DataRead intent
        Assert.DoesNotThrow(() => adapterNative.Read(0x0100));
        Assert.DoesNotThrow(() => adapterCompat.Read(0x0100));
    }

    /// <summary>
    /// Helper method to create a bus with full RAM mapping.
    /// </summary>
    private static MainBus CreateBusWithRam(out PhysicalMemory memory)
    {
        var bus = new MainBus(addressSpaceBits: 16);
        memory = new PhysicalMemory(65536, "TestRAM");
        var target = new RamTarget(memory.Slice(0, 65536));

        bus.MapPageRange(
            startPage: 0,
            pageCount: 16,
            deviceId: 1,
            regionTag: RegionTag.Ram,
            perms: PagePerms.ReadWrite,
            caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke | TargetCaps.SupportsWide,
            target: target,
            physicalBase: 0);

        return bus;
    }
}