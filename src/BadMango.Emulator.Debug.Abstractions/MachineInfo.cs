// <copyright file="MachineInfo.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

/// <summary>
/// Metadata describing a machine instance.
/// </summary>
/// <param name="Name">The machine identifier.</param>
/// <param name="DisplayName">The human-readable name.</param>
/// <param name="CpuType">The CPU architecture name.</param>
/// <param name="MemorySize">The total RAM size in bytes.</param>
/// <param name="Description">An optional description.</param>
public sealed record MachineInfo(
    string Name,
    string DisplayName,
    string CpuType,
    uint MemorySize,
    string? Description = null);