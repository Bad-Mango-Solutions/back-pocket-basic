// <copyright file="DebugLogWindowSink.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug;

using BadMango.Emulator.Debug.Infrastructure;

using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Serilog sink that forwards warning and error events to the debug log window stream.
/// </summary>
public sealed class DebugLogWindowSink : ILogEventSink
{
    private readonly IDebugLogEventStream logEventStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLogWindowSink"/> class.
    /// </summary>
    /// <param name="logEventStream">The stream that receives log events for UI display.</param>
    public DebugLogWindowSink(IDebugLogEventStream logEventStream)
    {
        ArgumentNullException.ThrowIfNull(logEventStream);
        this.logEventStream = logEventStream;
    }

    /// <inheritdoc/>
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        var level = logEvent.Level switch
        {
            LogEventLevel.Fatal => DebugLogLevel.Fatal,
            LogEventLevel.Error => DebugLogLevel.Error,
            _ => DebugLogLevel.Warning,
        };

        var message = logEvent.RenderMessage();
        if (logEvent.Exception is not null)
        {
            message = $"{message}{Environment.NewLine}{logEvent.Exception}";
        }

        this.logEventStream.Enqueue(new DebugLogEntry(logEvent.Timestamp, level, message));
    }
}