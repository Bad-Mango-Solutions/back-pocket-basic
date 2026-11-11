// <copyright file="Watchpoint.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

/// <summary>
/// A memory watchpoint entry configured in the debug session.
/// </summary>
/// <param name="Address">The watchpoint address.</param>
/// <param name="Access">The access modes to monitor.</param>
/// <param name="StopOnHit">Whether execution should stop when the watchpoint fires.</param>
/// <param name="Enabled">Whether the watchpoint is active.</param>
/// <param name="HitCount">The number of times the watchpoint fired.</param>
/// <param name="Label">An optional label.</param>
public sealed record Watchpoint(uint Address, WatchAccess Access, bool StopOnHit = false, bool Enabled = true, int HitCount = 0, string? Label = null);