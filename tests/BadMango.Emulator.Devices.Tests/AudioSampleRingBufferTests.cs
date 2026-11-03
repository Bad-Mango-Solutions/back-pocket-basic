// <copyright file="AudioSampleRingBufferTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

/// <summary>
/// Unit tests for <see cref="AudioSampleRingBuffer"/>.
/// </summary>
[TestFixture]
public class AudioSampleRingBufferTests
{
    /// <summary>
    /// Verifies that samples are read back in FIFO order.
    /// </summary>
    [Test]
    public void WriteThenRead_ReturnsSamplesInOrder()
    {
        var ring = new AudioSampleRingBuffer(capacity: 8);
        ring.Write([1, 2, 3, 4]);
        var destination = new short[4];

        int read = ring.Read(destination);

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.EqualTo(4));
            Assert.That(destination, Is.EqualTo(new short[] { 1, 2, 3, 4 }));
            Assert.That(ring.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that overflow drops the oldest samples to keep latency bounded.
    /// </summary>
    [Test]
    public void Write_WhenOverflowing_DropsOldestSamples()
    {
        var ring = new AudioSampleRingBuffer(capacity: 5);
        ring.Write([10, 20, 30, 40]);
        ring.Write([50, 60, 70]);
        var destination = new short[5];

        int read = ring.Read(destination);

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.EqualTo(5));
            Assert.That(destination, Is.EqualTo(new short[] { 30, 40, 50, 60, 70 }));
            Assert.That(ring.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that writes larger than capacity keep only the newest tail samples.
    /// </summary>
    [Test]
    public void Write_WhenInputExceedsCapacity_KeepsNewestTail()
    {
        var ring = new AudioSampleRingBuffer(capacity: 4);
        ring.Write([1, 2, 3, 4, 5, 6]);
        var destination = new short[4];

        int read = ring.Read(destination);

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.EqualTo(4));
            Assert.That(destination, Is.EqualTo(new short[] { 3, 4, 5, 6 }));
            Assert.That(ring.Count, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that reading from an empty buffer returns zero and leaves destination unchanged.
    /// </summary>
    [Test]
    public void Read_WhenEmpty_ReturnsZero()
    {
        var ring = new AudioSampleRingBuffer(capacity: 3);
        var destination = new short[] { 99, 98 };

        int read = ring.Read(destination);

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.EqualTo(0));
            Assert.That(destination, Is.EqualTo(new short[] { 99, 98 }));
            Assert.That(ring.Count, Is.EqualTo(0));
        });
    }
}