// <copyright file="DebugLogEntry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

/// <summary>
/// Represents a single log entry delivered to the debug log window.
/// </summary>
/// <param name="Timestamp">The event timestamp.</param>
/// <param name="Level">The event severity level.</param>
/// <param name="Message">The rendered log message.</param>
public sealed record DebugLogEntry(
    DateTimeOffset Timestamp,
    DebugLogLevel Level,
    string Message);