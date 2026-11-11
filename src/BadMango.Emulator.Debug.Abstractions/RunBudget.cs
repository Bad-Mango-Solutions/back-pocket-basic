// <copyright file="RunBudget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

/// <summary>
/// The resource budget enforced while a run is active.
/// </summary>
/// <param name="MaxCycles">The maximum number of execution cycles.</param>
/// <param name="WallClock">The wall-clock timeout, if any.</param>
public sealed record RunBudget(ulong MaxCycles, TimeSpan? WallClock = null);