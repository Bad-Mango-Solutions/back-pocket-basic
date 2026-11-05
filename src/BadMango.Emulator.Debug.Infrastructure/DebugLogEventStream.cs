// <copyright file="DebugLogEventStream.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using System.Threading.Channels;

/// <summary>
/// Bounded thread-safe in-memory stream for UI log window events.
/// </summary>
public sealed class DebugLogEventStream : IDebugLogEventStream
{
    private const int MaxPendingEntries = 2048;

    private readonly Channel<DebugLogEntry> channel = Channel.CreateBounded<DebugLogEntry>(new BoundedChannelOptions(MaxPendingEntries)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false,
    });

    /// <inheritdoc/>
    public void Enqueue(DebugLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _ = this.channel.Writer.TryWrite(entry);
    }

    /// <inheritdoc/>
    public bool TryDequeue(out DebugLogEntry? entry)
    {
        var read = this.channel.Reader.TryRead(out var dequeued);
        entry = dequeued;
        return read;
    }
}