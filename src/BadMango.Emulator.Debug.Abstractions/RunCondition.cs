// <copyright file="RunCondition.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

/// <summary>
/// A machine condition that can be used with <see cref="IDebugSession.RunUntil(RunCondition, RunBudget, CancellationToken)"/>.
/// </summary>
/// <param name="Kind">The kind of condition.</param>
/// <param name="Address">The address associated with the condition.</param>
/// <param name="BreakpointAddress">The breakpoint address used when the condition is a breakpoint trigger.</param>
/// <param name="WatchAddress">The watchpoint address used when the condition is a watch trigger.</param>
/// <param name="MemoryAddress">The address to test when the condition compares memory.</param>
/// <param name="MemoryValue">The expected value to compare against memory.</param>
/// <param name="CycleLimit">The cycle threshold when the condition is a cycle-bound stop.</param>
/// <param name="Label">An optional label.</param>
public sealed record RunCondition(
    RunConditionKind Kind,
    uint? Address = null,
    uint? BreakpointAddress = null,
    uint? WatchAddress = null,
    uint? MemoryAddress = null,
    byte[]? MemoryValue = null,
    ulong? CycleLimit = null,
    string? Label = null);