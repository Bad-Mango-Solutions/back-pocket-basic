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
    /// Verifies that RomTarget can be created with data array.
    /// </summary>
    [Test]
    public void RomTarget_CanBeCreatedWithDataArray()
    {
        var data = new byte[] { 0xEA, 0x00, 0xFF };
        var rom = new RomTarget(data);

        Assert.That(rom.Size, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that RomTarget can be created with ReadOnlySpan.
    /// </summary>
    [Test]
    public void RomTarget_CanBeCreatedWithReadOnlySpan()
    {
        ReadOnlySpan<byte> data = stackalloc byte[] { 0xEA, 0x00, 0xFF };
        var rom = new RomTarget(data);

        Assert.That(rom.Size, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that constructor throws for null data.
    /// </summary>
    [Test]
    public void RomTarget_Constructor_ThrowsForNullData()
    {
        Assert.Throws<ArgumentNullException>(() => new RomTarget((byte[])null!));
    }

    /// <summary>
    /// Verifies that Capabilities includes expected flags.
    /// </summary>
    [Test]
    public void RomTarget_Capabilities_IncludesExpectedFlags()
    {
        var rom = new RomTarget(new byte[] { 0x00 });

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
        var rom = new RomTarget(data);
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
        var rom = new RomTarget(data);
        var access = CreateDefaultAccess();

        rom.Write8(0, 0xFF, in access);

        Assert.That(rom.Read8(0, in access), Is.EqualTo(0xAA), "ROM should not be modified by writes");
    }

    /// <summary>
    /// Verifies that ROM data is copied (immutable from source).
    /// </summary>
    [Test]
    public void RomTarget_Data_IsCopiedFromSource()
    {
        var data = new byte[] { 0x11, 0x22 };
        var rom = new RomTarget(data);

        // Modify original data
        data[0] = 0xFF;

        var access = CreateDefaultAccess();
        Assert.That(rom.Read8(0, in access), Is.EqualTo(0x11), "ROM should not be affected by changes to original array");
    }

    /// <summary>
    /// Verifies that Read16 returns little-endian value.
    /// </summary>
    [Test]
    public void RomTarget_Read16_ReturnsLittleEndianValue()
    {
        var data = new byte[] { 0x34, 0x12 };
        var rom = new RomTarget(data);
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
        var rom = new RomTarget(data);
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
        var rom = new RomTarget(data);
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
        var rom = new RomTarget(data);
        var access = CreateDefaultAccess();

        rom.Write32(0, 0xDEADBEEFu, in access);

        Assert.That(rom.Read32(0, in access), Is.EqualTo(0x12345678u), "ROM should not be modified by writes");
    }

    /// <summary>
    /// Verifies that AsReadOnlySpan returns readable span.
    /// </summary>
    [Test]
    public void RomTarget_AsReadOnlySpan_ReturnsReadableSpan()
    {
        var data = new byte[] { 0x11, 0x22, 0x33 };
        var rom = new RomTarget(data);

        var span = rom.AsReadOnlySpan();

        Assert.That(span.Length, Is.EqualTo(3));
        Assert.That(span[0], Is.EqualTo(0x11));
        Assert.That(span[1], Is.EqualTo(0x22));
        Assert.That(span[2], Is.EqualTo(0x33));
    }

    /// <summary>
    /// Verifies empty ROM creation.
    /// </summary>
    [Test]
    public void RomTarget_EmptyRom_HasZeroSize()
    {
        var rom = new RomTarget(Array.Empty<byte>());

        Assert.That(rom.Size, Is.EqualTo(0));
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

        var rom = new RomTarget(data);
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
        var rom = new RomTarget(data);
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
        var rom = new RomTarget(data);
        var access = CreateDefaultAccess();

        uint value = rom.Read32(2, in access);

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