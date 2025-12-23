// <copyright file="RamTargetTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="RamTarget"/> class.
/// </summary>
[TestFixture]
public class RamTargetTests
{
    /// <summary>
    /// Verifies that RamTarget can be created with a memory slice.
    /// </summary>
    [Test]
    public void RamTarget_CanBeCreatedWithMemorySlice()
    {
        var physicalMemory = new PhysicalMemory(1024, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 1024));

        Assert.That(ram.Size, Is.EqualTo(1024));
    }

    /// <summary>
    /// Verifies that RamTarget can be created with a partial slice.
    /// </summary>
    [Test]
    public void RamTarget_CanBeCreatedWithPartialSlice()
    {
        var physicalMemory = new PhysicalMemory(1024, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(100, 256));

        Assert.That(ram.Size, Is.EqualTo(256));
    }

    /// <summary>
    /// Verifies that constructor throws for empty memory slice.
    /// </summary>
    [Test]
    public void RamTarget_Constructor_ThrowsForEmptySlice()
    {
        Assert.Throws<ArgumentException>(() => new RamTarget(Memory<byte>.Empty));
    }

    /// <summary>
    /// Verifies that Capabilities includes expected flags.
    /// </summary>
    [Test]
    public void RamTarget_Capabilities_IncludesExpectedFlags()
    {
        var physicalMemory = new PhysicalMemory(64, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 64));

        Assert.Multiple(() =>
        {
            Assert.That(ram.Capabilities.HasFlag(TargetCaps.SupportsPeek), Is.True);
            Assert.That(ram.Capabilities.HasFlag(TargetCaps.SupportsPoke), Is.True);
            Assert.That(ram.Capabilities.HasFlag(TargetCaps.SupportsWide), Is.True);
            Assert.That(ram.Capabilities.HasFlag(TargetCaps.HasSideEffects), Is.False);
        });
    }

    /// <summary>
    /// Verifies that Read8 returns written value.
    /// </summary>
    [Test]
    public void RamTarget_Read8_ReturnsWrittenValue()
    {
        var physicalMemory = new PhysicalMemory(64, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 64));
        var access = CreateDefaultAccess();

        ram.Write8(10, 0xAB, in access);
        byte value = ram.Read8(10, in access);

        Assert.That(value, Is.EqualTo(0xAB));
    }

    /// <summary>
    /// Verifies that Read8 returns initial data value from physical memory.
    /// </summary>
    [Test]
    public void RamTarget_Read8_ReturnsInitialDataValue()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var physicalMemory = new PhysicalMemory(data, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 4));
        var access = CreateDefaultAccess();

        byte value = ram.Read8(2, in access);

        Assert.That(value, Is.EqualTo(0x33));
    }

    /// <summary>
    /// Verifies that writes through RamTarget are visible in physical memory.
    /// </summary>
    [Test]
    public void RamTarget_Write_IsVisibleInPhysicalMemory()
    {
        var physicalMemory = new PhysicalMemory(64, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 64));
        var access = CreateDefaultAccess();

        ram.Write8(0, 0xFF, in access);

        Assert.That(physicalMemory.AsReadOnlySpan()[0], Is.EqualTo(0xFF), "Physical memory should reflect RAM writes");
    }

    /// <summary>
    /// Verifies that writes to physical memory are visible through RamTarget.
    /// </summary>
    [Test]
    public void RamTarget_PhysicalMemoryWrite_IsVisibleThroughRam()
    {
        var physicalMemory = new PhysicalMemory(64, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 64));
        var access = CreateDefaultAccess();

        physicalMemory.AsSpan()[0] = 0xAA;

        Assert.That(ram.Read8(0, in access), Is.EqualTo(0xAA), "RAM should see physical memory changes");
    }

    /// <summary>
    /// Verifies that Read16 returns little-endian value.
    /// </summary>
    [Test]
    public void RamTarget_Read16_ReturnsLittleEndianValue()
    {
        var physicalMemory = new PhysicalMemory(64, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 64));
        var access = CreateDefaultAccess();

        ram.Write8(0, 0x34, in access);
        ram.Write8(1, 0x12, in access);

        ushort value = ram.Read16(0, in access);

        Assert.That(value, Is.EqualTo(0x1234));
    }

    /// <summary>
    /// Verifies that Write16 stores little-endian value.
    /// </summary>
    [Test]
    public void RamTarget_Write16_StoresLittleEndianValue()
    {
        var physicalMemory = new PhysicalMemory(64, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 64));
        var access = CreateDefaultAccess();

        ram.Write16(0, 0xABCD, in access);

        Assert.Multiple(() =>
        {
            Assert.That(ram.Read8(0, in access), Is.EqualTo(0xCD));
            Assert.That(ram.Read8(1, in access), Is.EqualTo(0xAB));
        });
    }

    /// <summary>
    /// Verifies that Read32 returns little-endian value.
    /// </summary>
    [Test]
    public void RamTarget_Read32_ReturnsLittleEndianValue()
    {
        var physicalMemory = new PhysicalMemory(64, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 64));
        var access = CreateDefaultAccess();

        ram.Write8(0, 0x78, in access);
        ram.Write8(1, 0x56, in access);
        ram.Write8(2, 0x34, in access);
        ram.Write8(3, 0x12, in access);

        uint value = ram.Read32(0, in access);

        Assert.That(value, Is.EqualTo(0x12345678u));
    }

    /// <summary>
    /// Verifies that Write32 stores little-endian value.
    /// </summary>
    [Test]
    public void RamTarget_Write32_StoresLittleEndianValue()
    {
        var physicalMemory = new PhysicalMemory(64, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 64));
        var access = CreateDefaultAccess();

        ram.Write32(0, 0xDEADBEEFu, in access);

        Assert.Multiple(() =>
        {
            Assert.That(ram.Read8(0, in access), Is.EqualTo(0xEF));
            Assert.That(ram.Read8(1, in access), Is.EqualTo(0xBE));
            Assert.That(ram.Read8(2, in access), Is.EqualTo(0xAD));
            Assert.That(ram.Read8(3, in access), Is.EqualTo(0xDE));
        });
    }

    /// <summary>
    /// Verifies that new RAM slice is initialized to zero.
    /// </summary>
    [Test]
    public void RamTarget_NewRam_InitializedToZero()
    {
        var physicalMemory = new PhysicalMemory(4, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 4));
        var access = CreateDefaultAccess();

        Assert.Multiple(() =>
        {
            Assert.That(ram.Read8(0, in access), Is.EqualTo(0));
            Assert.That(ram.Read8(1, in access), Is.EqualTo(0));
            Assert.That(ram.Read8(2, in access), Is.EqualTo(0));
            Assert.That(ram.Read8(3, in access), Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that Read16 and Write16 roundtrip correctly.
    /// </summary>
    [Test]
    public void RamTarget_Read16Write16_Roundtrip()
    {
        var physicalMemory = new PhysicalMemory(64, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 64));
        var access = CreateDefaultAccess();

        ram.Write16(10, 0xABCD, in access);
        ushort value = ram.Read16(10, in access);

        Assert.That(value, Is.EqualTo(0xABCD));
    }

    /// <summary>
    /// Verifies that Read32 and Write32 roundtrip correctly.
    /// </summary>
    [Test]
    public void RamTarget_Read32Write32_Roundtrip()
    {
        var physicalMemory = new PhysicalMemory(64, "Test RAM");
        var ram = new RamTarget(physicalMemory.Slice(0, 64));
        var access = CreateDefaultAccess();

        ram.Write32(20, 0x12345678u, in access);
        uint value = ram.Read32(20, in access);

        Assert.That(value, Is.EqualTo(0x12345678u));
    }

    /// <summary>
    /// Verifies that multiple RamTargets can share the same physical memory.
    /// </summary>
    [Test]
    public void RamTarget_MultipleTargets_SharePhysicalMemory()
    {
        var physicalMemory = new PhysicalMemory(128, "Test RAM");
        var ram1 = new RamTarget(physicalMemory.Slice(0, 64));
        var ram2 = new RamTarget(physicalMemory.Slice(32, 64)); // Overlaps with ram1
        var access = CreateDefaultAccess();

        // Write via ram1 at offset 40 (which is offset 8 in ram2)
        ram1.Write8(40, 0xAA, in access);

        // Read via ram2
        byte value = ram2.Read8(8, in access);

        Assert.That(value, Is.EqualTo(0xAA), "Multiple targets should share the same storage");
    }

    /// <summary>
    /// Creates a default BusAccess for testing.
    /// </summary>
    /// <returns>A default BusAccess instance.</returns>
    private static BusAccess CreateDefaultAccess() => new(
        Address: 0,
        Value: 0,
        WidthBits: 8,
        Mode: CpuMode.Compat,
        EmulationFlag: true,
        Intent: AccessIntent.DataRead,
        SourceId: 0,
        Cycle: 0,
        Flags: AccessFlags.None,
        PrivilegeLevel: PrivilegeLevel.Ring0);
}