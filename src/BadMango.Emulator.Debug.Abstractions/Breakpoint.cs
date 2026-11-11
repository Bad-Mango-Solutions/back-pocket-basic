// <copyright file="Breakpoint.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

/// <summary>
/// A breakpoint entry configured in the debug session.
/// </summary>
/// <param name="Address">The breakpoint address.</param>
/// <param name="Label">An optional label.</param>
/// <param name="Enabled">Whether the breakpoint is active.</param>
/// <param name="HitCount">The number of times the breakpoint fired.</param>
public sealed record Breakpoint(uint Address, string? Label = null, bool Enabled = true, int HitCount = 0);