// <copyright file="Cpu65816Registers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.InteropServices;

/// <summary>
/// Represents the register state of a 65816 CPU.
/// </summary>
/// <remarks>
/// This structure contains the CPU registers for the 65816, which includes
/// 16-bit accumulator and index registers in native mode, plus additional registers.
/// Uses explicit layout for optimal memory packing.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Cpu65816Registers
{
    /// <summary>
    /// Gets or sets the Accumulator register (A/C - 16-bit in native mode, 8-bit in emulation mode).
    /// </summary>
    public ushort A { get; set; }

    /// <summary>
    /// Gets or sets the X index register (16-bit in native mode, 8-bit in emulation mode).
    /// </summary>
    public ushort X { get; set; }

    /// <summary>
    /// Gets or sets the Y index register (16-bit in native mode, 8-bit in emulation mode).
    /// </summary>
    public ushort Y { get; set; }

    /// <summary>
    /// Gets or sets the Stack Pointer register (S - 16-bit).
    /// </summary>
    public ushort S { get; set; }

    /// <summary>
    /// Gets or sets the Processor Status register (P).
    /// </summary>
    public byte P { get; set; }

    /// <summary>
    /// Gets or sets the Data Bank Register (DBR).
    /// </summary>
    public byte DBR { get; set; }

    /// <summary>
    /// Gets or sets the Program Bank Register (PBR).
    /// </summary>
    public byte PBR { get; set; }

    /// <summary>
    /// Gets or sets the Direct Page Register (D).
    /// </summary>
    public ushort D { get; set; }

    /// <summary>
    /// Gets or sets the Program Counter (PC).
    /// </summary>
    public ushort PC { get; set; }
}