// <copyright file="Cpu65C02Registers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.InteropServices;

/// <summary>
/// Represents the register state of a 65C02 CPU.
/// </summary>
/// <remarks>
/// This structure contains only the CPU registers, separate from execution state like cycle count.
/// Uses explicit layout for optimal memory packing.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Cpu65C02Registers : ICpuRegisters<byte, byte, byte, Word>
{
    /// <summary>
    /// Gets or sets the Accumulator register (A).
    /// </summary>
    public byte A { get; set; }

    /// <summary>
    /// Gets or sets the X index register.
    /// </summary>
    public byte X { get; set; }

    /// <summary>
    /// Gets or sets the Y index register.
    /// </summary>
    public byte Y { get; set; }

    /// <summary>
    /// Gets or sets the Stack Pointer register (SP).
    /// </summary>
    public byte SP { get; set; }

    /// <summary>
    /// Gets or sets the Processor Status register (P).
    /// </summary>
    public byte P { get; set; }

    /// <summary>
    /// Gets or sets the Program Counter (PC).
    /// </summary>
    public Word PC { get; set; }
}