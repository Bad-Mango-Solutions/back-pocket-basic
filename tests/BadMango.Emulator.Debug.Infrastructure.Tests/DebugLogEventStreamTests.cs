// <copyright file="DebugLogEventStreamTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="DebugLogEventStream"/>.
/// </summary>
[TestFixture]
public class DebugLogEventStreamTests
{
    /// <summary>
    /// Verifies that enqueued entries can be dequeued in FIFO order.
    /// </summary>
    [Test]
    public void EnqueueAndTryDequeue_ReturnsEntriesInOrder()
    {
        var stream = new DebugLogEventStream();
        var first = new DebugLogEntry(DateTimeOffset.UtcNow, DebugLogLevel.Warning, "first");
        var second = new DebugLogEntry(DateTimeOffset.UtcNow, DebugLogLevel.Error, "second");

        stream.Enqueue(first);
        stream.Enqueue(second);

        var firstRead = stream.TryDequeue(out var readFirst);
        var secondRead = stream.TryDequeue(out var readSecond);
        var thirdRead = stream.TryDequeue(out var readThird);

        Assert.Multiple(() =>
        {
            Assert.That(firstRead, Is.True);
            Assert.That(secondRead, Is.True);
            Assert.That(thirdRead, Is.False);
            Assert.That(readFirst, Is.EqualTo(first));
            Assert.That(readSecond, Is.EqualTo(second));
            Assert.That(readThird, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that dequeue on an empty stream returns false.
    /// </summary>
    [Test]
    public void TryDequeue_WhenEmpty_ReturnsFalse()
    {
        var stream = new DebugLogEventStream();

        var read = stream.TryDequeue(out var entry);

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.False);
            Assert.That(entry, Is.Null);
        });
    }
}