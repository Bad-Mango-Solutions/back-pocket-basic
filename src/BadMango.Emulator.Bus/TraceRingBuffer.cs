// <copyright file="TraceRingBuffer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

/// <summary>
/// Ring buffer for trace event storage with zero allocations during normal operation.
/// </summary>
/// <remarks>
/// <para>
/// This buffer provides efficient storage for bus access trace events. It uses a
/// fixed-size array and wraps around when full, overwriting the oldest events.
/// The capacity must be a power of 2 for efficient index wrapping.
/// </para>
/// <para>
/// The buffer is designed for the observability plane of the bus architecture.
/// When tracing is enabled, every bus access can be recorded for later analysis.
/// </para>
/// <para>
/// Thread safety: This buffer is NOT thread-safe. If concurrent access is needed,
/// external synchronization must be provided.
/// </para>
/// </remarks>
public sealed class TraceRingBuffer
{
    private readonly BusTraceEvent[] buffer;
    private readonly int mask;
    private int writeIndex;
    private long totalWritten;

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceRingBuffer"/> class.
    /// </summary>
    /// <param name="capacity">
    /// The capacity of the buffer. Must be a power of 2.
    /// Defaults to 65536 (64K entries).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="capacity"/> is not a power of 2 or is less than 2.
    /// </exception>
    public TraceRingBuffer(int capacity = 65536)
    {
        if (capacity < 2 || (capacity & (capacity - 1)) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "Capacity must be a power of 2 and at least 2.");
        }

        buffer = new BusTraceEvent[capacity];
        mask = capacity - 1;
    }

    /// <summary>
    /// Gets the capacity of the buffer.
    /// </summary>
    public int Capacity => buffer.Length;

    /// <summary>
    /// Gets the number of events currently in the buffer.
    /// </summary>
    /// <value>
    /// The count of events, up to <see cref="Capacity"/>.
    /// </value>
    public int Count => (int)Math.Min(totalWritten, buffer.Length);

    /// <summary>
    /// Gets the total number of events written to this buffer since creation or last clear.
    /// </summary>
    /// <value>
    /// The total write count. If greater than <see cref="Capacity"/>, some events have been overwritten.
    /// </value>
    public long TotalWritten => totalWritten;

    /// <summary>
    /// Gets whether any events have been overwritten due to buffer wrap-around.
    /// </summary>
    public bool HasOverwritten => totalWritten > buffer.Length;

    /// <summary>
    /// Gets the event at the specified index in the buffer.
    /// </summary>
    /// <param name="index">
    /// The index from the start of the buffer (0 = oldest available event).
    /// </param>
    /// <returns>The trace event at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is negative or greater than or equal to <see cref="Count"/>.
    /// </exception>
    public ref readonly BusTraceEvent this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be between 0 and {Count - 1}.");
            }

            // If we haven't wrapped, start from 0
            // If we have wrapped, start from the oldest entry
            int start = totalWritten <= buffer.Length
                ? 0
                : writeIndex;

            return ref buffer[(start + index) & mask];
        }
    }

    /// <summary>
    /// Emits a trace event to the buffer.
    /// </summary>
    /// <param name="evt">The event to record.</param>
    /// <remarks>
    /// This method is designed for hot-path usage. It performs no allocations
    /// and uses a single array index operation with bitwise AND for wrapping.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(in BusTraceEvent evt)
    {
        buffer[writeIndex & mask] = evt;
        writeIndex++;
        totalWritten++;
    }

    /// <summary>
    /// Clears all events from the buffer.
    /// </summary>
    public void Clear()
    {
        writeIndex = 0;
        totalWritten = 0;
        Array.Clear(buffer);
    }

    /// <summary>
    /// Gets all events in chronological order.
    /// </summary>
    /// <returns>An enumerable of all trace events, oldest first.</returns>
    /// <remarks>
    /// This method allocates an enumerator. For performance-critical code,
    /// use the indexer directly.
    /// </remarks>
    public IEnumerable<BusTraceEvent> GetAll()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            yield return this[i];
        }
    }

    /// <summary>
    /// Gets the most recent events.
    /// </summary>
    /// <param name="count">The maximum number of events to retrieve.</param>
    /// <returns>An enumerable of the most recent events, oldest first.</returns>
    public IEnumerable<BusTraceEvent> GetRecent(int count)
    {
        int available = Count;
        int skip = Math.Max(0, available - count);
        int take = Math.Min(count, available);

        for (int i = 0; i < take; i++)
        {
            yield return this[skip + i];
        }
    }
}