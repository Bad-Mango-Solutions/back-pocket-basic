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
public struct Cpu65C02State : ICpuState<Cpu65C02Registers, byte, byte, byte, Word>
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
    /// Gets a value indicating whether the CPU is halted.
    /// </summary>
    /// <remarks>
    /// This property returns true if the CPU is in any halt state (Brk, Wai, or Stp).
    /// For more granular halt state information, use <see cref="HaltReason"/>.
    /// </remarks>
    public readonly bool Halted => HaltReason != HaltState.None;

    /// <summary>
    /// Gets or sets the reason the CPU is halted.
    /// </summary>
    /// <remarks>
    /// Distinguishes between different halt states:
    /// - None: CPU is running
    /// - Brk: Halted by BRK instruction (software interrupt)
    /// - Wai: Halted by WAI instruction (wait for interrupt)
    /// - Stp: Halted by STP instruction (permanent halt until reset).
    /// </remarks>
    public HaltState HaltReason { get; set; }

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
    /// Gets or sets the Stack Pointer register (SP).
    /// </summary>
    public byte SP
    {
        get => Registers.SP;
        set
        {
            var r = Registers;
            r.SP = value;
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
    public Word PC
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