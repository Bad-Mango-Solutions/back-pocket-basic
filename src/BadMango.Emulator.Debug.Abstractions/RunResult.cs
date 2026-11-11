// <copyright file="RunResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

/// <summary>
/// The result of a run command.
/// </summary>
/// <param name="StopReason">The reason execution stopped.</param>
/// <param name="InstructionCount">The number of instructions processed.</param>
/// <param name="CycleCount">The total cycles consumed.</param>
/// <param name="Elapsed">The total amount of wall-clock time consumed.</param>
/// <param name="ProgramCounter">The final program counter.</param>
/// <param name="Message">An optional status message.</param>
public sealed record RunResult(
    StopReason StopReason,
    ulong InstructionCount,
    ulong CycleCount,
    TimeSpan Elapsed,
    uint ProgramCounter,
    string? Message = null);