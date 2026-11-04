// <copyright file="BusFaultRing.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// A thread-safe, fixed-capacity ring buffer of recent <see cref="BusFault"/> events.
/// </summary>
/// <remarks>
/// <para>
/// The ring buffer is the canonical sink for bus faults emitted by
/// <see cref="MainBus"/>. It keeps the <i>N</i> most recent faults and
/// drops older entries when the buffer wraps. Total fault counts are
/// preserved separately so debug tooling can tell how many faults have
/// been dropped from the visible window.
/// </para>
/// <para>
/// The class implements both <see cref="IBusFaultRecorder"/> (the
/// write-side contract used by the bus on the hot path) and
/// <see cref="IBusFaultRing"/> (the read-side contract used by debug
/// commands), so a single instance can be registered for both roles.
/// </para>
/// </remarks>
public sealed class BusFaultRing : IBusFaultRecorder, IBusFaultRing
{
    /// <summary>
    /// The default capacity used when no explicit value is supplied.
    /// </summary>
    public const int DefaultCapacity = 256;

    private readonly BusFault[] buffer;
    private readonly Lock syncLock = new();

    private int writeIndex;
    private int count;
    private ulong totalFaults;
    private BusFault? lastFault;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusFaultRing"/> class.
    /// </summary>
    /// <param name="capacity">
    /// Maximum number of faults retained in the buffer. Must be positive.
    /// Defaults to <see cref="DefaultCapacity"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="capacity"/> is less than one.
    /// </exception>
    public BusFaultRing(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        buffer = new BusFault[capacity];
    }

    /// <inheritdoc />
    public int Capacity => buffer.Length;

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (syncLock)
            {
                return count;
            }
        }
    }

    /// <inheritdoc />
    public ulong TotalFaults
    {
        get
        {
            lock (syncLock)
            {
                return totalFaults;
            }
        }
    }

    /// <inheritdoc />
    public BusFault? Last
    {
        get
        {
            lock (syncLock)
            {
                return lastFault;
            }
        }
    }

    /// <inheritdoc />
    public void Record(in BusFault fault)
    {
        if (!fault.IsFault)
        {
            return;
        }

        lock (syncLock)
        {
            buffer[writeIndex] = fault;
            writeIndex = (writeIndex + 1) % buffer.Length;

            if (count < buffer.Length)
            {
                count++;
            }

            totalFaults++;
            lastFault = fault;
        }
    }

    /// <inheritdoc />
    public BusFault[] Snapshot()
    {
        lock (syncLock)
        {
            if (count == 0)
            {
                return [];
            }

            var snapshot = new BusFault[count];
            int start = count < buffer.Length
                ? 0
                : writeIndex;

            for (int i = 0; i < count; i++)
            {
                snapshot[i] = buffer[(start + i) % buffer.Length];
            }

            return snapshot;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (syncLock)
        {
            Array.Clear(buffer);
            writeIndex = 0;
            count = 0;
            totalFaults = 0;
            lastFault = null;
        }
    }
}