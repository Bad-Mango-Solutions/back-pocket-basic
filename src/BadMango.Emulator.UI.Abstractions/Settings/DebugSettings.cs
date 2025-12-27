// <copyright file="DebugSettings.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Settings;

/// <summary>
/// Debug and logging settings.
/// </summary>
public record DebugSettings
{
    /// <summary>
    /// Gets a value indicating whether to attach the debugger on instance start.
    /// </summary>
    public bool AutoAttachDebugger { get; init; }

    /// <summary>
    /// Gets a value indicating whether to break into the debugger on reset.
    /// </summary>
    public bool BreakOnReset { get; init; }

    /// <summary>
    /// Gets the minimum log level (Verbose, Debug, Information, Warning, Error, Fatal).
    /// </summary>
    public string LogLevel { get; init; } = "Information";

    /// <summary>
    /// Gets a value indicating whether to write logs to a file.
    /// </summary>
    public bool LogToFile { get; init; } = true;

    /// <summary>
    /// Gets the maximum log file size in megabytes.
    /// </summary>
    public int MaxLogFileSizeMB { get; init; } = 10;

    /// <summary>
    /// Gets a value indicating whether instruction tracing is enabled.
    /// </summary>
    public bool TraceInstructions { get; init; }

    /// <summary>
    /// Gets a value indicating whether to display cycle count in the debugger.
    /// </summary>
    public bool ShowCycleCount { get; init; } = true;
}