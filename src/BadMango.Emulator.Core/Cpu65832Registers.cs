// <copyright file="Cpu65832Registers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.InteropServices;

/// <summary>
/// Represents the register state of a hypothetical 65832 CPU.
/// </summary>
/// <remarks>
/// This structure contains the CPU registers for the hypothetical 65832, a 32-bit extension
/// of the 65816 architecture. Uses explicit layout for optimal memory packing.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Cpu65832Registers
{
    /// <summary>
    /// Gets or sets the Accumulator register (A - 32-bit).
    /// </summary>
    public uint A { get; set; }

    /// <summary>
    /// Gets or sets the X index register (32-bit).
    /// </summary>
    public uint X { get; set; }

    /// <summary>
    /// Gets or sets the Y index register (32-bit).
    /// </summary>
    public uint Y { get; set; }

    /// <summary>
    /// Gets or sets the Stack Pointer register (S - 32-bit).
    /// </summary>
    public uint S { get; set; }

    /// <summary>
    /// Gets or sets the Processor Status register (P).
    /// </summary>
    public byte P { get; set; }

    /// <summary>
    /// Gets or sets the Program Counter (PC - 32-bit).
    /// </summary>
    public uint PC { get; set; }
}