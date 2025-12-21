// <copyright file="Cpu65C02State.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.InteropServices;

/// <summary>
/// Represents the complete state of a 65C02 CPU.
/// </summary>
/// <remarks>
/// This structure captures all CPU registers and execution state for
/// save states, debugging, and state inspection purposes.
/// Uses explicit layout for optimal memory packing.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Cpu65C02State
{
    /// <summary>
    /// Gets or sets the CPU registers.
    /// </summary>
    public Cpu65C02Registers Registers { get; set; }

    /// <summary>
    /// Gets or sets the total number of cycles executed.
    /// </summary>
    public ulong Cycles { get; set; }

    /// <summary>
    /// Gets or sets the Accumulator register (A).
    /// </summary>
    public byte A
    {
        get => Registers.A;
        set
        {
            var r = Registers;
            r.A = value;
            Registers = r;
        }
    }

    /// <summary>
    /// Gets or sets the X index register.
    /// </summary>
    public byte X
    {
        get => Registers.X;
        set
        {
            var r = Registers;
            r.X = value;
            Registers = r;
        }
    }

    /// <summary>
    /// Gets or sets the Y index register.
    /// </summary>
    public byte Y
    {
        get => Registers.Y;
        set
        {
            var r = Registers;
            r.Y = value;
            Registers = r;
        }
    }

    /// <summary>
    /// Gets or sets the Stack Pointer register (S).
    /// </summary>
    public byte S
    {
        get => Registers.S;
        set
        {
            var r = Registers;
            r.S = value;
            Registers = r;
        }
    }

    /// <summary>
    /// Gets or sets the Processor Status register (P).
    /// </summary>
    public byte P
    {
        get => Registers.P;
        set
        {
            var r = Registers;
            r.P = value;
            Registers = r;
        }
    }

    /// <summary>
    /// Gets or sets the Program Counter (PC).
    /// </summary>
    public ushort PC
    {
        get => Registers.PC;
        set
        {
            var r = Registers;
            r.PC = value;
            Registers = r;
        }
    }
}