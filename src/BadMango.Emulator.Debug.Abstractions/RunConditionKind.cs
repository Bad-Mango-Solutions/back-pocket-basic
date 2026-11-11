// <copyright file="RunConditionKind.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

using System.Text.Json.Serialization;

/// <summary>
/// Describes the kind of condition that can stop execution.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RunConditionKind>))]
public enum RunConditionKind
{
    /// <summary>
    /// Stop when the program counter reaches an address.
    /// </summary>
    ProgramCounter,

    /// <summary>
    /// Stop when a breakpoint is hit.
    /// </summary>
    Breakpoint,

    /// <summary>
    /// Stop when a watchpoint is hit.
    /// </summary>
    Watchpoint,

    /// <summary>
    /// Stop when a memory address matches an expected value.
    /// </summary>
    MemoryEquals,

    /// <summary>
    /// Stop after a cycle count is reached.
    /// </summary>
    CycleCount,
}