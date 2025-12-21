// <copyright file="Cpu65832State.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.InteropServices;

/// <summary>
/// Represents the complete state of a 65832 CPU.
/// </summary>
/// <remarks>
/// This structure captures all CPU registers and execution state for
/// save states, debugging, and state inspection purposes.
/// Uses explicit layout for optimal memory packing.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Cpu65832State
{
    /// <summary>
    /// Gets or sets the CPU registers.
    /// </summary>
    public Cpu65832Registers Registers { get; set; }

    /// <summary>
    /// Gets or sets the total number of cycles executed.
    /// </summary>
    public ulong Cycles { get; set; }
}