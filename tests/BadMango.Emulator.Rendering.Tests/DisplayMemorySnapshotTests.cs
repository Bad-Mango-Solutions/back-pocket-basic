// <copyright file="DisplayMemorySnapshotTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering.Tests;

/// <summary>
/// Unit tests for the <see cref="DisplayMemorySnapshot"/> class.
/// </summary>
[TestFixture]
public class DisplayMemorySnapshotTests
{
    /// <summary>
    /// Verifies that the snapshot covers text pages and both hi-res pages.
    /// </summary>
    [Test]
    public void Range_CoversTextAndHiResPages()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DisplayMemorySnapshot.BaseAddress, Is.LessThanOrEqualTo(0x0400));
            Assert.That(DisplayMemorySnapshot.EndAddress, Is.GreaterThanOrEqualTo(0x6000));
        });
    }

    /// <summary>
    /// Verifies that capturing main RAM copies text, hi-res page 1 and hi-res page 2.
    /// </summary>
    /// <param name="address">Address to check.</param>
    [TestCase(0x0400)]
    [TestCase(0x0BFF)]
    [TestCase(0x2000)]
    [TestCase(0x3FFF)]
    [TestCase(0x4000)]
    [TestCase(0x5FFF)]
    public void CaptureMain_CopiesDisplayAddresses(int address)
    {
        var ram = CreatePatternRam(0x00);
        var snapshot = new DisplayMemorySnapshot();

        snapshot.CaptureMain(ram);

        Assert.That(snapshot.ReadMain((ushort)address), Is.EqualTo(ram[address]));
    }

    /// <summary>
    /// Verifies that capturing aux RAM copies hi-res pages independently of main RAM.
    /// </summary>
    /// <param name="address">Address to check.</param>
    [TestCase(0x0400)]
    [TestCase(0x2000)]
    [TestCase(0x5FFF)]
    public void CaptureAux_CopiesDisplayAddressesIndependentlyOfMain(int address)
    {
        var main = CreatePatternRam(0x00);
        var aux = CreatePatternRam(0x55);
        var snapshot = new DisplayMemorySnapshot();

        snapshot.CaptureMain(main);
        snapshot.CaptureAux(aux);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.ReadMain((ushort)address), Is.EqualTo(main[address]));
            Assert.That(snapshot.ReadAux((ushort)address), Is.EqualTo(aux[address]));
            Assert.That(snapshot.ReadAux((ushort)address), Is.Not.EqualTo(snapshot.ReadMain((ushort)address)));
        });
    }

    /// <summary>
    /// Verifies that the snapshot is a copy: later RAM writes are not visible until the next capture.
    /// </summary>
    [Test]
    public void CaptureMain_IsACopy()
    {
        var ram = new byte[0x10000];
        ram[0x2000] = 0x11;
        var snapshot = new DisplayMemorySnapshot();

        snapshot.CaptureMain(ram);
        ram[0x2000] = 0x22;

        Assert.That(snapshot.ReadMain(0x2000), Is.EqualTo(0x11));

        snapshot.CaptureMain(ram);

        Assert.That(snapshot.ReadMain(0x2000), Is.EqualTo(0x22));
    }

    /// <summary>
    /// Verifies that addresses outside the snapshot range read as zero.
    /// </summary>
    /// <param name="address">Address outside the range.</param>
    [TestCase(0x0000)]
    [TestCase(0x03FF)]
    [TestCase(0x6000)]
    [TestCase(0xFFFF)]
    public void Read_OutsideRange_ReturnsZero(int address)
    {
        var ram = new byte[0x10000];
        Array.Fill(ram, (byte)0xFF);
        var snapshot = new DisplayMemorySnapshot();

        snapshot.CaptureMain(ram);
        snapshot.CaptureAux(ram);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.ReadMain((ushort)address), Is.Zero);
            Assert.That(snapshot.ReadAux((ushort)address), Is.Zero);
        });
    }

    /// <summary>
    /// Verifies that a span too small to cover the display range is rejected.
    /// </summary>
    [Test]
    public void CaptureMain_SpanTooSmall_Throws()
    {
        var snapshot = new DisplayMemorySnapshot();

        Assert.Throws<ArgumentException>(() => snapshot.CaptureMain(new byte[0x5FFF]));
    }

    /// <summary>
    /// Verifies that the per-byte fallback reads every address in the range.
    /// </summary>
    [Test]
    public void CaptureMain_WithReader_ReadsWholeRange()
    {
        var snapshot = new DisplayMemorySnapshot();
        int reads = 0;

        snapshot.CaptureMain(address =>
        {
            reads++;
            return (byte)(address >> 8);
        });

        Assert.Multiple(() =>
        {
            Assert.That(reads, Is.EqualTo(DisplayMemorySnapshot.Size));
            Assert.That(snapshot.ReadMain(0x2000), Is.EqualTo(0x20));
            Assert.That(snapshot.ReadMain(0x5FFF), Is.EqualTo(0x5F));
        });
    }

    /// <summary>
    /// Verifies that Clear zeroes both banks.
    /// </summary>
    [Test]
    public void Clear_ZeroesBothBanks()
    {
        var ram = new byte[0x10000];
        Array.Fill(ram, (byte)0xAA);
        var snapshot = new DisplayMemorySnapshot();
        snapshot.CaptureMain(ram);
        snapshot.CaptureAux(ram);

        snapshot.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.ReadMain(0x2000), Is.Zero);
            Assert.That(snapshot.ReadAux(0x2000), Is.Zero);
        });
    }

    private static byte[] CreatePatternRam(byte salt)
    {
        var ram = new byte[0x10000];
        for (int i = 0; i < ram.Length; i++)
        {
            ram[i] = (byte)((i ^ (i >> 8) ^ salt) | 0x01);
        }

        return ram;
    }
}