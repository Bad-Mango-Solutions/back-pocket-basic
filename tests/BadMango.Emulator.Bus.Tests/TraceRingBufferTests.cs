// <copyright file="TraceRingBufferTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="TraceRingBuffer"/> class.
/// </summary>
[TestFixture]
public class TraceRingBufferTests
{
    /// <summary>
    /// Verifies that the default constructor creates a buffer with 64K capacity.
    /// </summary>
    [Test]
    public void Constructor_Default_Creates64KBuffer()
    {
        var buffer = new TraceRingBuffer();
        Assert.That(buffer.Capacity, Is.EqualTo(65536));
    }

    /// <summary>
    /// Verifies that the constructor accepts a power of 2 capacity.
    /// </summary>
    [Test]
    public void Constructor_PowerOfTwo_CreatesBufferWithCapacity()
    {
        var buffer = new TraceRingBuffer(256);
        Assert.That(buffer.Capacity, Is.EqualTo(256));
    }

    /// <summary>
    /// Verifies that the constructor rejects non-power-of-2 capacity.
    /// </summary>
    [Test]
    public void Constructor_NonPowerOfTwo_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TraceRingBuffer(100));
    }

    /// <summary>
    /// Verifies that the constructor rejects capacity less than 2.
    /// </summary>
    [Test]
    public void Constructor_LessThanTwo_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TraceRingBuffer(1));
    }

    /// <summary>
    /// Verifies that a new buffer starts empty.
    /// </summary>
    [Test]
    public void NewBuffer_StartsEmpty()
    {
        var buffer = new TraceRingBuffer(16);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.TotalWritten, Is.EqualTo(0L));
            Assert.That(buffer.HasOverwritten, Is.False);
        });
    }

    /// <summary>
    /// Verifies that Emit adds events to the buffer.
    /// </summary>
    [Test]
    public void Emit_AddsEventsToBuffer()
    {
        var buffer = new TraceRingBuffer(16);
        var evt = CreateTestEvent(cycle: 100, address: 0x1000, value: 0x42);

        buffer.Emit(evt);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer.TotalWritten, Is.EqualTo(1L));
        });
    }

    /// <summary>
    /// Verifies that the indexer retrieves events correctly.
    /// </summary>
    [Test]
    public void Indexer_RetrievesEventsCorrectly()
    {
        var buffer = new TraceRingBuffer(16);
        buffer.Emit(CreateTestEvent(cycle: 1, address: 0x1000, value: 0x11));
        buffer.Emit(CreateTestEvent(cycle: 2, address: 0x2000, value: 0x22));
        buffer.Emit(CreateTestEvent(cycle: 3, address: 0x3000, value: 0x33));

        Assert.Multiple(() =>
        {
            Assert.That(buffer[0].Cycle, Is.EqualTo(1ul));
            Assert.That(buffer[1].Cycle, Is.EqualTo(2ul));
            Assert.That(buffer[2].Cycle, Is.EqualTo(3ul));
        });
    }

    /// <summary>
    /// Verifies that the indexer throws for out of range index.
    /// </summary>
    [Test]
    public void Indexer_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var buffer = new TraceRingBuffer(16);
        buffer.Emit(CreateTestEvent(cycle: 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[1]);
    }

    /// <summary>
    /// Verifies that the indexer throws for negative index.
    /// </summary>
    [Test]
    public void Indexer_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        var buffer = new TraceRingBuffer(16);
        buffer.Emit(CreateTestEvent(cycle: 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[-1]);
    }

    /// <summary>
    /// Verifies that the buffer wraps around when full.
    /// </summary>
    [Test]
    public void Buffer_WrapsAroundWhenFull()
    {
        var buffer = new TraceRingBuffer(4);

        // Emit 6 events in a buffer of 4
        for (int i = 0; i < 6; i++)
        {
            buffer.Emit(CreateTestEvent(cycle: (ulong)i, value: (uint)i));
        }

        Assert.Multiple(() =>
        {
            Assert.That(buffer.Count, Is.EqualTo(4));
            Assert.That(buffer.TotalWritten, Is.EqualTo(6L));
            Assert.That(buffer.HasOverwritten, Is.True);

            // Events 0 and 1 are overwritten; oldest is now event 2
            Assert.That(buffer[0].Value, Is.EqualTo(2u));
            Assert.That(buffer[1].Value, Is.EqualTo(3u));
            Assert.That(buffer[2].Value, Is.EqualTo(4u));
            Assert.That(buffer[3].Value, Is.EqualTo(5u));
        });
    }

    /// <summary>
    /// Verifies that Clear resets the buffer.
    /// </summary>
    [Test]
    public void Clear_ResetsBuffer()
    {
        var buffer = new TraceRingBuffer(16);
        buffer.Emit(CreateTestEvent(cycle: 1));
        buffer.Emit(CreateTestEvent(cycle: 2));

        buffer.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.TotalWritten, Is.EqualTo(0L));
            Assert.That(buffer.HasOverwritten, Is.False);
        });
    }

    /// <summary>
    /// Verifies that GetAll returns all events in order.
    /// </summary>
    [Test]
    public void GetAll_ReturnsAllEventsInOrder()
    {
        var buffer = new TraceRingBuffer(16);
        buffer.Emit(CreateTestEvent(cycle: 1));
        buffer.Emit(CreateTestEvent(cycle: 2));
        buffer.Emit(CreateTestEvent(cycle: 3));

        var events = buffer.GetAll().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(3));
            Assert.That(events[0].Cycle, Is.EqualTo(1ul));
            Assert.That(events[1].Cycle, Is.EqualTo(2ul));
            Assert.That(events[2].Cycle, Is.EqualTo(3ul));
        });
    }

    /// <summary>
    /// Verifies that GetRecent returns the most recent events.
    /// </summary>
    [Test]
    public void GetRecent_ReturnsMostRecentEvents()
    {
        var buffer = new TraceRingBuffer(16);
        for (int i = 1; i <= 10; i++)
        {
            buffer.Emit(CreateTestEvent(cycle: (ulong)i));
        }

        var recent = buffer.GetRecent(3).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(recent, Has.Count.EqualTo(3));
            Assert.That(recent[0].Cycle, Is.EqualTo(8ul));
            Assert.That(recent[1].Cycle, Is.EqualTo(9ul));
            Assert.That(recent[2].Cycle, Is.EqualTo(10ul));
        });
    }

    /// <summary>
    /// Verifies that GetRecent handles request for more than available.
    /// </summary>
    [Test]
    public void GetRecent_RequestMoreThanAvailable_ReturnsAll()
    {
        var buffer = new TraceRingBuffer(16);
        buffer.Emit(CreateTestEvent(cycle: 1));
        buffer.Emit(CreateTestEvent(cycle: 2));

        var recent = buffer.GetRecent(10).ToList();

        Assert.That(recent, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Verifies buffer behavior after wrap-around with GetAll.
    /// </summary>
    [Test]
    public void GetAll_AfterWrapAround_ReturnsEventsInCorrectOrder()
    {
        var buffer = new TraceRingBuffer(4);

        // Emit 6 events in a buffer of 4
        for (int i = 0; i < 6; i++)
        {
            buffer.Emit(CreateTestEvent(cycle: (ulong)i, value: (uint)i));
        }

        var events = buffer.GetAll().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(4));
            Assert.That(events[0].Value, Is.EqualTo(2u)); // Oldest surviving
            Assert.That(events[1].Value, Is.EqualTo(3u));
            Assert.That(events[2].Value, Is.EqualTo(4u));
            Assert.That(events[3].Value, Is.EqualTo(5u)); // Most recent
        });
    }

    /// <summary>
    /// Helper to create a test trace event.
    /// </summary>
    private static BusTraceEvent CreateTestEvent(
        ulong cycle = 0,
        uint address = 0,
        uint value = 0)
    {
        return new BusTraceEvent(
            cycle: cycle,
            address: address,
            value: value,
            widthBits: 8,
            intent: AccessIntent.DataRead,
            flags: AccessFlags.None,
            sourceId: 0,
            deviceId: 0,
            regionTag: RegionTag.Ram);
    }
}