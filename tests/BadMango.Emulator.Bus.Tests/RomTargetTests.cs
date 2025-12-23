// <copyright file="RomTargetTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="RomTarget"/> class.
/// </summary>
[TestFixture]
public class RomTargetTests
{
    /// <summary>
    /// Verifies that RomTarget can be created with a read-only memory slice.
    /// </summary>
    [Test]
    public void RomTarget_CanBeCreatedWithReadOnlyMemorySlice()
    {
        var data = new byte[] { 0xEA, 0x00, 0xFF };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, 3));

        Assert.That(rom.Size, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that RomTarget can be created with empty memory.
    /// </summary>
    [Test]
    public void RomTarget_CanBeCreatedWithEmptyMemory()
    {
        var rom = new RomTarget(ReadOnlyMemory<byte>.Empty);

        Assert.That(rom.Size, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that Capabilities includes expected flags.
    /// </summary>
    [Test]
    public void RomTarget_Capabilities_IncludesExpectedFlags()
    {
        var data = new byte[] { 0x00 };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, 1));

        Assert.Multiple(() =>
        {
            Assert.That(rom.Capabilities.HasFlag(TargetCaps.SupportsPeek), Is.True);
            Assert.That(rom.Capabilities.HasFlag(TargetCaps.SupportsPoke), Is.False, "ROM should not support Poke");
            Assert.That(rom.Capabilities.HasFlag(TargetCaps.SupportsWide), Is.True);
            Assert.That(rom.Capabilities.HasFlag(TargetCaps.HasSideEffects), Is.False);
        });
    }

    /// <summary>
    /// Verifies that Read8 returns correct value.
    /// </summary>
    [Test]
    public void RomTarget_Read8_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, 4));
        var access = CreateDefaultAccess();

        Assert.Multiple(() =>
        {
            Assert.That(rom.Read8(0, in access), Is.EqualTo(0x11));
            Assert.That(rom.Read8(1, in access), Is.EqualTo(0x22));
            Assert.That(rom.Read8(2, in access), Is.EqualTo(0x33));
            Assert.That(rom.Read8(3, in access), Is.EqualTo(0x44));
        });
    }

    /// <summary>
    /// Verifies that Write8 is silently ignored.
    /// </summary>
    [Test]
    public void RomTarget_Write8_IsSilentlyIgnored()
    {
        var data = new byte[] { 0xAA, 0xBB };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, 2));
        var access = CreateDefaultAccess();

        rom.Write8(0, 0xFF, in access);

        Assert.That(rom.Read8(0, in access), Is.EqualTo(0xAA), "ROM should not be modified by writes");
    }

    /// <summary>
    /// Verifies that ROM reflects physical memory content.
    /// </summary>
    [Test]
    public void RomTarget_ReflectsPhysicalMemoryContent()
    {
        var data = new byte[] { 0x11, 0x22 };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, 2));
        var access = CreateDefaultAccess();

        Assert.That(rom.Read8(0, in access), Is.EqualTo(0x11));
    }

    /// <summary>
    /// Verifies that Read16 returns little-endian value.
    /// </summary>
    [Test]
    public void RomTarget_Read16_ReturnsLittleEndianValue()
    {
        var data = new byte[] { 0x34, 0x12 };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, 2));
        var access = CreateDefaultAccess();

        ushort value = rom.Read16(0, in access);

        Assert.That(value, Is.EqualTo(0x1234));
    }

    /// <summary>
    /// Verifies that Write16 is silently ignored.
    /// </summary>
    [Test]
    public void RomTarget_Write16_IsSilentlyIgnored()
    {
        var data = new byte[] { 0x34, 0x12 };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, 2));
        var access = CreateDefaultAccess();

        rom.Write16(0, 0xFFFF, in access);

        Assert.That(rom.Read16(0, in access), Is.EqualTo(0x1234), "ROM should not be modified by writes");
    }

    /// <summary>
    /// Verifies that Read32 returns little-endian value.
    /// </summary>
    [Test]
    public void RomTarget_Read32_ReturnsLittleEndianValue()
    {
        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, 4));
        var access = CreateDefaultAccess();

        uint value = rom.Read32(0, in access);

        Assert.That(value, Is.EqualTo(0x12345678u));
    }

    /// <summary>
    /// Verifies that Write32 is silently ignored.
    /// </summary>
    [Test]
    public void RomTarget_Write32_IsSilentlyIgnored()
    {
        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, 4));
        var access = CreateDefaultAccess();

        rom.Write32(0, 0xDEADBEEFu, in access);

        Assert.That(rom.Read32(0, in access), Is.EqualTo(0x12345678u), "ROM should not be modified by writes");
    }

    /// <summary>
    /// Verifies that ROM can hold typical Apple II ROM sizes.
    /// </summary>
    [Test]
    public void RomTarget_CanHoldTypicalRomSize()
    {
        // 16KB ROM like the Apple II
        var data = new byte[16 * 1024];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i & 0xFF);
        }

        var physicalMemory = new PhysicalMemory(data, "Apple II ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, data.Length));
        var access = CreateDefaultAccess();

        Assert.Multiple(() =>
        {
            Assert.That(rom.Size, Is.EqualTo(16 * 1024));
            Assert.That(rom.Read8(0, in access), Is.EqualTo(0));
            Assert.That(rom.Read8(256, in access), Is.EqualTo(0));
            Assert.That(rom.Read8(16383, in access), Is.EqualTo(0xFF));
        });
    }

    /// <summary>
    /// Verifies that Read16 at non-zero offset works correctly.
    /// </summary>
    [Test]
    public void RomTarget_Read16_AtNonZeroOffset_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x00, 0x00, 0x34, 0x12 };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, 4));
        var access = CreateDefaultAccess();

        ushort value = rom.Read16(2, in access);

        Assert.That(value, Is.EqualTo(0x1234));
    }

    /// <summary>
    /// Verifies that Read32 at non-zero offset works correctly.
    /// </summary>
    [Test]
    public void RomTarget_Read32_AtNonZeroOffset_ReturnsCorrectValue()
    {
        var data = new byte[] { 0x00, 0x00, 0x78, 0x56, 0x34, 0x12 };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom = new RomTarget(physicalMemory.ReadOnlySlice(0, 6));
        var access = CreateDefaultAccess();

        uint value = rom.Read32(2, in access);

        Assert.That(value, Is.EqualTo(0x12345678u));
    }

    /// <summary>
    /// Verifies that multiple RomTargets can share the same physical memory.
    /// </summary>
    [Test]
    public void RomTarget_MultipleTargets_SharePhysicalMemory()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };
        var physicalMemory = new PhysicalMemory(data, "Test ROM");
        var rom1 = new RomTarget(physicalMemory.ReadOnlySlice(0, 4));
        var rom2 = new RomTarget(physicalMemory.ReadOnlySlice(2, 4)); // Overlaps with rom1
        var access = CreateDefaultAccess();

        // Both should see the same data at overlapping region
        Assert.Multiple(() =>
        {
            Assert.That(rom1.Read8(2, in access), Is.EqualTo(0x33));
            Assert.That(rom2.Read8(0, in access), Is.EqualTo(0x33));
        });
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
        Flags: AccessFlags.None);
}