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
    /// Verifies that RamTarget can be created with specified size.
    /// </summary>
    [Test]
    public void RamTarget_CanBeCreatedWithSize()
    {
        var ram = new RamTarget(1024);

        Assert.That(ram.Size, Is.EqualTo(1024));
    }

    /// <summary>
    /// Verifies that RamTarget can be created with initial data.
    /// </summary>
    [Test]
    public void RamTarget_CanBeCreatedWithInitialData()
    {
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var ram = new RamTarget(data);

        Assert.That(ram.Size, Is.EqualTo(4));
    }

    /// <summary>
    /// Verifies that constructor throws for zero size.
    /// </summary>
    [Test]
    public void RamTarget_Constructor_ThrowsForZeroSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RamTarget(0));
    }

    /// <summary>
    /// Verifies that constructor throws for negative size.
    /// </summary>
    [Test]
    public void RamTarget_Constructor_ThrowsForNegativeSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RamTarget(-1));
    }

    /// <summary>
    /// Verifies that constructor throws for null data.
    /// </summary>
    [Test]
    public void RamTarget_Constructor_ThrowsForNullData()
    {
        Assert.Throws<ArgumentNullException>(() => new RamTarget((byte[])null!));
    }

    /// <summary>
    /// Verifies that Capabilities includes expected flags.
    /// </summary>
    [Test]
    public void RamTarget_Capabilities_IncludesExpectedFlags()
    {
        var ram = new RamTarget(64);

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
        var ram = new RamTarget(64);
        var access = CreateDefaultAccess();

        ram.Write8(10, 0xAB, in access);
        byte value = ram.Read8(10, in access);

        Assert.That(value, Is.EqualTo(0xAB));
    }

    /// <summary>
    /// Verifies that Read8 returns initial data value.
    /// </summary>
    [Test]
    public void RamTarget_Read8_ReturnsInitialDataValue()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var ram = new RamTarget(data);
        var access = CreateDefaultAccess();

        byte value = ram.Read8(2, in access);

        Assert.That(value, Is.EqualTo(0x33));
    }

    /// <summary>
    /// Verifies that initial data is copied (immutable).
    /// </summary>
    [Test]
    public void RamTarget_InitialData_IsCopied()
    {
        var data = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var ram = new RamTarget(data);

        // Modify original data
        data[0] = 0xFF;

        var access = CreateDefaultAccess();
        byte value = ram.Read8(0, in access);

        Assert.That(value, Is.EqualTo(0x11), "RAM should not be affected by changes to original array");
    }

    /// <summary>
    /// Verifies that Read16 returns little-endian value.
    /// </summary>
    [Test]
    public void RamTarget_Read16_ReturnsLittleEndianValue()
    {
        var ram = new RamTarget(64);
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
        var ram = new RamTarget(64);
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
        var ram = new RamTarget(64);
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
        var ram = new RamTarget(64);
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
    /// Verifies that Fill sets all bytes to specified value.
    /// </summary>
    [Test]
    public void RamTarget_Fill_SetsAllBytesToValue()
    {
        var ram = new RamTarget(16);
        var access = CreateDefaultAccess();

        ram.Fill(0xCC);

        Assert.Multiple(() =>
        {
            Assert.That(ram.Read8(0, in access), Is.EqualTo(0xCC));
            Assert.That(ram.Read8(7, in access), Is.EqualTo(0xCC));
            Assert.That(ram.Read8(15, in access), Is.EqualTo(0xCC));
        });
    }

    /// <summary>
    /// Verifies that Clear sets all bytes to zero.
    /// </summary>
    [Test]
    public void RamTarget_Clear_SetsAllBytesToZero()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var ram = new RamTarget(data);
        var access = CreateDefaultAccess();

        ram.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(ram.Read8(0, in access), Is.EqualTo(0));
            Assert.That(ram.Read8(1, in access), Is.EqualTo(0));
            Assert.That(ram.Read8(2, in access), Is.EqualTo(0));
            Assert.That(ram.Read8(3, in access), Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that AsSpan returns writable span.
    /// </summary>
    [Test]
    public void RamTarget_AsSpan_ReturnsWritableSpan()
    {
        var ram = new RamTarget(8);
        var access = CreateDefaultAccess();

        var span = ram.AsSpan();
        span[0] = 0xAA;
        span[7] = 0xBB;

        Assert.Multiple(() =>
        {
            Assert.That(ram.Read8(0, in access), Is.EqualTo(0xAA));
            Assert.That(ram.Read8(7, in access), Is.EqualTo(0xBB));
        });
    }

    /// <summary>
    /// Verifies that AsReadOnlySpan returns readable span.
    /// </summary>
    [Test]
    public void RamTarget_AsReadOnlySpan_ReturnsReadableSpan()
    {
        var data = new byte[] { 0x11, 0x22, 0x33 };
        var ram = new RamTarget(data);

        var span = ram.AsReadOnlySpan();

        Assert.That(span.Length, Is.EqualTo(3));
        Assert.That(span[0], Is.EqualTo(0x11));
        Assert.That(span[1], Is.EqualTo(0x22));
        Assert.That(span[2], Is.EqualTo(0x33));
    }

    /// <summary>
    /// Verifies that new RAM is initialized to zero.
    /// </summary>
    [Test]
    public void RamTarget_NewRam_InitializedToZero()
    {
        var ram = new RamTarget(4);
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
        var ram = new RamTarget(64);
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
        var ram = new RamTarget(64);
        var access = CreateDefaultAccess();

        ram.Write32(20, 0x12345678u, in access);
        uint value = ram.Read32(20, in access);

        Assert.That(value, Is.EqualTo(0x12345678u));
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