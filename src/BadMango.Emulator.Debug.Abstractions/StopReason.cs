// <copyright file="StopReason.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

using System.Text.Json.Serialization;

/// <summary>
/// Describes why execution stopped.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<StopReason>))]
public enum StopReason
{
    /// <summary>
    /// The CPU halted or reached a terminal state.
    /// </summary>
    Halted,

    /// <summary>
    /// A configured breakpoint fired.
    /// </summary>
    Breakpoint,

    /// <summary>
    /// A configured watchpoint fired.
    /// </summary>
    Watchpoint,

    /// <summary>
    /// A memory compare condition fired.
    /// </summary>
    MemoryCondition,

    /// <summary>
    /// The cycle budget was exhausted.
    /// </summary>
    CycleLimit,

    /// <summary>
    /// The wall-clock budget was exhausted.
    /// </summary>
    WallClockLimit,

    /// <summary>
    /// Execution was paused.
    /// </summary>
    Paused,

    /// <summary>
    /// A reset completed.
    /// </summary>
    Reset,

    /// <summary>
    /// Execution was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Execution terminated on an error.
    /// </summary>
    Error,
}