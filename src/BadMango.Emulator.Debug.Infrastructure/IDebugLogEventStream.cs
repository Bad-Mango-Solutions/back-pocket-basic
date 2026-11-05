// <copyright file="IDebugLogEventStream.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

/// <summary>
/// Provides a thread-safe stream of warning and error log events for debug UI consumers.
/// </summary>
public interface IDebugLogEventStream
{
    /// <summary>
    /// Enqueues a log entry for later UI consumption.
    /// </summary>
    /// <param name="entry">The log entry to enqueue.</param>
    void Enqueue(DebugLogEntry entry);

    /// <summary>
    /// Attempts to dequeue one log entry.
    /// </summary>
    /// <param name="entry">When this method returns, contains the dequeued entry when successful.</param>
    /// <returns><c>true</c> when an entry was dequeued; otherwise <c>false</c>.</returns>
    bool TryDequeue(out DebugLogEntry? entry);
}