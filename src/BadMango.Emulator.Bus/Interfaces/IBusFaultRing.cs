// <copyright file="IBusFaultRing.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Read-only view onto a fixed-capacity ring buffer of recent bus faults.
/// </summary>
/// <remarks>
/// <para>
/// Debug tooling (e.g. the <c>fault</c> and <c>buslog</c> debug commands)
/// queries this interface to render recent <see cref="BusFault"/> events
/// without having to know the concrete recorder implementation.
/// </para>
/// <para>
/// Implementations of this interface are expected to be thread-safe so the
/// CPU thread can append new faults while the debug console reads snapshots
/// from another thread.
/// </para>
/// </remarks>
public interface IBusFaultRing
{
    /// <summary>
    /// Gets the maximum number of faults the buffer can hold before the
    /// oldest entries are overwritten.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// Gets the number of faults currently stored in the buffer
    /// (never larger than <see cref="Capacity"/>).
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the total number of faults ever recorded by this ring, including
    /// any that have been overwritten and dropped from the buffer.
    /// </summary>
    ulong TotalFaults { get; }

    /// <summary>
    /// Gets the most recent fault recorded, or <see langword="null"/>
    /// if no faults have ever been recorded.
    /// </summary>
    BusFault? Last { get; }

    /// <summary>
    /// Returns a snapshot of the buffer contents in chronological order
    /// (oldest fault first, most recent fault last).
    /// </summary>
    /// <returns>A newly allocated array containing the buffer contents.</returns>
    BusFault[] Snapshot();

    /// <summary>
    /// Discards all faults currently in the buffer and resets the
    /// <see cref="TotalFaults"/> counter to zero.
    /// </summary>
    void Clear();
}